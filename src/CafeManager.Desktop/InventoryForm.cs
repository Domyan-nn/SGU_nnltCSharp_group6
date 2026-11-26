using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// UC13 – Kho nguyên liệu (quyền INVENTORY_WORK): xem tồn sổ, lượng đang giữ và tồn khả dụng; nhập kho; lập phiếu kiểm kê.
// Người có thêm INVENTORY_APPROVE thấy nút Duyệt/Từ chối phiếu. Người lập phiếu không tự duyệt (máy chủ chặn, form báo trước).
// Nhập kho: mỗi lần bấm có một mã yêu cầu; nếu chưa biết kết quả (mất phản hồi) thì lần thử lại với cùng nguyên liệu,
// số lượng và ghi chú dùng lại mã cũ để máy chủ không cộng tồn hai lần.
public sealed class InventoryForm : Form
{
    private readonly record struct PendingReceipt(Guid RequestId, long IngredientId, decimal Quantity, string? Note);

    private readonly StaffApi _staff;
    private readonly bool _canApprove;
    private readonly string? _myName;
    private readonly DataGridView _items = new() { Name = "inventory" };
    private readonly DataGridView _counts = new() { Name = "counts" };
    private readonly TextBox _search = new() { Name = "search", Width = 220, AccessibleName = "Tìm nguyên liệu", PlaceholderText = "Tìm theo tên…" };
    private readonly ComboBox _filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, AccessibleName = "Lọc phiếu kiểm kê" };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _find = Ui.Button("Tìm");
    private readonly Button _refresh = Ui.Button("Tải lại (F5)");
    private readonly Button _receive = Ui.Button("Nhập kho…", primary: true);
    private readonly Button _count = Ui.Button("Kiểm kê…");
    private readonly Button _approve = Ui.Button("Duyệt…");
    private readonly Button _reject = Ui.Button("Từ chối…");
    private List<InventoryItemDto> _itemData = [];
    private List<StockCountDto> _countData = [];
    private PendingReceipt? _pendingReceipt;
    private bool _busy;

    public InventoryForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        _canApprove = api.CurrentUser?.Permissions.Contains(Permissions.InventoryApprove) == true;
        _myName = api.CurrentUser?.DisplayName;
        Text = "Kho nguyên liệu (UC13)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1000, 680);
        MinimumSize = new Size(780, 520);
        KeyPreview = true;

        Ui.ConfigureGrid(_items, "Tồn kho nguyên liệu");
        _items.Columns.Add(Ui.Column("Tên nguyên liệu", 34, minWidth: 160));
        _items.Columns.Add(Ui.Column("Đơn vị", 8, auto: true));
        _items.Columns.Add(Ui.Column("Tồn sổ", 12, alignRight: true, auto: true));
        _items.Columns.Add(Ui.Column("Đang giữ cho đơn", 12, alignRight: true, auto: true));
        _items.Columns.Add(Ui.Column("Khả dụng", 12, alignRight: true, auto: true));
        _items.Columns.Add(Ui.Column("Phiếu kiểm kê", 12, auto: true));
        _items.Columns.Add(Ui.Column("Trạng thái", 12, auto: true));
        _items.SelectionChanged += (_, _) => UpdateButtons();

        Ui.ConfigureGrid(_counts, "Phiếu kiểm kê");
        _counts.Columns.Add(Ui.Column("Mã phiếu", 8, auto: true));
        _counts.Columns.Add(Ui.Column("Nguyên liệu", 22, minWidth: 140));
        _counts.Columns.Add(Ui.Column("Tồn sổ lúc đếm", 10, alignRight: true, auto: true));
        _counts.Columns.Add(Ui.Column("Thực đếm", 10, alignRight: true, auto: true));
        _counts.Columns.Add(Ui.Column("Chênh lệch", 10, alignRight: true, auto: true));
        _counts.Columns.Add(Ui.Column("Người đếm", 14, auto: true));
        _counts.Columns.Add(Ui.Column("Lúc đếm", 12, auto: true));
        _counts.Columns.Add(Ui.Column("Trạng thái", 10, auto: true));
        _counts.Columns.Add(Ui.Column("Người duyệt", 14, auto: true));
        _counts.SelectionChanged += (_, _) => UpdateButtons();

        _filter.Items.AddRange(["Chờ duyệt", "Tất cả"]);
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += async (_, _) => await RefreshAsync();
        _find.Click += async (_, _) => await RefreshAsync();
        _refresh.Click += async (_, _) => { _search.Clear(); await RefreshAsync(); };
        _receive.Click += async (_, _) => await ReceiveAsync();
        _count.Click += async (_, _) => await CountAsync();
        _approve.Click += async (_, _) => await ApproveAsync();
        _reject.Click += async (_, _) => await RejectAsync();
        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; await RefreshAsync(); }
        };

        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        toolbar.Controls.AddRange([_search, _find, _refresh, _receive, _count]);

        var countBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        countBar.Controls.Add(new Label { Text = "Phiếu kiểm kê:", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = new Font(Font, FontStyle.Bold) });
        countBar.Controls.Add(_filter);
        if (_canApprove) countBar.Controls.AddRange([_approve, _reject]);
        else countBar.Controls.Add(new Label { Text = "Quản lý có quyền duyệt kiểm kê sẽ duyệt hoặc từ chối phiếu.", AutoSize = true, Margin = new Padding(8, 8, 0, 0) });

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 5, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(toolbar);
        root.Controls.Add(Host(_items));
        root.Controls.Add(countBar);
        root.Controls.Add(Host(_counts));
        root.Controls.Add(_status);
        Controls.Add(root);

        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; await RefreshAsync(); }
        };
        Shown += async (_, _) => await RefreshAsync();
        UpdateButtons();
    }

    private static Panel Host(Control grid)
    {
        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 4) };
        host.Controls.Add(grid);
        return host;
    }

    private InventoryItemDto? SelectedItem =>
        _items.CurrentRow?.Tag is long id ? _itemData.FirstOrDefault(i => i.IngredientId == id) : null;

    private StockCountDto? SelectedCount =>
        _counts.CurrentRow?.Tag is long id ? _countData.FirstOrDefault(c => c.CountId == id) : null;

    private bool IsMine(StockCountDto count) => _myName is not null && count.CountedByName == _myName;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var item = SelectedItem;
        var count = SelectedCount;
        _find.Enabled = !_busy;
        _refresh.Enabled = !_busy;
        _filter.Enabled = !_busy;
        _receive.Enabled = !_busy && item is { IsActive: true };
        _count.Enabled = !_busy && item is { PendingCountId: null };
        var decidable = !_busy && count is { Status: StockCountStatuses.Pending } && !IsMine(count);
        _approve.Enabled = decidable;
        _reject.Enabled = decidable;
    }

    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Tải cả hai danh sách; lỗi thì giữ dữ liệu cũ và báo rõ.
    private async Task LoadAsync()
    {
        var selectedItem = SelectedItem?.IngredientId;
        var selectedCount = SelectedCount?.CountId;
        var items = await _staff.InventoryAsync(_search.Text);
        var counts = await _staff.StockCountsAsync(_filter.SelectedIndex == 0 ? StockCountStatuses.Pending : null);
        if (IsDisposed) return;
        if (!items.Ok || !counts.Ok)
        {
            var failed = !items.Ok ? StaffMessages.ForLoad(items, "tồn kho") : StaffMessages.ForLoad(counts, "phiếu kiểm kê");
            Ui.SetStatus(_status, failed + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        _itemData = items.Value!;
        _countData = counts.Value!;
        BindItems(selectedItem);
        BindCounts(selectedCount);
        var pending = _itemData.Count(i => i.PendingCountId is not null);
        Ui.SetStatus(_status, $"Có {_itemData.Count} nguyên liệu, {pending} phiếu kiểm kê chờ duyệt. Cập nhật lúc {DateTime.Now:HH:mm:ss}.");
    }

    private void BindItems(long? selectedId)
    {
        _items.Rows.Clear();
        foreach (var i in _itemData)
        {
            var index = _items.Rows.Add(i.IngredientName, i.UnitName, Display.Number(i.QuantityOnHand), Display.Number(i.QuantityReserved),
                Display.Number(i.QuantityAvailable), i.PendingCountId is { } c ? $"CNT-{c} chờ duyệt" : "", i.IsActive ? "Đang dùng" : "Ngừng sử dụng");
            _items.Rows[index].Tag = i.IngredientId;
        }
        Select(_items, selectedId is null ? -1 : _itemData.FindIndex(i => i.IngredientId == selectedId));
    }

    private void BindCounts(long? selectedId)
    {
        _counts.Rows.Clear();
        foreach (var c in _countData)
        {
            var index = _counts.Rows.Add($"CNT-{c.CountId}", c.IngredientName, $"{Display.Number(c.BookQty)} {c.UnitName}",
                $"{Display.Number(c.CountedQty)} {c.UnitName}", $"{InventoryText.Difference(c.Difference)} {c.UnitName}",
                c.CountedByName, Display.Time(c.CountedAt), InventoryText.Status(c.Status), c.ApprovedByName ?? "");
            _counts.Rows[index].Tag = c.CountId;
        }
        Select(_counts, selectedId is null ? -1 : _countData.FindIndex(c => c.CountId == selectedId));
    }

    private void Select(DataGridView grid, int keep)
    {
        if (grid.Rows.Count > 0)
        {
            var row = grid.Rows[Math.Max(keep, 0)];
            grid.CurrentCell = row.Cells[0];
            row.Selected = true;
        }
        UpdateButtons();
    }

    private void Notify(string message, bool ok)
    {
        Ui.SetStatus(_status, message, error: !ok);
        if (!ok) MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task AfterAction(string message, bool ok)
    {
        await LoadAsync();
        if (IsDisposed) return;
        var loadFailed = _status.ForeColor == Color.Firebrick;
        Ui.SetStatus(_status, message + " " + _status.Text.Replace("Lỗi: ", ""), error: loadFailed || !ok);
    }

    private static string Message<T>(ApiResult<T> result, string success) => result switch
    {
        { Ok: true } => success,
        { Unknown: true } => result.Message + " Danh sách đã được tải lại; hãy kiểm tra thao tác đã có hiệu lực chưa trước khi làm lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền thực hiện thao tác kho này.",
        { StatusCode: 404 } => "Không tìm thấy mục này (có thể vừa bị thay đổi). Danh sách đã được tải lại.",
        _ => result.Message,
    };

    private async Task ReceiveAsync()
    {
        var item = SelectedItem;
        if (item is null || _busy) return;
        var pending = _pendingReceipt is { } p && p.IngredientId == item.IngredientId ? p : (PendingReceipt?)null;
        decimal quantity; string? note;
        using (var dialog = new StockReceiptDialog(item, pending?.Quantity, pending?.Note))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            quantity = dialog.Quantity; note = dialog.Note;
        }
        var requestId = pending is { } same && same.Quantity == quantity && same.Note == note ? same.RequestId : Guid.NewGuid();

        SetBusy(true);
        try
        {
            var result = await _staff.ReceiveStockAsync(item.IngredientId, new StockReceiptRequest(requestId, quantity, note));
            if (IsDisposed) return;
            _pendingReceipt = result.Unknown ? new PendingReceipt(requestId, item.IngredientId, quantity, note) : null;
            var message = result.Unknown
                ? result.Message + " Nếu tồn kho chưa tăng, bấm Nhập kho lại với cùng số lượng; hệ thống không cộng hai lần."
                : Message(result, $"Đã nhập {Display.Number(quantity)} {item.UnitName} \"{item.IngredientName}\"; tồn sổ mới {Display.Number(result.Value?.QuantityOnHand ?? 0)} {item.UnitName}.");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task CountAsync()
    {
        var item = SelectedItem;
        if (item is null || _busy) return;
        decimal counted; string? note;
        using (var dialog = new StockCountDialog(item))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            counted = dialog.CountedQty; note = dialog.Note;
        }
        SetBusy(true);
        try
        {
            var result = await _staff.CreateStockCountAsync(item.IngredientId, new CreateStockCountRequest(counted, note));
            if (IsDisposed) return;
            var message = Message(result, result.Value is { } c
                ? $"Đã lập phiếu CNT-{c.CountId} cho \"{c.IngredientName}\": chênh lệch {InventoryText.Difference(c.Difference)} {c.UnitName}, chờ quản lý duyệt. Tồn sổ chưa đổi."
                : "");
            Notify(message, result.Ok);
            if (result.Ok) _filter.SelectedIndex = 0;   // xem ngay phiếu chờ duyệt vừa lập
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task ApproveAsync()
    {
        var count = SelectedCount;
        if (count is null || _busy) return;
        var change = count.Difference == 0 ? "Tồn sổ không đổi."
            : count.Difference > 0 ? $"Tồn sổ sẽ tăng {Display.Number(count.Difference)} {count.UnitName}."
            : $"Tồn sổ sẽ giảm {Display.Number(-count.Difference)} {count.UnitName}.";
        var confirm = MessageBox.Show(this,
            $"Duyệt phiếu CNT-{count.CountId} \"{count.IngredientName}\": tồn sổ lúc đếm {Display.Number(count.BookQty)}, thực đếm {Display.Number(count.CountedQty)} {count.UnitName}. {change} Thao tác ghi vào sổ kho, không hoàn tác được.",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;
        SetBusy(true);
        try
        {
            var result = await _staff.ApproveStockCountAsync(count.CountId);
            if (IsDisposed) return;
            var message = Message(result, $"Đã duyệt phiếu CNT-{count.CountId}; tồn sổ \"{count.IngredientName}\" nay là {Display.Number(result.Value?.CurrentOnHand ?? 0)} {count.UnitName}.");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task RejectAsync()
    {
        var count = SelectedCount;
        if (count is null || _busy) return;
        string reason;
        using (var dialog = new RejectCountDialog(count))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            reason = dialog.Reason;
        }
        SetBusy(true);
        try
        {
            var result = await _staff.RejectStockCountAsync(count.CountId, reason);
            if (IsDisposed) return;
            var message = Message(result, $"Đã từ chối phiếu CNT-{count.CountId}; tồn sổ giữ nguyên.");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }
}
