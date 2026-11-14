using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// D06 – Quản lý món, biến thể, giá và công thức (UC17). Mọi thao tác gọi API qua StaffApi; không truy cập CSDL.
// Quy tắc hiển thị: biến thể chỉ bán được khi có giá và công thức hiệu lực; cột Trạng thái nói rõ còn thiếu gì.
// Hệ thống không xóa món (đã có chứng từ thì chỉ ngừng bán).
public sealed class MenuManagementForm : Form
{
    private readonly StaffApi _staff;
    private readonly DataGridView _grid = new() { Name = "menu" };
    private readonly TextBox _search = new() { Name = "search", Width = 240, AccessibleName = "Tìm theo tên món, biến thể hoặc danh mục" };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _reload = Ui.Button("Tải lại (F5)");
    private readonly Button _addProduct = Ui.Button("Thêm món");
    private readonly Button _editProduct = Ui.Button("Sửa món");
    private readonly Button _addVariant = Ui.Button("Thêm biến thể");
    private readonly Button _price = Ui.Button("Đổi giá");
    private readonly Button _recipe = Ui.Button("Công thức");
    private readonly Button _activate = Ui.Button("Bật bán", primary: true);
    private readonly Button _deactivate = Ui.Button("Ngừng bán");
    private StaffMenuResponse _menu = new([], [], []);
    private (long ProductId, long? VariantId)? _selectAfter;
    private bool _busy;

