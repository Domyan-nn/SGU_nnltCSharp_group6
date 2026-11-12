using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Một biến thể đang bán được: món, danh mục và biến thể đều còn bán, có giá hiệu lực và công thức hiệu lực có định lượng.
public sealed class SellableVariant
{
    public long VariantId { get; init; }
    public string VariantName { get; init; } = "";
    public string VariantType { get; init; } = "";
    public long ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public decimal UnitPrice { get; init; }
}

// Thực đơn công khai (UC01) và nguồn giá duy nhất cho đặt đơn (UC02): giá luôn lấy tại máy chủ.
public sealed class MenuQueryService(CafeDbContext db)
{
    public static IQueryable<SellableVariant> Sellable(CafeDbContext db, DateTime now) =>
        from v in db.ProductVariant
        join p in db.Product on v.ProductId equals p.ProductId
        join c in db.Category on p.CategoryId equals c.CategoryId
        where v.IsActive && p.IsActive && c.IsActive
        where db.PriceHistory.Any(h => h.VariantId == v.VariantId && h.EffectiveFrom <= now && (h.EffectiveTo == null || now < h.EffectiveTo))
        where db.RecipeVersion.Any(r => r.VariantId == v.VariantId && r.EffectiveFrom <= now && (r.EffectiveTo == null || now < r.EffectiveTo)
            && db.RecipeDetail.Any(d => d.RecipeVersionId == r.RecipeVersionId))
        select new SellableVariant
        {
            VariantId = v.VariantId, VariantName = v.VariantName, VariantType = v.VariantType,
            ProductId = p.ProductId, ProductName = p.ProductName, CategoryId = c.CategoryId, CategoryName = c.CategoryName,
            UnitPrice = db.PriceHistory.Where(h => h.VariantId == v.VariantId && h.EffectiveFrom <= now && (h.EffectiveTo == null || now < h.EffectiveTo))
                .OrderByDescending(h => h.EffectiveFrom).Select(h => h.UnitPrice).First(),
        };

    public async Task<IReadOnlyList<MenuProductDto>> GetAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var sellable = await Sellable(db, utcNow).AsNoTracking().ToListAsync(ct);
        var addonsByProduct = (await (from a in db.AllowedAddon select new { a.ProductId, a.AddonVariantId, a.MaxPerUnit }).ToListAsync(ct))
            .GroupBy(a => a.ProductId).ToDictionary(g => g.Key, g => g.ToList());
        var byId = sellable.ToDictionary(x => x.VariantId);
        return sellable.Where(x => x.VariantType == "Drink")
            .GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryId, x.CategoryName })
            .OrderBy(g => g.Key.CategoryName).ThenBy(g => g.Key.ProductName)
            .Select(g =>
            {
                var addons = (addonsByProduct.GetValueOrDefault(g.Key.ProductId) ?? [])
                    .Where(a => byId.TryGetValue(a.AddonVariantId, out var av) && av.VariantType == "Addon")
                    .Select(a => new AddonOptionDto(a.AddonVariantId, byId[a.AddonVariantId].ProductName, byId[a.AddonVariantId].UnitPrice, a.MaxPerUnit))
                    .OrderBy(a => a.Name).ToList();
                var variants = g.OrderBy(v => v.UnitPrice).ThenBy(v => v.VariantName)
                    .Select(v => new MenuVariantDto(v.VariantId, v.VariantName, v.UnitPrice, addons)).ToList();
                return new MenuProductDto(g.Key.ProductId, g.Key.ProductName, g.Key.CategoryId, g.Key.CategoryName, variants);
            }).ToList();
    }
}
