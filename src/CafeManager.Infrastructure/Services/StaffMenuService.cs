using System.Data;
using CafeManager.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Quản lý thực đơn, giá và công thức (UC17). Quyền MENU_MANAGE được kiểm tra ở lớp Web.
// Quy tắc: món/biến thể chỉ bán (IsActive = true) khi có giá và công thức hiệu lực; giá và công thức đổi bằng phiên bản mới
// theo khoảng hiệu lực, không sửa bản cũ; không xóa món đã có chứng từ, chỉ ngừng bán.
public sealed class StaffMenuService(CafeDbContext db)
{
    private static readonly string[] VariantTypes = ["Drink", "Addon"];
    private static readonly TimeSpan PastTolerance = TimeSpan.FromMinutes(5);

    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    public async Task<StaffMenuResponse> GetAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var categories = await db.Category.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(ct);
        var products = await (from p in db.Product join c in db.Category on p.CategoryId equals c.CategoryId
                              orderby c.CategoryName, p.ProductName select new { p, c.CategoryName }).AsNoTracking().ToListAsync(ct);
        var variants = await db.ProductVariant.AsNoTracking().OrderBy(v => v.VariantId).ToListAsync(ct);
        var prices = await db.PriceHistory.AsNoTracking().Where(h => h.EffectiveFrom <= utcNow && (h.EffectiveTo == null || utcNow < h.EffectiveTo)).ToListAsync(ct);
        var recipes = await (from r in db.RecipeVersion join d in db.RecipeDetail on r.RecipeVersionId equals d.RecipeVersionId
                             join i in db.Ingredient on d.IngredientId equals i.IngredientId
                             where r.EffectiveFrom <= utcNow && (r.EffectiveTo == null || utcNow < r.EffectiveTo)
                             select new { r.VariantId, Line = new StaffRecipeLineDto(i.IngredientId, i.IngredientName, i.UnitName, d.QuantityRequired) })
                            .AsNoTracking().ToListAsync(ct);
        var ingredients = await db.Ingredient.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.IngredientName).ToListAsync(ct);
        return new StaffMenuResponse(
            products.Select(x => new StaffProductDto(x.p.ProductId, x.p.ProductName, x.p.CategoryId, x.CategoryName, x.p.IsActive,
                variants.Where(v => v.ProductId == x.p.ProductId).Select(v =>
                {
                    var price = prices.FirstOrDefault(h => h.VariantId == v.VariantId);
                    return new StaffVariantDto(v.VariantId, v.VariantName, v.VariantType, v.IsActive, price?.UnitPrice, price?.EffectiveFrom,
                        recipes.Where(r => r.VariantId == v.VariantId).Select(r => r.Line).OrderBy(l => l.IngredientName).ToList());
                }).ToList())).ToList(),
            categories.Select(c => new CategoryLookupDto(c.CategoryId, c.CategoryName)).ToList(),
            ingredients.Select(i => new IngredientLookupDto(i.IngredientId, i.IngredientName, i.UnitName)).ToList());
    }

    private static string CheckName(string? value, string label)
    {
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name)) throw new AppException(ApiErrorCodes.Validation, $"Nhập {label}.");
        if (name.Length > 200) throw new AppException(ApiErrorCodes.Validation, $"{char.ToUpper(label[0])}{label[1..]} tối đa 200 ký tự.");
        return name;
    }

    public async Task<long> UpsertProductAsync(long? productId, ProductUpsertRequest request, CancellationToken ct = default)
    {
        var name = CheckName(request?.ProductName, "tên món");
        if (!await db.Category.AnyAsync(c => c.CategoryId == request!.CategoryId && c.IsActive, ct))
            throw new AppException(ApiErrorCodes.Validation, "Danh mục không tồn tại hoặc đã ngừng dùng.");
        Product product;
        if (productId == null) { product = new Product(); db.Add(product); }
        else product = await db.Product.SingleOrDefaultAsync(p => p.ProductId == productId, ct) ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy món.");
        product.ProductName = name; product.CategoryId = request!.CategoryId; product.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return product.ProductId;
    }

    public async Task<long> UpsertVariantAsync(long? variantId, VariantUpsertRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        var name = CheckName(request?.VariantName, "tên biến thể");
        if (!VariantTypes.Contains(request!.VariantType)) throw new AppException(ApiErrorCodes.Validation, "Loại biến thể phải là Drink hoặc Addon.");
        if (!await db.Product.AnyAsync(p => p.ProductId == request.ProductId, ct)) throw new AppException(ApiErrorCodes.Validation, "Món không tồn tại.");
        ProductVariant variant;
        if (variantId == null)
        {
            variant = new ProductVariant { ProductId = request.ProductId, VariantType = request.VariantType, IsActive = false }; // chưa có giá, công thức: chưa bán
            db.Add(variant);
        }
        else
        {
            variant = await db.ProductVariant.SingleOrDefaultAsync(v => v.VariantId == variantId, ct) ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy biến thể.");
            if (variant.VariantType != request.VariantType || variant.ProductId != request.ProductId)
                throw new AppException(ApiErrorCodes.Validation, "Không đổi được món hoặc loại của biến thể đã tạo.");
        }
        variant.VariantName = name;
        if (variantId != null)
        {
            if (request.IsActive) await RequireSellableAsync(variant.VariantId, utcNow, ct);
            variant.IsActive = request.IsActive;
        }
        else if (request.IsActive) throw new AppException(ApiErrorCodes.Validation, "Biến thể mới chưa có giá và công thức nên chưa bán được; hãy đặt giá và công thức rồi bật bán.");
        await db.SaveChangesAsync(ct);
        return variant.VariantId;
    }

    private async Task RequireSellableAsync(long variantId, DateTime now, CancellationToken ct)
    {
        var hasPrice = await db.PriceHistory.AnyAsync(h => h.VariantId == variantId && h.EffectiveFrom <= now && (h.EffectiveTo == null || now < h.EffectiveTo), ct);
        var hasRecipe = await db.RecipeVersion.AnyAsync(r => r.VariantId == variantId && r.EffectiveFrom <= now && (r.EffectiveTo == null || now < r.EffectiveTo)
            && db.RecipeDetail.Any(d => d.RecipeVersionId == r.RecipeVersionId), ct);
        if (!hasPrice || !hasRecipe) throw new AppException(ApiErrorCodes.Validation, "Cần có giá và công thức đang hiệu lực trước khi bật bán.");
    }

    public async Task DeactivateVariantAsync(long variantId, CancellationToken ct = default)
    {
        var variant = await db.ProductVariant.SingleOrDefaultAsync(v => v.VariantId == variantId, ct) ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy biến thể.");
        variant.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static DateTime CheckEffective(DateTime value, DateTime utcNow)
    {
        var utc = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        if (utc < utcNow - PastTolerance) throw new AppException(ApiErrorCodes.Validation, "Ngày hiệu lực không được ở quá khứ.");
        return utc;
    }

    private static bool IsOverlap(DbUpdateException ex) => ex.InnerException is SqlException { Number: 51001 };

    public async Task SetPriceAsync(long variantId, SetPriceRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        if (request == null || request.UnitPrice < 0 || decimal.Round(request.UnitPrice, 2) != request.UnitPrice || request.UnitPrice > 100_000_000)
            throw new AppException(ApiErrorCodes.Validation, "Giá phải từ 0, tối đa 2 chữ số thập phân.");
        var from = CheckEffective(request.EffectiveFrom, utcNow);
        if (!await db.ProductVariant.AnyAsync(v => v.VariantId == variantId, ct)) throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy biến thể.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync($"price:{variantId}", ct);
        try
        {
            var last = await db.PriceHistory.Where(h => h.VariantId == variantId).OrderByDescending(h => h.EffectiveFrom).FirstOrDefaultAsync(ct);
            if (last != null)
            {
                if (last.EffectiveFrom >= from) throw new AppException(ApiErrorCodes.OverlappingInterval, "Đã có giá hiệu lực từ cùng hoặc sau ngày này.");
                if (last.EffectiveTo == null || last.EffectiveTo > from) { last.EffectiveTo = from; await db.SaveChangesAsync(ct); }
            }
            db.Add(new PriceHistory { VariantId = variantId, UnitPrice = request.UnitPrice, EffectiveFrom = from });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsOverlap(ex)) { throw new AppException(ApiErrorCodes.OverlappingInterval, "Khoảng hiệu lực giá bị chồng lấn."); }
    }

    public async Task SetRecipeAsync(long variantId, SetRecipeRequest request, DateTime utcNow, CancellationToken ct = default)
    {
        if (request?.Lines == null || request.Lines.Count == 0) throw new AppException(ApiErrorCodes.Validation, "Công thức cần ít nhất một nguyên liệu.");
        if (request.Lines.Select(l => l.IngredientId).Distinct().Count() != request.Lines.Count) throw new AppException(ApiErrorCodes.Validation, "Mỗi nguyên liệu chỉ xuất hiện một lần.");
        if (request.Lines.Any(l => l.Quantity <= 0 || decimal.Round(l.Quantity, 3) != l.Quantity || l.Quantity > 1_000_000))
            throw new AppException(ApiErrorCodes.Validation, "Định lượng phải lớn hơn 0, tối đa 3 chữ số thập phân.");
        var from = CheckEffective(request.EffectiveFrom, utcNow);
        if (!await db.ProductVariant.AnyAsync(v => v.VariantId == variantId, ct)) throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy biến thể.");
        var ids = request.Lines.Select(l => l.IngredientId).ToList();
        if (await db.Ingredient.CountAsync(i => ids.Contains(i.IngredientId) && i.IsActive, ct) != ids.Count)
            throw new AppException(ApiErrorCodes.Validation, "Có nguyên liệu không tồn tại hoặc đã ngừng dùng.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync($"recipe:{variantId}", ct);
        try
        {
            var last = await db.RecipeVersion.Where(r => r.VariantId == variantId).OrderByDescending(r => r.EffectiveFrom).FirstOrDefaultAsync(ct);
            if (last != null)
            {
                if (last.EffectiveFrom >= from) throw new AppException(ApiErrorCodes.OverlappingInterval, "Đã có công thức hiệu lực từ cùng hoặc sau ngày này.");
                if (last.EffectiveTo == null || last.EffectiveTo > from) { last.EffectiveTo = from; await db.SaveChangesAsync(ct); }
            }
            var version = new RecipeVersion { VariantId = variantId, EffectiveFrom = from };
            db.Add(version); await db.SaveChangesAsync(ct);
            foreach (var line in request.Lines)
                db.Add(new RecipeDetail { RecipeVersionId = version.RecipeVersionId, IngredientId = line.IngredientId, QuantityRequired = line.Quantity });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsOverlap(ex)) { throw new AppException(ApiErrorCodes.OverlappingInterval, "Khoảng hiệu lực công thức bị chồng lấn."); }
    }
}
