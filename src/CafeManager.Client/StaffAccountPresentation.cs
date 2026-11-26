using System.Text.RegularExpressions;
using CafeManager.Core;

namespace CafeManager.Client;

// Logic hiển thị và kiểm tra đầu vào cho màn hình Nhân viên (A6, UC16), tách khỏi Windows Forms để kiểm thử được.
// Máy chủ vẫn kiểm tra lại mọi giới hạn dưới đây; đây chỉ để báo lỗi sớm, gần trường nhập.

public static class StaffAccountInput
{
    public const int MinUserName = 3, MaxUserName = 50, MinPassword = 6, MaxPassword = 100, MaxFullName = 200;
    private static readonly Regex UserNameShape = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    public static bool TryUserName(string? text, out string userName, out string error)
    {
        userName = text?.Trim() ?? "";
        if (userName.Length == 0) { error = "Nhập tên đăng nhập."; return false; }
        if (userName.Length < MinUserName || userName.Length > MaxUserName || !UserNameShape.IsMatch(userName))
        {
            error = $"Tên đăng nhập {MinUserName} đến {MaxUserName} ký tự, gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch nối.";
            return false;
        }
        error = ""; return true;
    }

    // Mật khẩu không bị cắt khoảng trắng đầu cuối (người dùng có thể cố ý dùng); hai lần nhập phải khớp từng ký tự.
    public static bool TryPassword(string? password, string? confirm, out string error)
    {
        password ??= "";
        if (password.Length < MinPassword || password.Length > MaxPassword) { error = $"Mật khẩu từ {MinPassword} đến {MaxPassword} ký tự."; return false; }
        if (!string.Equals(password, confirm ?? "", StringComparison.Ordinal)) { error = "Hai lần nhập mật khẩu không giống nhau."; return false; }
        error = ""; return true;
    }

    public static bool TryFullName(string? text, out string fullName, out string error)
    {
        fullName = text?.Trim() ?? "";
        if (fullName.Length == 0) { error = "Nhập họ tên."; return false; }
        if (fullName.Length > MaxFullName) { error = $"Họ tên tối đa {MaxFullName} ký tự."; return false; }
        error = ""; return true;
    }

    public static bool TryRoles(IEnumerable<long> selected, out List<long> roleIds, out string error)
    {
        roleIds = selected.Distinct().ToList();
        if (roleIds.Count == 0) { error = "Chọn ít nhất một vai trò."; return false; }
        error = ""; return true;
    }
}

public static class StaffAccountText
{
    // Tên dễ hiểu của các mã quyền. Mã lạ (quyền mới thêm sau) hiện nguyên mã để không giấu thông tin.
    public static string Permission(string code) => code switch
    {
        Permissions.MenuManage => "Quản lý thực đơn",
        Permissions.OrderReceive => "Tiếp nhận đơn",
        Permissions.PaymentCollect => "Thu ngân",
        Permissions.PreparationWork => "Pha chế",
        Permissions.StaffManage => "Quản lý nhân viên",
        Permissions.InventoryWork => "Kho nguyên liệu",
        Permissions.InventoryApprove => "Duyệt kiểm kê",
        _ => code,
    };

    public static string Status(StaffAccountDto staff) => staff.IsActive ? "Đang hoạt động" : "Đã khóa";

    public static string Roles(StaffAccountDto staff) => staff.RoleNames.Count == 0 ? "(chưa có vai trò)" : string.Join(", ", staff.RoleNames);

    // Dòng hiển thị một vai trò trong hộp chọn: tên vai trò và các quyền nó cấp.
    public static string RoleLabel(RoleDto role) => role.Permissions.Count == 0
        ? role.RoleName + " (chưa cấp quyền nào)"
        : role.RoleName + " (" + string.Join(", ", role.Permissions.Select(Permission)) + ")";

    public static string ToggleCaption(StaffAccountDto? staff) => staff is null || staff.IsActive ? "Khóa…" : "Mở khóa";

    public static string ConfirmToggle(StaffAccountDto staff) => staff.IsActive
        ? $"Khóa tài khoản \"{staff.UserName}\" ({staff.FullName})? Nhân viên sẽ không đăng nhập được và phiên đang mở bị thu hồi ngay. Có thể mở khóa lại sau."
        : $"Mở khóa tài khoản \"{staff.UserName}\" ({staff.FullName})?";

    // Thao tác được phép với dòng đang chọn: không tự khóa mình (máy chủ cũng chặn, đây là báo sớm).
    public static bool CanToggle(StaffAccountDto? staff) => staff is not null && !(staff.IsCurrentUser && staff.IsActive);

    public static string ForSave<T>(ApiResult<T> r, string successText) => r switch
    {
        { Ok: true } => successText,
        { Unknown: true } => r.Message + " Danh sách đã được tải lại; hãy kiểm tra thao tác đã có hiệu lực chưa trước khi làm lại.",
        { StatusCode: 403 } => "Tài khoản không có quyền quản lý nhân viên.",
        { StatusCode: 404 } => "Không tìm thấy nhân viên này (có thể vừa bị thay đổi). Danh sách đã được tải lại.",
        _ => r.Message,
    };

    public static string ForToggle(ApiResult<Empty> r, bool activated) => ForSave(r, activated ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
}
