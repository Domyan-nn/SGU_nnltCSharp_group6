using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// A4 – Thu ngân: mở ca, chọn đơn đã tiếp nhận còn phải thu, nhập tiền khách đưa (hiện tiền thối), đóng ca và xem chênh lệch.
// Mọi thao tác gọi API qua StaffApi (quyền PAYMENT_COLLECT); form chỉ thu đầu vào và hiển thị, máy chủ quyết định số tiền.
// Danh sách tự làm mới mỗi 5 giây; khi lần tải lỗi vẫn giữ dữ liệu cũ và báo rõ, không xóa trắng.
public sealed class CashierForm : Form
{
    private readonly StaffApi _staff;
    private readonly DataGridView _grid = new() { Name = "payable" };
    private readonly Label _shiftLine = new() { Name = "shift", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(8, 8, 16, 0) };
    private readonly Label _due = new() { Name = "due", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(0, 8, 16, 0) };
    private readonly Label _change = new() { Name = "change", AutoSize = true, Margin = new Padding(16, 8, 16, 0) };
    private readonly TextBox _received = new() { Name = "received", Width = 150, MaxLength = 20, AccessibleName = "Tiền khách đưa", TextAlign = HorizontalAlignment.Right };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _openShift = Ui.Button("Mở ca…");
    private readonly Button _closeShift = Ui.Button("Đóng ca…");
    private readonly Button _refresh = Ui.Button("Tải lại (F5)");
    private readonly Button _exact = Ui.Button("Đưa đủ");
    private readonly Button _pay = Ui.Button("Thu tiền mặt", primary: true);
    private readonly CheckBox _auto = new() { Name = "auto", Text = "Tự làm mới mỗi 5 giây", Checked = true, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private List<PayableOrderDto> _data = [];
    private ShiftDto? _shift;
    private bool _busy, _modal;

    // Lần thu có kết quả chưa xác định (mất mạng, hết giờ): thử lại phải dùng đúng requestId cũ để máy chủ không thu hai lần.
    private readonly record struct PendingPayment(Guid RequestId, long OrderId, decimal Amount);
    private PendingPayment? _pending;

    public CashierForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Thu ngân (A4)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1100, 640);
        MinimumSize = new Size(900, 520);
        KeyPreview = true;
        AcceptButton = _pay;

        Ui.ConfigureGrid(_grid, "Danh sách đơn cần thu tiền");
        _grid.Columns.Add(Ui.Column("Mã đơn", 18, auto: true));
        _grid.Columns.Add(Ui.Column("Khách", 40, minWidth: 140));
        _grid.Columns.Add(Ui.Column("Mã nhận hàng", 16, auto: true));
        _grid.Columns.Add(Ui.Column("Trạng thái", 16, auto: true));
        _grid.Columns.Add(Ui.Column("Tổng tiền", 14, alignRight: true, auto: true));
        _grid.Columns.Add(Ui.Column("Đã thu", 14, alignRight: true, auto: true));
        _grid.Columns.Add(Ui.Column("Còn phải thu", 14, alignRight: true, auto: true));
        _grid.SelectionChanged += (_, _) => ShowSelection();

        _openShift.Click += async (_, _) => await OpenShiftAsync();
        _closeShift.Click += async (_, _) => await CloseShiftAsync();
        _refresh.Click += async (_, _) => await RefreshAsync();
        _exact.Click += (_, _) => { if (Selected is { } o) _received.Text = Display.Number(o.Remaining); };
        _pay.Click += async (_, _) => await PayAsync();
        _auto.CheckedChanged += (_, _) => _timer.Enabled = _auto.Checked;
        _received.TextChanged += (_, _) => UpdatePreview();

        var shiftBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        shiftBar.Controls.Add(_openShift);
        shiftBar.Controls.Add(_closeShift);
        shiftBar.Controls.Add(_refresh);
        shiftBar.Controls.Add(_auto);
        shiftBar.Controls.Add(_shiftLine);

