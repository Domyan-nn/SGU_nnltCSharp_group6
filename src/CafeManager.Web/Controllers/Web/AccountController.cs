using CafeManager.Core;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Web;

// W00: đăng ký và đăng nhập khách. Sai mật khẩu hoặc tài khoản khóa thì KHÔNG cấp phiên.
[AutoValidateAntiforgeryToken]
public sealed class AccountController : Controller
{
    private const string SessionMissing =
        "Chưa bật session trên máy chủ nên chưa đăng nhập được (xem docs/patch-session-for-A.md).";

    [HttpGet("/tai-khoan")]
    public IActionResult Index()
    {
        ViewData["Nav"] = "account";
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return Redirect("/tai-khoan/dang-nhap");
        return View(me);
    }

    [HttpGet("/tai-khoan/dang-nhap")]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["Nav"] = "account";
        if (CustomerSession.Current(HttpContext) is not null) return LocalRedirect(SafeTarget(returnUrl));
        if (!CartStore.IsAvailable(HttpContext)) ModelState.AddModelError(string.Empty, SessionMissing);
        return View(new LoginForm { ReturnUrl = returnUrl });
    }

    [HttpPost("/tai-khoan/dang-nhap")]
    public async Task<IActionResult> LoginPost(LoginForm form)
    {
        ViewData["Nav"] = "account";
        if (!CartStore.IsAvailable(HttpContext))
        {
            ModelState.AddModelError(string.Empty, SessionMissing);
            return View("Login", form);
        }
        if (!ModelState.IsValid) return View("Login", form);

        var result = await CustomerGatewayProvider.Current.LoginAsync(new LoginRequest(form.UserName.Trim(), form.Password));
        if (result.TimedOut)
        {
            ModelState.AddModelError(string.Empty, "Không kết nối được máy chủ. Vui lòng thử lại sau.");
            return View("Login", form);
        }
        if (!result.Ok || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error?.Code switch
            {
                ApiErrorCodes.Unauthorized => "Tên đăng nhập hoặc mật khẩu không đúng.",
                ApiErrorCodes.Forbidden => "Tài khoản đã bị khóa. Vui lòng liên hệ quán để được hỗ trợ.",
                _ => result.Error?.Message ?? "Không đăng nhập được. Vui lòng thử lại.",
            });
            return View("Login", form);
        }
        if (result.Value.UserKind != UserKinds.Customer)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản này không phải tài khoản khách. Nhân viên đăng nhập bằng ứng dụng quản lý.");
            return View("Login", form);
        }

        CustomerSession.SignIn(HttpContext, form.UserName.Trim(), result.Value.DisplayName);
        return LocalRedirect(SafeTarget(form.ReturnUrl));
    }

    [HttpGet("/tai-khoan/dang-ky")]
    public IActionResult Register()
    {
        ViewData["Nav"] = "account";
        return View(new RegisterForm());
    }

    [HttpPost("/tai-khoan/dang-ky")]
    public async Task<IActionResult> RegisterPost(RegisterForm form)
    {
        ViewData["Nav"] = "account";
        if (!ModelState.IsValid) return View("Register", form);

        var request = new RegisterCustomerRequest(form.UserName.Trim(), form.Password, form.FullName.Trim(), form.Phone.Trim());
        var result = await CustomerGatewayProvider.Current.RegisterAsync(request);
        if (result.TimedOut)
        {
            ModelState.AddModelError(string.Empty, "Không kết nối được máy chủ. Vui lòng thử lại sau.");
            return View("Register", form);
        }
        if (!result.Ok)
        {
            if (result.Error?.Code == ApiErrorCodes.Conflict)
                ModelState.AddModelError(nameof(RegisterForm.UserName), "Tên đăng nhập đã được dùng.");
            else
                ModelState.AddModelError(string.Empty, result.Error?.Message ?? "Không đăng ký được. Vui lòng thử lại.");
            return View("Register", form);
        }

        TempData["Message"] = "Đăng ký thành công. Hãy đăng nhập.";
        return Redirect("/tai-khoan/dang-nhap");
    }

    [HttpPost("/tai-khoan/dang-xuat")]
    public IActionResult Logout()
    {
        if (CartStore.IsAvailable(HttpContext)) CustomerSession.SignOut(HttpContext);
        return Redirect("/");
    }

    private string SafeTarget(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}
