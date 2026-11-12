using System.Globalization;
using System.Text.Json;
using CafeManager.Core;
using CafeManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeManager.Web.Controllers.Web;

// W02: xem lại đơn, nhập giờ nhận và liên hệ, gửi đơn.
// - requestId mới mỗi lần MỞ form; gửi lại do mất phản hồi dùng lại CÙNG requestId (không tạo đơn trùng).
// - Giá chỉ để hiển thị; máy chủ tính lại. Giá khách đã thấy gửi kèm làm expectedUnitPrice để phát hiện giá đổi (409).
[AutoValidateAntiforgeryToken]
public sealed class CheckoutController : Controller
{
    private const string CheckoutPath = "/dat-hang";

    private sealed record Prepared(IActionResult? Redirect, CustomerIdentity? Me, Cart? Cart, CartPageModel? Page);

    private Prepared Prepare()
    {
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return new Prepared(Redirect(CustomerSession.LoginUrl(CheckoutPath)), null, null, null);

        var cart = CartStore.Load(HttpContext.Session);
        var page = CartResolver.Resolve(cart, MenuSource.Current);
        if (page.Lines.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng đang trống. Hãy chọn món trước khi đặt.";
            return new Prepared(Redirect("/gio-hang"), me, cart, page);
        }
        if (page.HasUnavailable)
        {
            TempData["Error"] = "Giỏ hàng có món ngừng bán. Hãy xóa và chọn lại trước khi đặt.";
            return new Prepared(Redirect("/gio-hang"), me, cart, page);
        }
        return new Prepared(null, me, cart, page);
    }

    [HttpGet(CheckoutPath)]
    public IActionResult Index()
    {
        ViewData["Nav"] = "cart";
        var p = Prepare();
        if (p.Redirect is not null) return p.Redirect;

        var suggested = OrderRules.NowLocal().AddMinutes(30);
        var model = BuildModel(p.Page!, Guid.NewGuid(), suggested.ToString(OrderRules.LocalFormat, CultureInfo.InvariantCulture),
            p.Me!.DisplayName, "", DefaultPrices(p.Page!));
        return View(model);
    }

    [HttpPost(CheckoutPath)]
    public async Task<IActionResult> Submit(Guid requestId, string? pickupAtLocal, string? contactName, string? contactPhone)
    {
        ViewData["Nav"] = "cart";
        var p = Prepare();
        if (p.Redirect is not null) return p.Redirect;
        var me = p.Me!;
        var cart = p.Cart!;
        var page = p.Page!;

        var expected = ReadExpectedPrices(page);
        if (expected is null || requestId == Guid.Empty)
        {
            TempData["Error"] = "Dữ liệu đặt hàng không hợp lệ. Vui lòng mở lại trang đặt hàng.";
            return Redirect(CheckoutPath);
        }

        var name = (contactName ?? "").Trim();
        var phone = (contactPhone ?? "").Trim();
        var pickupError = OrderRules.ValidatePickup(pickupAtLocal, out var pickupUtc);
        var nameError = name.Length == 0 ? "Nhập tên liên hệ." : name.Length > 100 ? "Tên liên hệ tối đa 100 ký tự." : null;
        var phoneError = OrderRules.PhoneRegex.IsMatch(phone) ? null : "Số điện thoại không hợp lệ (ví dụ 0901234567).";
        if (pickupError is not null || nameError is not null || phoneError is not null)
        {
            return View("Index", BuildModel(page, requestId, pickupAtLocal ?? "", name, phone, expected,
                pickupError: pickupError, nameError: nameError, phoneError: phoneError));
        }

        var lines = cart.Lines.Select(l => new OrderLineRequest(
            l.VariantId, l.Quantity, expected[l.Id],
            l.Addons.Select(a => new OrderAddonRequest(a.AddonVariantId, a.QuantityPerUnit)).ToList())).ToList();
        var request = new CreateOrderRequest(requestId, pickupUtc, name, phone, lines);

        var result = await CustomerGatewayProvider.Current.CreateOrderAsync(me.UserName, request);
        var model = BuildModel(page, requestId, pickupAtLocal ?? "", name, phone, expected);

        // Mất phản hồi: KHÔNG khẳng định thất bại, KHÔNG tạo mã mới; "Tra cứu kết quả" gửi lại cùng requestId.
        if (result.TimedOut) return View("Timeout", model);

        if (!result.Ok || result.Value is null)
        {
            var error = result.Error;
            if (error?.Code == ApiErrorCodes.PriceChanged && error.PriceChanges is { Count: > 0 } changes)
                return View("PriceChanged", BuildPriceChangedModel(cart, page, model, changes));
            if (error?.Code == ApiErrorCodes.Unauthorized)
                return Redirect(CustomerSession.LoginUrl(CheckoutPath));
            return View("Index", BuildModel(page, requestId, pickupAtLocal ?? "", name, phone, expected,
                errors: new[] { string.IsNullOrWhiteSpace(error?.Message) ? "Không gửi được đơn. Vui lòng thử lại." : error!.Message }));
        }

        CartStore.Save(HttpContext.Session, new Cart());
        CustomerSession.SaveLastOrder(HttpContext, JsonSerializer.Serialize(result.Value));
        return Redirect("/dat-hang/ket-qua");
    }

