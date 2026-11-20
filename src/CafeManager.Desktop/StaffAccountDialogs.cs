using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Desktop;

// Hộp thoại thêm/sửa nhân viên (A6). Kiểm tra sớm bằng StaffAccountInput; máy chủ vẫn kiểm tra lại.
// Thêm: tên đăng nhập, họ tên, mật khẩu (nhập hai lần), vai trò. Sửa: chỉ họ tên và vai trò (tên đăng nhập không đổi, mật khẩu không đổi ở đây).
internal sealed class StaffAccountDialog : EditDialog
{
    private sealed record RoleItem(RoleDto Role)
    {
        public override string ToString() => StaffAccountText.RoleLabel(Role);
    }

    private readonly TextBox _userName = new() { Width = 560, MaxLength = StaffAccountInput.MaxUserName, AccessibleName = "Tên đăng nhập" };
    private readonly TextBox _fullName = new() { Width = 560, MaxLength = StaffAccountInput.MaxFullName, AccessibleName = "Họ tên" };
    private readonly TextBox _password = new() { Width = 560, MaxLength = StaffAccountInput.MaxPassword, UseSystemPasswordChar = true, AccessibleName = "Mật khẩu" };
    private readonly TextBox _confirm = new() { Width = 560, MaxLength = StaffAccountInput.MaxPassword, UseSystemPasswordChar = true, AccessibleName = "Nhập lại mật khẩu" };
    private readonly CheckedListBox _roles = new() { Width = 560, Height = 140, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true, AccessibleName = "Vai trò" };
    private readonly StaffAccountDto? _existing;

    public CreateStaffRequest? CreateRequest { get; private set; }
    public UpdateStaffRequest? UpdateRequest { get; private set; }

    public StaffAccountDialog(StaffAccountDto? existing, IReadOnlyList<RoleDto> roles) : base(existing is null ? "Thêm nhân viên" : "Sửa nhân viên")
    {
        _existing = existing;
        foreach (var role in roles)
            _roles.Items.Add(new RoleItem(role), existing?.RoleIds.Contains(role.RoleId) == true);

        if (existing is null)
        {
            AddRow(Ui.Labeled("Tên đăng nhập (chữ không dấu, số, . _ -)", _userName));
            AddRow(Ui.Labeled("Họ tên", _fullName));
            AddRow(Ui.Labeled($"Mật khẩu ({StaffAccountInput.MinPassword} ký tự trở lên)", _password));
            AddRow(Ui.Labeled("Nhập lại mật khẩu", _confirm));
        }
        else
        {
            _fullName.Text = existing.FullName;
            AddRow(new Label { Text = "Tên đăng nhập: " + existing.UserName, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) });
            AddRow(Ui.Labeled("Họ tên", _fullName));
        }
        AddRow(Ui.Labeled("Vai trò (tick các vai trò được giao; quyền là hợp các quyền của vai trò)", _roles));
        AddRow(new Label
        {
            Text = existing is null
                ? "Nhân viên mới đăng nhập được ngay. Màn hình này chưa có chức năng đặt lại mật khẩu, nên hãy ghi lại mật khẩu vừa đặt."
                : "Đổi vai trò có hiệu lực ngay ở thao tác kế tiếp của nhân viên, không cần họ đăng nhập lại.",
            AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, 8),
        });
    }

    protected override bool TryAccept()
    {
        string userName = "";
        if (_existing is null)
        {
            if (!StaffAccountInput.TryUserName(_userName.Text, out userName, out var nameError)) return Fail(_userName, nameError);
        }
        if (!StaffAccountInput.TryFullName(_fullName.Text, out var fullName, out var fullNameError)) return Fail(_fullName, fullNameError);
        if (_existing is null && !StaffAccountInput.TryPassword(_password.Text, _confirm.Text, out var passwordError))
            return Fail(string.IsNullOrEmpty(_password.Text) || _password.Text.Length < StaffAccountInput.MinPassword ? _password : _confirm, passwordError);
        var picked = _roles.CheckedItems.Cast<RoleItem>().Select(i => i.Role.RoleId);
        if (!StaffAccountInput.TryRoles(picked, out var roleIds, out var rolesError)) return Fail(_roles, rolesError);

        if (_existing is null) CreateRequest = new CreateStaffRequest(userName, _password.Text, fullName, roleIds);
        else UpdateRequest = new UpdateStaffRequest(fullName, roleIds);
        return true;
    }
}
