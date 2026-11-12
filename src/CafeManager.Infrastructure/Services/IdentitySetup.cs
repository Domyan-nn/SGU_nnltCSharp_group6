using Microsoft.AspNetCore.Identity;

namespace CafeManager.Infrastructure.Services;

// Cấu hình Identity dùng chung cho Web và kiểm thử: mật khẩu tối thiểu 6 ký tự (khớp giao diện đăng ký),
// khóa tài khoản tạm thời sau 5 lần nhập sai liên tiếp.
public static class IdentitySetup
{
    public static void Configure(IdentityOptions o)
    {
        o.Password.RequiredLength = 6;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.User.RequireUniqueEmail = false;
    }
}
