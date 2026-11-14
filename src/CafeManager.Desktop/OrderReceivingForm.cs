using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// D02 – Thu ngân tiếp nhận hoặc từ chối đơn Web đang chờ (UC03). Mọi thao tác gọi API qua StaffApi; không truy cập CSDL.
// Danh sách tự làm mới mỗi 5 giây; khi lần tải lỗi vẫn giữ danh sách cũ và báo rõ, không xóa trắng.
public sealed class OrderReceivingForm : Form
{
    private readonly StaffApi _staff;
    private readonly DataGridView _orders = new() { Name = "orders" };
    private readonly DataGridView _lines = new() { Name = "lines" };
    private readonly TextBox _info = new()
    {
        Name = "info", Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = SystemColors.Control,
        Dock = DockStyle.Fill, TabStop = false, AccessibleName = "Chi tiết đơn",
    };
    private readonly Label _total = new() { Name = "total", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(0, 8, 16, 0) };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _refresh = Ui.Button("Tải lại (F5)");
    private readonly Button _accept = Ui.Button("Tiếp nhận đơn", primary: true);
    private readonly Button _reject = Ui.Button("Từ chối đơn…");
    private readonly CheckBox _auto = new() { Name = "auto", Text = "Tự làm mới mỗi 5 giây", Checked = true, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private List<StaffOrderDto> _data = [];
    private bool _busy, _modal;

    public OrderReceivingForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Tiếp nhận đơn Web (D02)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(900, 560);
        KeyPreview = true;
        _info.Height = (int)(Font.Height * 3.6);

        Ui.ConfigureGrid(_orders, "Danh sách đơn chờ tiếp nhận");
        _orders.Columns.Add(Ui.Column("Mã đơn", 20, auto: true));
        _orders.Columns.Add(Ui.Column("Giờ đặt", 15, auto: true));
        _orders.Columns.Add(Ui.Column("Giờ nhận", 15, auto: true));
        _orders.Columns.Add(Ui.Column("Khách", 40, minWidth: 140));
        _orders.Columns.Add(Ui.Column("Điện thoại", 14, auto: true));
        _orders.Columns.Add(Ui.Column("Tổng tiền", 14, alignRight: true, auto: true));
        _orders.Columns.Add(Ui.Column("Trạng thái", 15, auto: true));
        _orders.SelectionChanged += (_, _) => ShowDetail();

        Ui.ConfigureGrid(_lines, "Các món trong đơn");
        _lines.Columns.Add(Ui.Column("Món", 40, minWidth: 140));
        _lines.Columns.Add(Ui.Column("Biến thể", 14, auto: true));
        _lines.Columns.Add(Ui.Column("SL", 6, alignRight: true, auto: true));
        _lines.Columns.Add(Ui.Column("Đơn giá", 14, alignRight: true, auto: true));
        _lines.Columns.Add(Ui.Column("Tùy chọn (mỗi ly)", 40, minWidth: 140));
        _lines.Columns.Add(Ui.Column("Thành tiền", 16, alignRight: true, auto: true));

        _refresh.Click += async (_, _) => await RefreshAsync();
        _accept.Click += async (_, _) => await AcceptAsync();
        _reject.Click += async (_, _) => await RejectAsync();
        _auto.CheckedChanged += (_, _) => _timer.Enabled = _auto.Checked;

        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        toolbar.Controls.Add(_refresh);
        toolbar.Controls.Add(_auto);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
        actions.Controls.Add(_total);
        actions.Controls.Add(_accept);
        actions.Controls.Add(_reject);

        var right = new TableLayoutPanel { ColumnCount = 1, RowCount = 3, Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.Controls.Add(_info);
        right.Controls.Add(_lines);
        right.Controls.Add(actions);

        // Danh sách đơn ở trên, chi tiết ở dưới: cả hai lưới đều có đủ chiều ngang để không cắt chữ.
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        var top = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 4) };
        top.Controls.Add(_orders);
        split.Panel1.Controls.Add(top);
        split.Panel2.Controls.Add(right);
        Shown += (_, _) => split.SplitterDistance = Math.Max(120, (int)(split.Height * 0.42));

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 3, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(toolbar);
        root.Controls.Add(split);
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
        ShowDetail();
    }

    private StaffOrderDto? Selected =>
        _orders.CurrentRow?.Tag is long id ? _data.FirstOrDefault(o => o.Detail.Summary.OrderId == id) : null;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var canAct = !_busy && Selected is { Detail.Summary.Status: "Pending" };
        _accept.Enabled = canAct;
        _reject.Enabled = canAct;
        _refresh.Enabled = !_busy;
    }

    // Tải lại có kiểm tra bận (dùng cho nút, F5 và bộ hẹn giờ).
    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Tải danh sách; lỗi thì giữ danh sách cũ và báo rõ (mất kết nối không được hiện như "không có đơn").
    private async Task LoadAsync()
    {
        var selectedId = Selected?.Detail.Summary.OrderId;
        var result = await _staff.PendingOrdersAsync();
        if (IsDisposed) return;
        if (!result.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(result, "danh sách đơn") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        _data = result.Value!;
        Bind(selectedId);
        Ui.SetStatus(_status, $"Có {_data.Count} đơn chờ tiếp nhận. Cập nhật lúc {DateTime.Now:HH:mm:ss}.");
    }

    private void Bind(long? selectedId)
    {
        _orders.Rows.Clear();
        foreach (var order in _data)
        {
            var d = order.Detail;
            var index = _orders.Rows.Add(d.Summary.OrderCode, Display.TimeShort(d.Summary.CreatedAt), Display.TimeShort(d.Summary.PickupAt),
                d.ContactName ?? "", d.ContactPhone ?? "", Display.Money(d.Summary.Total), Display.OrderStatus(d.Summary.Status));
            _orders.Rows[index].Tag = d.Summary.OrderId;
        }
        if (_orders.Rows.Count > 0)
        {
            var keep = selectedId is null ? -1 : _data.FindIndex(o => o.Detail.Summary.OrderId == selectedId);
            var row = _orders.Rows[Math.Max(keep, 0)];
            _orders.CurrentCell = row.Cells[0];
            row.Selected = true;
        }
        ShowDetail();
    }

    private void ShowDetail()
    {
        _lines.Rows.Clear();
        var order = Selected;
        if (order is null)
        {
            _info.Text = _data.Count == 0 ? "Không có đơn nào đang chờ tiếp nhận." : "Chọn một đơn để xem chi tiết.";
            _total.Text = "";
            UpdateButtons();
            return;
        }
        var d = order.Detail;
        _info.Text = string.Join(Environment.NewLine,
            $"Mã đơn: {d.Summary.OrderCode}    Mã nhận hàng: {d.PickupCode}    Trạng thái: {Display.OrderStatus(d.Summary.Status)}",
            $"Khách: {d.ContactName} – {d.ContactPhone}",
            $"Giờ đặt: {Display.Time(d.Summary.CreatedAt)}    Giờ nhận: {Display.Time(d.Summary.PickupAt)}");
        foreach (var line in d.Lines)
            _lines.Rows.Add(line.ProductName, line.VariantName, line.Quantity, Display.Money(line.UnitPrice), Display.AddonsText(line), Display.Money(Display.LineTotal(line)));
        _total.Text = "Tổng: " + Display.Money(d.Summary.Total);
        UpdateButtons();
    }

    private void Notify(string message, bool ok)
    {
        Ui.SetStatus(_status, message, error: !ok);
        if (!ok) MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task AcceptAsync()
    {
        var order = Selected;
        if (order is null || _busy) return;
        var d = order.Detail;
        var confirm = MessageBox.Show(this,
            $"Tiếp nhận đơn {d.Summary.OrderCode}?{Environment.NewLine}Khách: {d.ContactName}{Environment.NewLine}Giờ nhận: {Display.Time(d.Summary.PickupAt)}{Environment.NewLine}Tổng: {Display.Money(d.Summary.Total)}",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        if (confirm != DialogResult.Yes) return;

        SetBusy(true);
        try
        {
            var result = await _staff.AcceptAsync(d.Summary.OrderId);
            if (IsDisposed) return;
            var message = $"Đơn {d.Summary.OrderCode}: {StaffMessages.ForAccept(result)}";
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await LoadAfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task RejectAsync()
    {
        var order = Selected;
        if (order is null || _busy) return;
        var d = order.Detail;
        string reason;
        _modal = true;
        try
        {
            using var dialog = new RejectDialog(d.Summary.OrderCode);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            reason = dialog.Reason;
        }
        finally { _modal = false; }

        SetBusy(true);
        try
        {
            var result = await _staff.RejectAsync(d.Summary.OrderId, reason);
            if (IsDisposed) return;
            var message = $"Đơn {d.Summary.OrderCode}: {StaffMessages.ForReject(result)}";
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await LoadAfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Sau thao tác thì tải lại; thanh trạng thái giữ kết quả thao tác (không bị ghi đè bằng "Cập nhật lúc").
    private async Task LoadAfterAction(string message, bool actionOk)
    {
        await LoadAsync();
        if (IsDisposed) return;
        if (_status.ForeColor == Color.Firebrick) // lần tải lỗi: giữ cả kết quả thao tác lẫn lỗi tải
            Ui.SetStatus(_status, message + " " + _status.Text.Replace("Lỗi: ", ""), error: true);
        else
            Ui.SetStatus(_status, $"{message} Còn {_data.Count} đơn chờ.", error: !actionOk);
    }
}
