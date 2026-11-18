using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// C3 – Pha chế: danh sách ly chưa giao (cũ nhất trước), bắt đầu làm → làm xong → đã giao (quyền PREPARATION_WORK).
// Mỗi ly là một dòng. Danh sách tự làm mới mỗi 5 giây; khi lần tải lỗi vẫn giữ danh sách cũ và báo rõ, không xóa trắng.
// Ba thao tác không tự thử lại: máy chủ trả 409 nếu ly đã đổi trạng thái, khi đó form tải lại danh sách.
public sealed class PreparationForm : Form
{
    private readonly StaffApi _staff;
    private readonly DataGridView _grid = new() { Name = "units" };
    private readonly Label _info = new() { Name = "info", AutoSize = true, Margin = new Padding(0, 8, 16, 0) };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _refresh = Ui.Button("Tải lại (F5)");
    private readonly Button _action = Ui.Button("Chọn một ly", primary: true);
    private readonly CheckBox _auto = new() { Name = "auto", Text = "Tự làm mới mỗi 5 giây", Checked = true, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private List<PreparationUnitDto> _data = [];
    private bool _busy;

    public PreparationForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Pha chế (C3)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1180, 640);
        MinimumSize = new Size(900, 480);
        KeyPreview = true;

        Ui.ConfigureGrid(_grid, "Danh sách ly cần pha chế");
        _grid.Columns.Add(Ui.Column("Mã đơn", 16, auto: true));
        _grid.Columns.Add(Ui.Column("Mã nhận hàng", 14, auto: true));
        _grid.Columns.Add(Ui.Column("Món", 30, minWidth: 140));
        _grid.Columns.Add(Ui.Column("Biến thể", 12, auto: true));
        _grid.Columns.Add(Ui.Column("Tùy chọn (mỗi ly)", 30, minWidth: 120));
        _grid.Columns.Add(Ui.Column("Ly", 6, alignRight: true, auto: true));
        _grid.Columns.Add(Ui.Column("Trạng thái", 14, auto: true));
        _grid.Columns.Add(Ui.Column("Nhận lúc", 12, auto: true));
        _grid.SelectionChanged += (_, _) => ShowSelection();
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await ActAsync(); };

        _refresh.Click += async (_, _) => await RefreshAsync();
        _action.Click += async (_, _) => await ActAsync();
        _auto.CheckedChanged += (_, _) => _timer.Enabled = _auto.Checked;

        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        toolbar.Controls.Add(_refresh);
        toolbar.Controls.Add(_auto);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4) };
        actions.Controls.Add(_info);
        actions.Controls.Add(_action);

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 4) };
        gridHost.Controls.Add(_grid);

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 4, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(toolbar);
        root.Controls.Add(gridHost);
        root.Controls.Add(actions);
        root.Controls.Add(_status);
        Controls.Add(root);

        _timer.Tick += async (_, _) =>
        {
            if (_auto.Checked && !_busy && Visible) await RefreshAsync();
        };
        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; await RefreshAsync(); }
        };
        Shown += async (_, _) => { await RefreshAsync(); _timer.Enabled = _auto.Checked; };
        FormClosed += (_, _) => { _timer.Stop(); _timer.Dispose(); };
        ShowSelection();
    }

    private PreparationUnitDto? Selected =>
        _grid.CurrentRow?.Tag is long id ? _data.FirstOrDefault(u => u.UnitId == id) : null;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _refresh.Enabled = !_busy;
        var next = Selected is { } u ? PreparationText.NextAction(u.Status) : null;
        _action.Text = next ?? (Selected is null ? "Chọn một ly" : "Không còn thao tác");
        _action.Enabled = !_busy && next is not null;
    }

    private void ShowSelection()
    {
        var u = Selected;
        _info.Text = u is null
            ? (_data.Count == 0 ? "Không có ly nào cần làm." : "Chọn một ly để thao tác (bấm đúp để làm bước kế tiếp).")
            : $"Đơn {u.OrderCode} – {u.ProductName} ({u.VariantName}) – ly {PreparationText.Cups(u)} – {PreparationText.Status(u.Status)}";
        UpdateButtons();
    }

    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Tải danh sách; lỗi thì giữ danh sách cũ và báo rõ (mất kết nối không được hiện như "không có ly nào").
    private async Task LoadAsync()
    {
        var selectedId = Selected?.UnitId;
        var result = await _staff.PreparationQueueAsync();
        if (IsDisposed) return;
        if (!result.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(result, "danh sách ly cần làm") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        _data = result.Value!;
        Bind(selectedId);
        var waiting = _data.Count(u => u.Status == PreparationStatuses.Waiting);
        var preparing = _data.Count(u => u.Status == PreparationStatuses.Preparing);
        var ready = _data.Count(u => u.Status == PreparationStatuses.Ready);
        Ui.SetStatus(_status, $"Chờ làm: {waiting}; đang làm: {preparing}; sẵn sàng giao: {ready}. Cập nhật lúc {DateTime.Now:HH:mm:ss}.");
    }

    private void Bind(long? selectedId)
    {
        _grid.Rows.Clear();
        foreach (var u in _data)
        {
            var index = _grid.Rows.Add(u.OrderCode, u.PickupCode ?? "", u.ProductName, u.VariantName, PreparationText.Addons(u),
                PreparationText.Cups(u), PreparationText.Status(u.Status), Display.TimeShort(u.AcceptedAt));
            _grid.Rows[index].Tag = u.UnitId;
        }
        if (_grid.Rows.Count > 0)
        {
            var keep = selectedId is null ? -1 : _data.FindIndex(u => u.UnitId == selectedId);
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

    // Bước kế tiếp của ly đang chọn: Chờ làm → Đang làm → Sẵn sàng → Đã giao.
    private async Task ActAsync()
    {
        var unit = Selected;
        if (unit is null || _busy) return;
        var status = unit.Status;
        if (PreparationText.NextAction(status) is null) return;

        SetBusy(true);
        try
        {
            var result = status switch
            {
                PreparationStatuses.Waiting => await _staff.StartUnitAsync(unit.UnitId),
                PreparationStatuses.Preparing => await _staff.MarkReadyAsync(unit.UnitId),
                _ => await _staff.DeliverAsync(unit.UnitId),
            };
            if (IsDisposed) return;
            var message = $"Đơn {unit.OrderCode}, ly {PreparationText.Cups(unit)}: {PreparationText.ForAction(result, PreparationText.SuccessText(status))}";
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result))
            {
                await LoadAsync();
                if (IsDisposed) return;
                if (_status.ForeColor == Color.Firebrick)
                    Ui.SetStatus(_status, message + " " + _status.Text.Replace("Lỗi: ", ""), error: true);
                else
                    Ui.SetStatus(_status, message + " " + _status.Text, error: !result.Ok);
            }
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }
}
