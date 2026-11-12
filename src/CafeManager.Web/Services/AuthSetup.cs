using System.Security.Claims;
using CafeManager.Core;
using CafeManager.Infrastructure;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace CafeManager.Web.Services;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}

// Quyền được kiểm tra trong CSDL ở MỖI yêu cầu (không tin quyền nằm trong cookie): thu hồi quyền có hiệu lực ngay.
public sealed class PermissionHandler(StaffPermissionReader reader) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null && await reader.HasAsync(userId, requirement.PermissionCode)) context.Succeed(requirement);
    }
}

public static class AuthSetup
{
    public const string Scheme = "Cafe";
    public const string KindClaim = "cafe:kind";

    public static void ConfigureAuthorization(AuthorizationOptions o)
    {
        o.AddPolicy("Customer", p => p.RequireAuthenticatedUser().RequireClaim(KindClaim, UserKinds.Customer));
        foreach (var code in new[] { Permissions.MenuManage, Permissions.OrderReceive })
            o.AddPolicy(code, p => p.RequireAuthenticatedUser().RequireClaim(KindClaim, UserKinds.Staff).AddRequirements(new PermissionRequirement(code)));
    }

    public static ClaimsPrincipal Principal(AccountService.Session session)
    {
        var identity = new ClaimsIdentity(
        [
            new(ClaimTypes.NameIdentifier, session.IdentityUserId), new(ClaimTypes.Name, session.UserName),
            new("cafe:display", session.DisplayName), new(KindClaim, session.Kind),
        ], Scheme);
        return new ClaimsPrincipal(identity);
    }

    private static Task WriteJson(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ApiError(code, message));
    }

    public static void ConfigureCookie(CookieAuthenticationOptions o, CookieSecurePolicy security)
    {
        o.Cookie.Name = ".CafeManager.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = security;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        // API trả JSON 401/403 (Desktop không nhận trang đăng nhập HTML).
        o.Events.OnRedirectToLogin = ctx => ctx.Request.Path.StartsWithSegments("/api")
            ? WriteJson(ctx.HttpContext, 401, ApiErrorCodes.Unauthorized, "Chưa đăng nhập hoặc phiên đã hết hạn.")
            : Task.CompletedTask;
        o.Events.OnRedirectToAccessDenied = ctx => ctx.Request.Path.StartsWithSegments("/api")
            ? WriteJson(ctx.HttpContext, 403, ApiErrorCodes.Forbidden, "Không đủ quyền thực hiện thao tác này.")
            : Task.CompletedTask;
        // Kiểm tra lại tài khoản ở mỗi yêu cầu: khóa tài khoản hoặc vô hiệu hóa nhân viên làm phiên mất hiệu lực ngay.
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var kind = ctx.Principal?.FindFirstValue(KindClaim);
            var accounts = ctx.HttpContext.RequestServices.GetRequiredService<AccountService>();
            var session = userId == null || kind == null ? null : await accounts.RevalidateAsync(userId, kind, ctx.HttpContext.RequestAborted);
            if (session == null)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(Scheme);
            }
        };
    }
}
