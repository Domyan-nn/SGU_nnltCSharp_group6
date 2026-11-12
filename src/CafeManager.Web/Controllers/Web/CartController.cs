using System.Globalization;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Web;

// W01: giỏ hàng (session). Mọi thao tác ghi là POST có antiforgery; dữ liệu từ trình duyệt đều được kiểm tra lại ở đây.
[AutoValidateAntiforgeryToken]
public sealed class CartController : Controller
{
    private const string SessionMissing =
        "Giỏ hàng chưa bật: Program.cs thiếu AddSession/UseSession (xem docs/patch-session-for-A.md).";

    [HttpGet("/gio-hang")]
    public IActionResult Index()
    {
        ViewData["Nav"] = "cart";
        var cart = CartStore.IsAvailable(HttpContext) ? CartStore.Load(HttpContext.Session) : new Cart();
        if (!CartStore.IsAvailable(HttpContext)) TempData["Error"] = SessionMissing;
        return View(CartResolver.Resolve(cart, MenuSource.Current));
    }

    [HttpPost("/gio-hang/them")]
    public IActionResult Add(long variantId, string? quantity, string? returnUrl)
    {
        if (!CartStore.IsAvailable(HttpContext)) return Fail(returnUrl, SessionMissing);

        if (!Request.HasFormContentType) return Fail(returnUrl, "Yêu cầu không hợp lệ.");

        var addonTexts = new Dictionary<long, string?>();
        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("addon_", StringComparison.Ordinal)) continue;
            if (!long.TryParse(key.AsSpan("addon_".Length), NumberStyles.None, CultureInfo.InvariantCulture, out var addonId))
                return Fail(returnUrl, "Dữ liệu tùy chọn không hợp lệ.");
            addonTexts[addonId] = Request.Form[key].ToString();
        }

        var (line, error) = CartRules.TryBuildLine(MenuSource.Current, variantId, quantity, addonTexts);
        if (line is null) return Fail(returnUrl, error ?? "Không thêm được vào giỏ.");

        var cart = CartStore.Load(HttpContext.Session);
        var addError = CartRules.AddToCart(cart, line);
        if (addError is not null) return Fail(returnUrl, addError);

        CartStore.Save(HttpContext.Session, cart);
        TempData["Message"] = $"Đã thêm {line.Quantity} × {line.ProductName} ({line.VariantName}) vào giỏ.";
        return LocalRedirect(SafeTarget(returnUrl));
    }

    [HttpPost("/gio-hang/cap-nhat")]
    public IActionResult Update(string? id, string? quantity)
    {
        if (!CartStore.IsAvailable(HttpContext)) return FailToCart(SessionMissing);
        var cart = CartStore.Load(HttpContext.Session);
        var line = cart.Lines.FirstOrDefault(l => l.Id == id);
        if (line is null) return FailToCart("Dòng này không còn trong giỏ.");
        var error = CartRules.ValidateQuantity(quantity, out var parsed);
        if (error is not null) return FailToCart(error);

        line.Quantity = parsed;
        CartStore.Save(HttpContext.Session, cart);
        TempData["Message"] = "Đã cập nhật số lượng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/gio-hang/xoa")]
    public IActionResult Remove(string? id)
    {
        if (!CartStore.IsAvailable(HttpContext)) return FailToCart(SessionMissing);
        var cart = CartStore.Load(HttpContext.Session);
        var removed = cart.Lines.RemoveAll(l => l.Id == id);
        if (removed == 0) return FailToCart("Dòng này không còn trong giỏ.");

        CartStore.Save(HttpContext.Session, cart);
        TempData["Message"] = "Đã xóa khỏi giỏ.";
        return RedirectToAction(nameof(Index));
    }

    private string SafeTarget(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

    private IActionResult Fail(string? returnUrl, string message)
    {
        TempData["Error"] = message;
        return LocalRedirect(SafeTarget(returnUrl));
    }

    private IActionResult FailToCart(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
