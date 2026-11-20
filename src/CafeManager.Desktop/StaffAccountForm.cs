using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// A6 – Quản lý nhân viên và phân quyền (UC16, quyền STAFF_MANAGE): xem, tìm, thêm, sửa họ tên và vai trò, khóa/mở khóa.
// Không có nút xóa vì nhân viên đã có chứng từ tham chiếu. Tự khóa mình và khóa quản trị viên cuối cùng bị máy chủ từ chối (409).
// Mọi thao tác gọi API qua StaffApi.
public sealed class StaffAccountForm : Form
{
    private readonly StaffApi _staff;
    private readonly DataGridView _grid = new() { Name = "staff" };
    private readonly TextBox _search = new() { Name = "search", Width = 240, AccessibleName = "Tìm nhân viên", PlaceholderText = "Tìm theo tên đăng nhập hoặc họ tên…" };
    private readonly Label _status = Ui.StatusLabel("status");
    private readonly Button _find = Ui.Button("Tìm");
    private readonly Button _refresh = Ui.Button("Tải lại (F5)");
    private readonly Button _add = Ui.Button("Thêm…", primary: true);
    private readonly Button _edit = Ui.Button("Sửa…");
    private readonly Button _toggle = Ui.Button("Khóa…");
    private List<StaffAccountDto> _data = [];
    private List<RoleDto> _roles = [];
    private bool _busy;

    public StaffAccountForm(ApiClient api)
    {
        _staff = new StaffApi(api);
        Text = "Quản lý nhân viên và phân quyền (A6)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(960, 560);
        MinimumSize = new Size(760, 420);
        KeyPreview = true;

        Ui.ConfigureGrid(_grid, "Danh sách nhân viên");
        _grid.Columns.Add(Ui.Column("Tên đăng nhập", 24, minWidth: 240));   // đủ chỗ cho tên dài kèm hậu tố " (bạn)"
        _grid.Columns.Add(Ui.Column("Họ tên", 28, minWidth: 140));
        _grid.Columns.Add(Ui.Column("Vai trò", 32, minWidth: 140));
        _grid.Columns.Add(Ui.Column("Trạng thái", 14, auto: true));
        // CurrentRow có thể đổi sau SelectionChanged, nên nghe cả hai sự kiện để nút Khóa/Mở khóa luôn theo dòng đang chọn.
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CurrentCellChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await EditAsync(); };

        _find.Click += async (_, _) => await RefreshAsync();
        _refresh.Click += async (_, _) => { _search.Clear(); await RefreshAsync(); };
        _add.Click += async (_, _) => await AddAsync();
        _edit.Click += async (_, _) => await EditAsync();
        _toggle.Click += async (_, _) => await ToggleAsync();
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
        toolbar.Controls.Add(_toggle);

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

    private StaffAccountDto? Selected =>
        _grid.CurrentRow?.Tag is long id ? _data.FirstOrDefault(s => s.StaffId == id) : null;

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var selected = Selected;
        _find.Enabled = !_busy;
        _refresh.Enabled = !_busy;
        _add.Enabled = !_busy && _roles.Count > 0;
        _edit.Enabled = !_busy && selected is not null && _roles.Count > 0;
        _toggle.Text = StaffAccountText.ToggleCaption(selected);
        _toggle.Enabled = !_busy && StaffAccountText.CanToggle(selected);
    }

    private async Task RefreshAsync()
    {
        if (_busy || IsDisposed) return;
        SetBusy(true);
        try { await LoadAsync(); }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    // Tải danh sách nhân viên và danh sách vai trò; lỗi thì giữ dữ liệu cũ và báo rõ.
    private async Task LoadAsync()
    {
        var selectedId = Selected?.StaffId;
        var accounts = await _staff.StaffAccountsAsync(_search.Text);
        var roles = await _staff.RolesAsync();
        if (IsDisposed) return;
        if (!roles.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(roles, "danh sách vai trò") + " Chưa thể thêm hoặc sửa nhân viên.", error: true);
            _roles = [];
        }
        else _roles = roles.Value!;
        if (!accounts.Ok)
        {
            Ui.SetStatus(_status, StaffMessages.ForLoad(accounts, "danh sách nhân viên") + " Đang hiển thị dữ liệu của lần tải trước.", error: true);
            UpdateButtons();
            return;
        }
        _data = accounts.Value!;
        Bind(selectedId);
        var note = _roles.Count == 0 && roles.Ok ? " Chưa có vai trò nào trong CSDL nên chưa thêm được nhân viên." : "";
        if (roles.Ok)
            Ui.SetStatus(_status, $"Có {_data.Count} nhân viên" + (string.IsNullOrWhiteSpace(_search.Text) ? "" : $" khớp \"{_search.Text.Trim()}\"")
                + $". Cập nhật lúc {DateTime.Now:HH:mm:ss}." + note, error: note.Length > 0);
    }

    private void Bind(long? selectedId)
    {
        _grid.Rows.Clear();
        foreach (var s in _data)
        {
            var index = _grid.Rows.Add(s.UserName + (s.IsCurrentUser ? " (bạn)" : ""), s.FullName, StaffAccountText.Roles(s), StaffAccountText.Status(s));
            _grid.Rows[index].Tag = s.StaffId;
            if (!s.IsActive) _grid.Rows[index].DefaultCellStyle.ForeColor = SystemColors.GrayText;
        }
        if (_grid.Rows.Count > 0)
        {
            var keep = selectedId is null ? -1 : _data.FindIndex(s => s.StaffId == selectedId);
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

    private async Task AddAsync()
    {
        if (_busy) return;
        CreateStaffRequest request;
        using (var dialog = new StaffAccountDialog(null, _roles))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            request = dialog.CreateRequest!;
        }
        SetBusy(true);
        try
        {
            var result = await _staff.CreateStaffAsync(request);
            if (IsDisposed) return;
            var message = StaffAccountText.ForSave(result, $"Đã thêm nhân viên \"{request.UserName}\". Họ đăng nhập được ngay bằng mật khẩu vừa đặt.");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task EditAsync()
    {
        var staff = Selected;
        if (staff is null || _busy || _roles.Count == 0) return;
        UpdateStaffRequest request;
        using (var dialog = new StaffAccountDialog(staff, _roles))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            request = dialog.UpdateRequest!;
        }
        SetBusy(true);
        try
        {
            var result = await _staff.UpdateStaffAsync(staff.StaffId, request);
            if (IsDisposed) return;
            var message = StaffAccountText.ForSave(result, $"Đã lưu nhân viên \"{staff.UserName}\".");
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task ToggleAsync()
    {
        var staff = Selected;
        if (staff is null || _busy || !StaffAccountText.CanToggle(staff)) return;
        var confirm = MessageBox.Show(this, StaffAccountText.ConfirmToggle(staff), Text,
            MessageBoxButtons.YesNo, staff.IsActive ? MessageBoxIcon.Warning : MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        var activate = !staff.IsActive;
        SetBusy(true);
        try
        {
            var result = await _staff.SetStaffActiveAsync(staff.StaffId, activate);
            if (IsDisposed) return;
            var message = StaffAccountText.ForToggle(result, activate);
            Notify(message, result.Ok);
            if (StaffMessages.ReloadAfter(result)) await AfterAction(message, result.Ok);
        }
        finally { if (!IsDisposed) SetBusy(false); }
    }
}