        var payBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        payBar.Controls.Add(_due);
        payBar.Controls.Add(new Label { Text = "Khách đưa:", AutoSize = true, Margin = new Padding(0, 12, 6, 0) });
        payBar.Controls.Add(_received);
        payBar.Controls.Add(new Label { Text = "₫", AutoSize = true, Margin = new Padding(4, 12, 8, 0) });
        payBar.Controls.Add(_exact);
        payBar.Controls.Add(_change);
        payBar.Controls.Add(_pay);

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 4) };
        gridHost.Controls.Add(_grid);

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 4, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(shiftBar);
        root.Controls.Add(gridHost);
        root.Controls.Add(payBar);
        root.Controls.Add(_status);
        Controls.Add(root);

        _timer.Tick += async (_, _) =>
        {
            if (_auto.Checked && !_busy && !_modal && Visible) await RefreshAsync();
        };
        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; await RefreshAsync(); }
        };
        Shown += async (_, _) => { await RefreshAsync(); _timer.Enabled = _auto.Checked; };
        FormClosed += (_, _) => { _timer.Stop(); _timer.Dispose(); };
        ShowShift();
        ShowSelection();
    }

    private PayableOrderDto? Selected =>
        _grid.CurrentRow?.Tag is long id ? _data.FirstOrDefault(o => o.OrderId == id) : null;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var hasShift = _shift is not null;
        _openShift.Enabled = !_busy && !hasShift;
        _closeShift.Enabled = !_busy && hasShift;
        _refresh.Enabled = !_busy;
        var canPay = !_busy && hasShift && Selected is not null;
        _pay.Enabled = canPay;
        _exact.Enabled = canPay;
        _received.Enabled = !_busy && hasShift;
    }

    private void ShowShift()
    {
        _shiftLine.Text = CashierText.ShiftLine(_shift);
        UpdateButtons();
    }

    private void ShowSelection()
    {
        var order = Selected;
        _due.Text = order is null ? "Còn phải thu: —" : "Còn phải thu: " + Display.Money(order.Remaining);
        UpdatePreview();
        UpdateButtons();
    }

    private void UpdatePreview()
    {
        var order = Selected;
        _change.Text = order is null ? "Tiền thối: —" : CashierInput.PreviewChange(_received.Text, order.Remaining);
    }

    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Tải ca hiện tại và danh sách đơn cần thu; lỗi thì giữ dữ liệu cũ và báo rõ (mất kết nối không được hiện như "không có đơn").
    private async Task LoadAsync()
    {
        var selectedId = Selected?.OrderId;
        var shift = await _staff.CurrentShiftAsync();
        if (IsDisposed) return;
        if (!shift.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(shift, "thông tin ca") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        var orders = await _staff.PayableOrdersAsync();
        if (IsDisposed) return;
        if (!orders.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(orders, "danh sách đơn cần thu") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        _shift = shift.Value!.Shift;
        _data = orders.Value!;
        ShowShift();
        Bind(selectedId);
        Ui.SetStatus(_status, (_shift is null ? "Chưa mở ca: mở ca trước khi thu tiền. " : "") +
            $"Có {_data.Count} đơn cần thu. Cập nhật lúc {DateTime.Now:HH:mm:ss}.");
    }

    private void Bind(long? selectedId)
    {
        _grid.Rows.Clear();
        foreach (var o in _data)
        {
            var index = _grid.Rows.Add(o.OrderCode, o.ContactName ?? "", o.PickupCode ?? "", Display.OrderStatus(o.Status),
                Display.Money(o.Total), Display.Money(o.Paid), Display.Money(o.Remaining));
            _grid.Rows[index].Tag = o.OrderId;
        }
        if (_grid.Rows.Count > 0)
        {
            var keep = selectedId is null ? -1 : _data.FindIndex(o => o.OrderId == selectedId);
            var row = _grid.Rows[Math.Max(keep, 0)];
            _grid.CurrentCell = row.Cells[0];
            row.Selected = true;
        }
        ShowSelection();
    }

    private void Notify(string message, bool ok)
    {
        Ui.SetStatus(_status, message, error: !ok);
        if (!ok) MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // Sau thao tác thì tải lại; thanh trạng thái giữ kết quả thao tác (không bị ghi đè bằng "Cập nhật lúc").
    private async Task LoadAfterAction(string message, bool actionOk)
    {
        await LoadAsync();
        if (IsDisposed) return;
        if (_status.ForeColor == Color.Firebrick)
            Ui.SetStatus(_status, message + " " + _status.Text.Replace("Lỗi: ", ""), error: true);
        else
            Ui.SetStatus(_status, $"{message} Còn {_data.Count} đơn cần thu.", error: !actionOk);
    }

    private async Task OpenShiftAsync()
    {
        if (_busy || _shift is not null) return;
        SetBusy(true);
        try
        {
            var counters = await _staff.CountersAsync();
            if (IsDisposed) return;
            if (!counters.Ok) { Notify(StaffMessages.ForLoad(counters, "danh sách quầy"), false); return; }
            if (counters.Value!.Count == 0) { Notify("Chưa có quầy nào trong hệ thống nên không mở ca được.", false); return; }

            long counterId; decimal openingCash;
            _modal = true;
            try
            {
                using var dialog = new OpenShiftDialog(counters.Value!);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                counterId = dialog.CounterId; openingCash = dialog.OpeningCash;
            }
            finally { _modal = false; }

            var result = await _staff.OpenShiftAsync(counterId, openingCash);
            if (IsDisposed) return;
            var message = CashierMessages.ForOpenShift(result);
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await LoadAfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task CloseShiftAsync()
    {
        var shift = _shift;
        if (shift is null || _busy) return;
        decimal counted; string? note;
        _modal = true;
        try
        {
            using var dialog = new CloseShiftDialog(shift);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            counted = dialog.CountedCash; note = dialog.Note;
        }
        finally { _modal = false; }

        SetBusy(true);
        try
        {
            var result = await _staff.CloseShiftAsync(shift.ShiftId, counted, note);
            if (IsDisposed) return;
            var message = CashierMessages.ForCloseShift(result);
            Notify(message, result.Ok);
            if (result.Ok)
                MessageBox.Show(this, CashierText.ShiftSummary(result.Value!), "Kết quả đối soát ca",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (StaffMessages.ReloadAfter(result)) await LoadAfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task PayAsync()
    {
        var order = Selected;
        if (order is null || _busy || _shift is null) return;
        if (!CashierInput.TryParseReceived(_received.Text, order.Remaining, out var received, out var error))
        {
            Ui.SetStatus(_status, error, error: true);
            _received.Focus();
            return;
        }
        // Mỗi lần bấm Thu tiền tạo một requestId; chỉ dùng lại requestId cũ khi lần trước chưa xác định và đơn, số tiền không đổi.
        var requestId = _pending is { } p && p.OrderId == order.OrderId && p.Amount == received ? p.RequestId : Guid.NewGuid();

        SetBusy(true);
        try
        {
            var result = await _staff.PayCashAsync(requestId, order.OrderId, received);
            if (IsDisposed) return;
            _pending = result.Unknown ? new PendingPayment(requestId, order.OrderId, received) : null;
            var message = CashierMessages.ForPayment(result, order.OrderCode);
            Notify(message, result.Ok);
            if (result.Ok)
            {
                _received.Clear();
                MessageBox.Show(this, $"Đơn {order.OrderCode}: đã thu {Display.Money(result.Value!.AmountDue)}." + Environment.NewLine +
                    $"Khách đưa {Display.Money(result.Value.AmountReceived)} – TIỀN THỐI {Display.Money(result.Value.Change)}.",
                    "Thu tiền thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (StaffMessages.ReloadAfter(result)) await LoadAfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }
}