    public MenuManagementForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Quản lý món (D06)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1240, 680);
        MinimumSize = new Size(1000, 520);
        KeyPreview = true;

        Ui.ConfigureGrid(_grid, "Danh sách món và biến thể");
        _grid.Columns.Add(Ui.Column("Món", 30, minWidth: 140));
        _grid.Columns.Add(Ui.Column("Danh mục", 12, auto: true));
        _grid.Columns.Add(Ui.Column("Biến thể", 14, minWidth: 100));
        _grid.Columns.Add(Ui.Column("Loại", 8, auto: true));
        _grid.Columns.Add(Ui.Column("Giá bán", 11, alignRight: true, auto: true));
        _grid.Columns.Add(Ui.Column("Hiệu lực từ", 12, auto: true));
        _grid.Columns.Add(Ui.Column("Công thức (một ly/phần)", 40, minWidth: 180));
        _grid.Columns.Add(Ui.Column("Trạng thái", 20, auto: true));
        _grid.SelectionChanged += (_, _) => UpdateButtons();

        _search.TextChanged += (_, _) => Bind();
        _reload.Click += async (_, _) => await ReloadAsync();
        _addProduct.Click += async (_, _) => await AddProductAsync();
        _editProduct.Click += async (_, _) => await EditProductAsync();
        _addVariant.Click += async (_, _) => await AddVariantAsync();
        _price.Click += async (_, _) => await ChangePriceAsync();
        _recipe.Click += async (_, _) => await ChangeRecipeAsync();
        _activate.Click += async (_, _) => await ActivateAsync();
        _deactivate.Click += async (_, _) => await DeactivateAsync();
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await ChangePriceAsync(); };

        var searchBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 0) };
        searchBar.Controls.Add(new Label { Text = "Tìm:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) });
        searchBar.Controls.Add(_search);
        searchBar.Controls.Add(_reload);

        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4), WrapContents = true };
        foreach (var button in new[] { _addProduct, _editProduct, _addVariant, _price, _recipe, _activate, _deactivate }) actions.Controls.Add(button);

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        gridHost.Controls.Add(_grid);

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 4, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(searchBar);
        root.Controls.Add(actions);
        root.Controls.Add(gridHost);
        root.Controls.Add(_status);
        Controls.Add(root);

        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; await ReloadAsync(); }
        };
        Shown += async (_, _) => await ReloadAsync();
        UpdateButtons();
    }

    private MenuRow? Selected => _grid.CurrentRow?.Tag as MenuRow;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var row = Selected;
        _reload.Enabled = !_busy;
        _addProduct.Enabled = !_busy && _menu.Categories.Count > 0;
        _editProduct.Enabled = !_busy && row is not null;
        _addVariant.Enabled = !_busy && row is not null;
        _price.Enabled = !_busy && row?.VariantId is not null;
        _recipe.Enabled = !_busy && row?.VariantId is not null;
        _activate.Enabled = !_busy && row is { CanActivate: true };
        _deactivate.Enabled = !_busy && row is { CanDeactivate: true };
    }

    private async Task ReloadAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Lỗi tải thì giữ danh sách cũ và báo rõ (không xóa trắng).
    private async Task LoadAsync()
    {
        var result = await _staff.MenuAsync();
        if (IsDisposed) return;
        if (!result.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(result, "thực đơn") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        _menu = result.Value!;
        Bind();
        var variants = _menu.Products.Sum(p => p.Variants.Count);
        Ui.SetStatus(_status, $"{_menu.Products.Count} món, {variants} biến thể. Cập nhật lúc {DateTime.Now:HH:mm:ss}.");
    }

    private void Bind()
    {
        var keep = _selectAfter ?? (Selected is { } s ? (s.ProductId, s.VariantId) : ((long, long?)?)null);
        _selectAfter = null;
        _grid.Rows.Clear();
        var rows = MenuRows.Flatten(_menu, _search.Text);
        foreach (var r in rows)
        {
            var index = _grid.Rows.Add(r.ProductName, r.CategoryName, r.VariantName, VariantTypeText(r.VariantType),
                r.Price is null ? "" : Display.Money(r.Price.Value), Display.TimeShort(r.PriceFrom), r.RecipeSummary, r.Status);
            _grid.Rows[index].Tag = r;
            if (!r.ProductActive || (r.VariantId is not null && !r.VariantActive)) _grid.Rows[index].DefaultCellStyle.ForeColor = SystemColors.GrayText;
        }
        if (_grid.Rows.Count > 0)
        {
            var at = keep is { } k ? rows.ToList().FindIndex(r => r.ProductId == k.ProductId && r.VariantId == k.VariantId) : -1;
            var row = _grid.Rows[Math.Max(at, 0)];
            _grid.CurrentCell = row.Cells[0];
            row.Selected = true;
        }
        UpdateButtons();
    }

    private static string VariantTypeText(string type) => type switch { "Drink" => "Đồ uống", "Addon" => "Tùy chọn", _ => type };

    // Gọi API, báo kết quả, tải lại khi cần. select: chọn lại dòng vừa tạo sau khi tải.
    private async Task RunAsync<T>(Func<Task<ApiResult<T>>> call, string success, Func<ApiResult<T>, (long, long?)?>? select = null)
    {
        if (_busy) return;
        SetBusy(true);
        try
        {
            var result = await call();
            if (IsDisposed) return;
            var message = StaffMessages.ForMenu(result, success);
            Ui.SetStatus(_status, message, error: !result.Ok);
            if (!result.Ok) MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (result.Ok && select?.Invoke(result) is { } key) _selectAfter = key;
            if (StaffMessages.ReloadAfter(result))
            {
                await LoadAsync();
                if (!IsDisposed && _status.ForeColor != Color.Firebrick) Ui.SetStatus(_status, message, error: !result.Ok);
            }
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task AddProductAsync()
    {
        using var dialog = new ProductDialog(_menu.Categories, null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _staff.SaveProductAsync(null, dialog.Result!),
            "Đã thêm món. Bước tiếp theo: Thêm biến thể, đặt giá và công thức, rồi Bật bán.",
            r => (r.Value!.Id, null));
    }

    private async Task EditProductAsync()
    {
        if (Selected is not { } row) return;
        using var dialog = new ProductDialog(_menu.Categories, row);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _staff.SaveProductAsync(row.ProductId, dialog.Result!), "Đã lưu thông tin món.", _ => (row.ProductId, row.VariantId));
    }

    private async Task AddVariantAsync()
    {
        if (Selected is not { } row) return;
        using var dialog = new VariantDialog(row.ProductName, null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _staff.SaveVariantAsync(null, new VariantUpsertRequest(row.ProductId, dialog.VariantName, dialog.VariantType, false)),
            "Đã thêm biến thể (chưa bán). Đặt giá và công thức, rồi bấm Bật bán.", r => (row.ProductId, r.Value!.Id));
    }

    private async Task ChangePriceAsync()
    {
        if (Selected is not { VariantId: { } variantId } row) return;
        using var dialog = new PriceDialog($"{row.ProductName} – {row.VariantName}", row.Price);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _staff.SetPriceAsync(variantId, dialog.Price, dialog.EffectiveFromUtc),
            $"Đã đặt giá {Display.Money(dialog.Price)} có hiệu lực từ {Display.Time(dialog.EffectiveFromUtc)}.", _ => (row.ProductId, variantId));
    }

    private async Task ChangeRecipeAsync()
    {
        if (Selected is not { VariantId: { } variantId } row) return;
        using var dialog = new RecipeDialog($"{row.ProductName} – {row.VariantName}", _menu.Ingredients, row.Recipe);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => _staff.SetRecipeAsync(variantId, dialog.EffectiveFromUtc, dialog.Lines),
            $"Đã lưu công thức ({dialog.Lines.Count} nguyên liệu) có hiệu lực từ {Display.Time(dialog.EffectiveFromUtc)}.", _ => (row.ProductId, variantId));
    }

    private async Task ActivateAsync()
    {
        if (Selected is not { VariantId: { } variantId, CanActivate: true } row) return;
        await RunAsync(() => _staff.SaveVariantAsync(variantId, new VariantUpsertRequest(row.ProductId, row.VariantName, row.VariantType, true)),
            $"Đã bật bán {row.ProductName} – {row.VariantName}; khách thấy món này trên Web sau tối đa vài giây.", _ => (row.ProductId, variantId));
    }

    private async Task DeactivateAsync()
    {
        if (Selected is not { VariantId: { } variantId, CanDeactivate: true } row) return;
        var confirm = MessageBox.Show(this,
            $"Ngừng bán {row.ProductName} – {row.VariantName}?{Environment.NewLine}Đơn đã đặt trước đó không đổi; có thể bật bán lại sau.",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;
        await RunAsync(() => _staff.DeactivateAsync(variantId), $"Đã ngừng bán {row.ProductName} – {row.VariantName}.", _ => (row.ProductId, variantId));
    }
}
