using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// C1 – Quản lý danh mục: xem, tìm, thêm, sửa, xóa (quyền MENU_MANAGE). Xóa luôn hỏi xác nhận và báo rõ khi danh mục còn món:
// máy chủ trả 409 và form hướng dẫn ngừng sử dụng thay vì xóa. Mọi thao tác gọi API qua StaffApi.
public sealed class CategoryForm : Form
{
    private readonly StaffApi _staff;
    private readonly DataGridView _grid = new() { Name = "categories" };
    private readonly TextBox _search = new() { Name = "search", Width = 240, AccessibleName = "Tìm danh mục", PlaceholderText = "Tìm theo tên…" };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _find = Ui.Button("Tìm");
    private readonly Button _refresh = Ui.Button("Tải lại (F5)");
    private readonly Button _add = Ui.Button("Thêm…", primary: true);
    private readonly Button _edit = Ui.Button("Sửa…");
    private readonly Button _delete = Ui.Button("Xóa…");
    private List<CategoryDto> _data = [];
    private bool _busy;

    public CategoryForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Quản lý danh mục (C1)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 560);
        MinimumSize = new Size(720, 420);
        KeyPreview = true;

        Ui.ConfigureGrid(_grid, "Danh sách danh mục");
        _grid.Columns.Add(Ui.Column("Tên danh mục", 60, minWidth: 180));
        _grid.Columns.Add(Ui.Column("Số món", 12, alignRight: true, auto: true));
        _grid.Columns.Add(Ui.Column("Trạng thái", 18, auto: true));
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await EditAsync(); };

        _find.Click += async (_, _) => await RefreshAsync();
        _refresh.Click += async (_, _) => { _search.Clear(); await RefreshAsync(); };
        _add.Click += async (_, _) => await AddAsync();
        _edit.Click += async (_, _) => await EditAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; await RefreshAsync(); }
        };

        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };
        toolbar.Controls.Add(_search);
        toolbar.Controls.Add(_find);
        toolbar.Controls.Add(_refresh);
        toolbar.Controls.Add(_add);
        toolbar.Controls.Add(_edit);
        toolbar.Controls.Add(_delete);

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 4) };
        gridHost.Controls.Add(_grid);

        var root = new TableLayoutPanel { ColumnCount = 1, RowCount = 3, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(toolbar);
        root.Controls.Add(gridHost);
        root.Controls.Add(_status);
        Controls.Add(root);

        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.F5) { e.Handled = true; await RefreshAsync(); }
        };
        Shown += async (_, _) => await RefreshAsync();
        UpdateButtons();
    }

    private CategoryDto? Selected =>
        _grid.CurrentRow?.Tag is long id ? _data.FirstOrDefault(c => c.CategoryId == id) : null;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var has = Selected is not null;
        _find.Enabled = !_busy;
        _refresh.Enabled = !_busy;
        _add.Enabled = !_busy;
        _edit.Enabled = !_busy && has;
        _delete.Enabled = !_busy && has;
    }

    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Tải danh sách; lỗi thì giữ danh sách cũ và báo rõ.
    private async Task LoadAsync()
    {
        var selectedId = Selected?.CategoryId;
        var result = await _staff.CategoriesAsync(_search.Text);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(result, "danh sách danh mục") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            return;
        }
        _data = result.Value!;
        Bind(selectedId);
        Ui.SetStatus(_status, $"Có {_data.Count} danh mục" + (string.IsNullOrWhiteSpace(_search.Text) ? "" : $" khớp \"{_search.Text.Trim()}\"") + $". Cập nhật lúc {DateTime.Now:HH:mm:ss}.");
    }

    private void Bind(long? selectedId)
    {
        _grid.Rows.Clear();
        foreach (var c in _data)
        {
            var index = _grid.Rows.Add(c.CategoryName, c.ProductCount, c.IsActive ? "Đang dùng" : "Ngừng sử dụng");
            _grid.Rows[index].Tag = c.CategoryId;
        }
        if (_grid.Rows.Count > 0)
        {
            var keep = selectedId is null ? -1 : _data.FindIndex(c => c.CategoryId == selectedId);
            var row = _grid.Rows[Math.Max(keep, 0)];
            _grid.CurrentCell = row.Cells[0];
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
        if (_status.ForeColor == Color.Firebrick)
            Ui.SetStatus(_status, message + " " + _status.Text.Replace("Lỗi: ", ""), error: true);
        else
            Ui.SetStatus(_status, message + " " + _status.Text, error: !ok);
    }

    private Task AddAsync() => SaveAsync(null);

    private Task EditAsync() => Selected is { } c ? SaveAsync(c) : Task.CompletedTask;

    private async Task SaveAsync(CategoryDto? existing)
    {
        if (_busy) return;
        CategoryUpsertRequest request;
        using (var dialog = new CategoryDialog(existing))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            request = dialog.Result!;
        }
        SetBusy(true);
        try
        {
            var result = await _staff.SaveCategoryAsync(existing?.CategoryId, request);
            if (IsDisposed) return;
            var message = StaffMessages.ForMenu(result, existing is null ? "Đã thêm danh mục." : "Đã lưu danh mục.");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task DeleteAsync()
    {
        var category = Selected;
        if (category is null || _busy) return;
        if (category.ProductCount > 0)
        {
            Notify(CatalogMessages.BlockedByProducts(category.CategoryName, category.ProductCount), false);
            return;
        }
        var confirm = MessageBox.Show(this, $"Xóa danh mục \"{category.CategoryName}\"? Thao tác này không hoàn tác được.",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        SetBusy(true);
        try
        {
            var result = await _staff.DeleteCategoryAsync(category.CategoryId);
            if (IsDisposed) return;
            var message = CatalogMessages.ForDelete(result, $"danh mục \"{category.CategoryName}\"");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }
}
