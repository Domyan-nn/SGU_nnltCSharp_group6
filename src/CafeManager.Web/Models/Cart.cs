using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CafeManager.Core;
using Microsoft.AspNetCore.Http.Features;

namespace CafeManager.Web.Models;

// Giỏ hàng lưu trong session CHỈ gồm mã biến thể, số lượng và nhãn hiển thị. Không lưu giá: giá luôn tính lại từ
// thực đơn khi hiển thị, và máy chủ sẽ tính lại một lần nữa khi gửi đơn (T5). Không tin giá từ trình duyệt.

public sealed class CartAddon
{
    public long AddonVariantId { get; set; }
    public int QuantityPerUnit { get; set; }
}

public sealed class CartLine
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public long VariantId { get; set; }
    public string ProductName { get; set; } = "";
    public string VariantName { get; set; } = "";
    public int Quantity { get; set; }
    public List<CartAddon> Addons { get; set; } = new();
}

public sealed class Cart
{
    public List<CartLine> Lines { get; set; } = new();

    [JsonIgnore]
    public int ItemCount => Lines.Sum(l => l.Quantity);
}

public static class CartStore
{
    private const string Key = "cart";

    /// <summary>Session chỉ có khi Program.cs gọi AddSession/UseSession (A bật). Thiếu thì giỏ hàng tắt, không làm hỏng cả trang.</summary>
    public static bool IsAvailable(HttpContext context) => context.Features.Get<ISessionFeature>() is not null;

    public static Cart Load(ISession session)
    {
        var json = session.GetString(Key);
        if (string.IsNullOrEmpty(json)) return new Cart();
        try { return JsonSerializer.Deserialize<Cart>(json) ?? new Cart(); }
        catch (JsonException) { return new Cart(); }
    }

    public static void Save(ISession session, Cart cart) => session.SetString(Key, JsonSerializer.Serialize(cart));

    public static int ItemCount(HttpContext context) => IsAvailable(context) ? Load(context.Session).ItemCount : 0;
}

/// <summary>Luật kiểm tra đầu vào phía máy chủ cho giỏ hàng (trình duyệt có thể bị sửa).</summary>
public static class CartRules
{
    /// <summary>Giới hạn số ly mỗi dòng (do nhóm chọn để chặn nhập sai; đổi được tại đây).</summary>
    public const int MaxQuantityPerLine = 50;

    public static string? ValidateQuantity(string? text, out int quantity)
    {
        quantity = 0;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out quantity) || quantity < 1)
            return "Số lượng phải là số nguyên dương.";
        if (quantity > MaxQuantityPerLine)
            return $"Số lượng tối đa {MaxQuantityPerLine} mỗi dòng.";
        return null;
    }

    public static (CartLine? Line, string? Error) TryBuildLine(
        IReadOnlyList<MenuProductDto> menu, long variantId, string? quantityText, IReadOnlyDictionary<long, string?> addonTexts)
    {
        var product = menu.FirstOrDefault(p => p.Variants.Any(v => v.VariantId == variantId));
        var variant = product?.Variants.FirstOrDefault(v => v.VariantId == variantId);
        if (product is null || variant is null)
            return (null, "Món này không còn bán. Vui lòng chọn lại món khác.");

        var quantityError = ValidateQuantity(quantityText, out var quantity);
        if (quantityError is not null) return (null, quantityError);

        var addons = new List<CartAddon>();
        foreach (var (addonId, text) in addonTexts)
        {
            var option = variant.Addons.FirstOrDefault(a => a.AddonVariantId == addonId);
            if (option is null) return (null, "Tùy chọn thêm không hợp lệ cho món này.");
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var perUnit))
                return (null, $"Số lượng «{option.Name}» phải là số nguyên không âm.");
            if (perUnit > option.MaxPerUnit)
                return (null, $"«{option.Name}» tối đa {option.MaxPerUnit} mỗi ly.");
            if (perUnit > 0) addons.Add(new CartAddon { AddonVariantId = addonId, QuantityPerUnit = perUnit });
        }

        var line = new CartLine
        {
            VariantId = variantId,
            ProductName = product.ProductName,
            VariantName = variant.VariantName,
            Quantity = quantity,
            Addons = addons.OrderBy(a => a.AddonVariantId).ToList(),
        };
        return (line, null);
    }

    private static string Signature(CartLine l) =>
        l.VariantId + ":" + string.Join(",", l.Addons.OrderBy(a => a.AddonVariantId).Select(a => a.AddonVariantId + "x" + a.QuantityPerUnit));

    /// <summary>Thêm vào giỏ; cùng món, cùng tùy chọn thì cộng dồn số lượng. Trả về lỗi hoặc null.</summary>
    public static string? AddToCart(Cart cart, CartLine line)
    {
        var existing = cart.Lines.FirstOrDefault(l => Signature(l) == Signature(line));
        if (existing is null)
        {
            cart.Lines.Add(line);
            return null;
        }
        if (existing.Quantity + line.Quantity > MaxQuantityPerLine)
            return $"Số lượng tối đa {MaxQuantityPerLine} mỗi dòng.";
        existing.Quantity += line.Quantity;
        return null;
    }
}

public sealed record CartAddonView(string Name, int QuantityPerUnit, decimal UnitPrice);

public sealed record CartLineView(string Id, string ProductName, string VariantName, int Quantity, decimal UnitPrice,
    IReadOnlyList<CartAddonView> Addons, bool Available)
{
    public decimal LineTotal => Available ? UnitPrice * Quantity : 0m;
}

public sealed record CartPageModel(IReadOnlyList<CartLineView> Lines)
{
    /// <summary>Tạm tính chỉ để hiển thị; không gồm dòng đã ngừng bán. Máy chủ tính lại khi gửi đơn.</summary>
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
    public bool HasUnavailable => Lines.Any(l => !l.Available);
}

public static class CartResolver
{
    public static CartPageModel Resolve(Cart cart, IReadOnlyList<MenuProductDto> menu)
    {
        var views = new List<CartLineView>();
        foreach (var line in cart.Lines)
        {
            MenuProductDto? product = null;
            MenuVariantDto? variant = null;
            foreach (var p in menu)
            {
                var v = p.Variants.FirstOrDefault(x => x.VariantId == line.VariantId);
                if (v is null) continue;
                product = p;
                variant = v;
                break;
            }

            if (product is null || variant is null)
            {
                views.Add(new CartLineView(line.Id, line.ProductName, line.VariantName, line.Quantity, 0m,
                    Array.Empty<CartAddonView>(), false));
                continue;
            }

            var available = true;
            var unitPrice = variant.UnitPrice;
            var addonViews = new List<CartAddonView>();
            foreach (var a in line.Addons)
            {
                var option = variant.Addons.FirstOrDefault(o => o.AddonVariantId == a.AddonVariantId);
                if (option is null || a.QuantityPerUnit > option.MaxPerUnit)
                {
                    available = false;
                    continue;
                }
                addonViews.Add(new CartAddonView(option.Name, a.QuantityPerUnit, option.UnitPrice));
                unitPrice += option.UnitPrice * a.QuantityPerUnit;
            }
            views.Add(new CartLineView(line.Id, product.ProductName, variant.VariantName, line.Quantity, unitPrice, addonViews, available));
        }
        return new CartPageModel(views);
    }
}