    [HttpGet("/dat-hang/ket-qua")]
    public IActionResult Result()
    {
        ViewData["Nav"] = "cart";
        var me = CustomerSession.Current(HttpContext);
        if (me is null) return Redirect(CustomerSession.LoginUrl("/don-cua-toi"));
        var json = CustomerSession.LoadLastOrder(HttpContext);
        if (string.IsNullOrEmpty(json)) return Redirect("/don-cua-toi");
        CreateOrderResponse? order;
        try { order = JsonSerializer.Deserialize<CreateOrderResponse>(json); }
        catch (JsonException) { order = null; }
        if (order is null) return Redirect("/don-cua-toi");
        return View(order);
    }

    private static IReadOnlyDictionary<string, decimal> DefaultPrices(CartPageModel page) =>
        page.Lines.ToDictionary(l => l.Id, l => l.VariantPrice);

    private IReadOnlyDictionary<string, decimal>? ReadExpectedPrices(CartPageModel page)
    {
        if (!Request.HasFormContentType) return null;
        var result = new Dictionary<string, decimal>();
        foreach (var line in page.Lines)
        {
            var text = Request.Form["exp_" + line.Id].ToString();
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
                return null;
            result[line.Id] = price;
        }
        return result;
    }

    private static CheckoutViewModel BuildModel(CartPageModel page, Guid requestId, string pickup, string name, string phone,
        IReadOnlyDictionary<string, decimal> expected, IReadOnlyList<string>? errors = null,
        string? pickupError = null, string? nameError = null, string? phoneError = null) => new()
    {
        Cart = page,
        RequestId = requestId,
        PickupAtLocal = pickup,
        MinPickupLocal = OrderRules.NowLocal().ToString(OrderRules.LocalFormat, CultureInfo.InvariantCulture),
        ContactName = name,
        ContactPhone = phone,
        ExpectedPrices = expected,
        Errors = errors ?? Array.Empty<string>(),
        PickupError = pickupError,
        NameError = nameError,
        PhoneError = phoneError,
    };

    // 409 PRICE_CHANGED: hiện giá cũ/mới; chỉ gửi lại khi khách đồng ý. Đơn CHƯA được tạo nên dùng requestId mới.
    private static CheckoutViewModel BuildPriceChangedModel(Cart cart, CartPageModel page, CheckoutViewModel current,
        IReadOnlyList<PriceChange> changes)
    {
        var newBase = new Dictionary<long, decimal>();
        foreach (var c in changes) newBase[c.VariantId] = c.NewPrice;

        var labels = new Dictionary<long, string>();
        foreach (var l in cart.Lines) labels.TryAdd(l.VariantId, l.ProductName + " – " + l.VariantName);

        var pageLines = page.Lines.ToDictionary(l => l.Id);
        var newExpected = new Dictionary<string, decimal>();
        var newSubtotal = 0m;
        foreach (var line in cart.Lines)
        {
            var view = pageLines[line.Id];
            var basePrice = newBase.TryGetValue(line.VariantId, out var nb) ? nb : current.ExpectedPrices[line.Id];
            newExpected[line.Id] = basePrice;
            newSubtotal += (view.UnitPrice - view.VariantPrice + basePrice) * view.Quantity;
        }

        return new CheckoutViewModel
        {
            Cart = page,
            RequestId = Guid.NewGuid(),
            PickupAtLocal = current.PickupAtLocal,
            MinPickupLocal = current.MinPickupLocal,
            ContactName = current.ContactName,
            ContactPhone = current.ContactPhone,
            ExpectedPrices = newExpected,
            NewSubtotal = newSubtotal,
            Changes = changes.Select(c => new PriceChangeView(
                labels.TryGetValue(c.VariantId, out var label) ? label : "Mã " + c.VariantId, c.OldPrice, c.NewPrice)).ToList(),
        };
    }
}
