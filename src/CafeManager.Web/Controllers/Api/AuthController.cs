using System.Security.Claims;
using CafeManager.Core;
using CafeManager.Infrastructure.Services;
using CafeManager.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Api;

public abstract class ApiBase : ControllerBase
{
    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AppException(ApiErrorCodes.Unauthorized, "Chưa đăng nhập.");
}

// Xác thực (UC18, UC19). Đăng nhập, đăng ký và đăng xuất không có phiên để bị giả mạo nên không đòi token chống giả mạo;
// mọi yêu cầu ghi khác phải kèm header X-CSRF-TOKEN lấy từ GET /api/auth/antiforgery SAU khi đăng nhập
// (token gắn với danh tính hiện tại).
[ApiController]
[Route("api/auth")]
public sealed class AuthController(AccountService accounts, IAntiforgery antiforgery) : ApiBase
{
    [HttpPost("login"), AllowAnonymous, IgnoreAntiforgeryToken]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var session = await accounts.LoginAsync(request, null, ct);
        await HttpContext.SignInAsync(AuthSetup.Scheme, AuthSetup.Principal(session), new AuthenticationProperties { IsPersistent = false });
        return session.ToResponse();
    }

    [HttpPost("logout"), AllowAnonymous, IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSetup.Scheme);
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<LoginResponse>> Me(CancellationToken ct)
    {
        var kind = User.FindFirstValue(AuthSetup.KindClaim)!;
        var session = await accounts.RevalidateAsync(UserId, kind, ct) ?? throw new AppException(ApiErrorCodes.Unauthorized, "Phiên không còn hiệu lực.");
        return session.ToResponse();
    }

    [HttpPost("register-customer"), AllowAnonymous, IgnoreAntiforgeryToken]
    public async Task<IActionResult> RegisterCustomer(RegisterCustomerRequest request, CancellationToken ct)
    {
        await accounts.RegisterCustomerAsync(request, ct);
        return StatusCode(201);
    }

    [HttpGet("antiforgery"), AllowAnonymous]
    public ActionResult<AntiforgeryTokenResponse> Antiforgery() =>
        new AntiforgeryTokenResponse(antiforgery.GetAndStoreTokens(HttpContext).RequestToken ?? "");
}
