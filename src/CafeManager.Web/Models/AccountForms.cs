using System.ComponentModel.DataAnnotations;

namespace CafeManager.Web.Models;

public sealed class LoginForm
{
    [Required(ErrorMessage = "Nhập tên đăng nhập.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [StringLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự.")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterForm
{
    [Required(ErrorMessage = "Nhập tên đăng nhập.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch nối.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Nhập lại mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0901234567).")]
    public string Phone { get; set; } = "";
}
