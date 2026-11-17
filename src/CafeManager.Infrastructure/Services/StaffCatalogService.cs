using System.Data;
using CafeManager.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure.Services;

// Quản lý danh mục và nguyên liệu (A1, Desktop C1/C2). Quyền MENU_MANAGE được kiểm tra ở lớp Web.
// Quy tắc: tên không trùng (không phân biệt hoa thường); chỉ xóa khi chưa có tham chiếu, ngược lại trả 409 CONFLICT
// (muốn ẩn thì đặt IsActive = false). Nguyên liệu mới có sẵn một dòng tồn bằng 0 để tiếp nhận đơn dùng được ngay khi nhập kho sau.
public sealed class StaffCatalogService(CafeDbContext db)
{
    private const int MaxName = 200, MaxUnit = 30, MaxList = 500;

    private Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
IF @r < 0 THROW 51000, 'Cannot acquire operation lock', 1;", ct);

    private static string Check(string? value, string label, int max)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) throw new AppException(ApiErrorCodes.Validation, $"Nhập {label}.");
        if (text.Length > max) throw new AppException(ApiErrorCodes.Validation, $"{char.ToUpper(label[0])}{label[1..]} tối đa {max} ký tự.");
        return text;
    }

    private static bool IsReference(DbUpdateException ex) => ex.InnerException is SqlException { Number: 547 };
    private static bool IsDuplicate(DbUpdateException ex) => ex.InnerException is SqlException { Number: 2601 or 2627 };

    // ---------------------------------------------------------------- Danh mục

    public async Task<IReadOnlyList<CategoryDto>> CategoriesAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Category.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var q = search.Trim(); query = query.Where(c => c.CategoryName.Contains(q)); }
        var rows = await query.OrderBy(c => c.CategoryName).Take(MaxList)
            .Select(c => new CategoryDto(c.CategoryId, c.CategoryName, c.IsActive, db.Product.Count(p => p.CategoryId == c.CategoryId)))
            .ToListAsync(ct);
        return rows;
    }

    public async Task<long> SaveCategoryAsync(long? categoryId, CategoryUpsertRequest request, CancellationToken ct = default)
    {
        var name = Check(request?.CategoryName, "tên danh mục", MaxName);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync("catalog:category-name", ct);
        var category = categoryId == null ? new Category()
            : await db.Category.SingleOrDefaultAsync(c => c.CategoryId == categoryId, ct)
              ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy danh mục.");
        if (await db.Category.AnyAsync(c => c.CategoryName == name && c.CategoryId != category.CategoryId, ct))
            throw new AppException(ApiErrorCodes.Conflict, "Đã có danh mục trùng tên.");
        category.CategoryName = name; category.IsActive = request!.IsActive;
        if (categoryId == null) db.Add(category);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { throw new AppException(ApiErrorCodes.Conflict, "Đã có danh mục trùng tên."); }
        await tx.CommitAsync(ct);
        return category.CategoryId;
    }

    public async Task DeleteCategoryAsync(long categoryId, CancellationToken ct = default)
    {
        var category = await db.Category.SingleOrDefaultAsync(c => c.CategoryId == categoryId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy danh mục.");
        if (await db.Product.AnyAsync(p => p.CategoryId == categoryId, ct))
            throw new AppException(ApiErrorCodes.Conflict, "Danh mục còn món nên không xóa được; hãy chuyển món sang danh mục khác hoặc ngừng dùng danh mục.");
        db.Remove(category);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsReference(ex)) { throw new AppException(ApiErrorCodes.Conflict, "Danh mục vừa được dùng ở nơi khác nên không xóa được."); }
    }

    // ---------------------------------------------------------------- Nguyên liệu

    public async Task<IReadOnlyList<IngredientDto>> IngredientsAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Ingredient.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var q = search.Trim(); query = query.Where(i => i.IngredientName.Contains(q)); }
        var items = await query.OrderBy(i => i.IngredientName).Take(MaxList).ToListAsync(ct);
        var ids = items.Select(i => i.IngredientId).ToList();
        var stock = await db.StockBalance.AsNoTracking().Where(s => ids.Contains(s.IngredientId)).ToDictionaryAsync(s => s.IngredientId, ct);
        var recipes = await db.RecipeDetail.AsNoTracking().Where(r => ids.Contains(r.IngredientId))
            .GroupBy(r => r.IngredientId).Select(g => new { Id = g.Key, Count = g.Select(r => r.RecipeVersionId).Distinct().Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return items.Select(i => new IngredientDto(i.IngredientId, i.IngredientName, i.UnitName, i.IsActive,
            stock.TryGetValue(i.IngredientId, out var s) ? s.QuantityOnHand : 0, s?.QuantityReserved ?? 0, recipes.GetValueOrDefault(i.IngredientId))).ToList();
    }

    public async Task<long> SaveIngredientAsync(long? ingredientId, IngredientUpsertRequest request, CancellationToken ct = default)
    {
        var name = Check(request?.IngredientName, "tên nguyên liệu", MaxName);
        var unit = Check(request!.UnitName, "đơn vị tính", MaxUnit);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockAsync("catalog:ingredient-name", ct);
        var ingredient = ingredientId == null ? new Ingredient()
            : await db.Ingredient.SingleOrDefaultAsync(i => i.IngredientId == ingredientId, ct)
              ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy nguyên liệu.");
        if (await db.Ingredient.AnyAsync(i => i.IngredientName == name && i.IngredientId != ingredient.IngredientId, ct))
            throw new AppException(ApiErrorCodes.Conflict, "Đã có nguyên liệu trùng tên.");
        ingredient.IngredientName = name; ingredient.UnitName = unit; ingredient.IsActive = request.IsActive;
        if (ingredientId == null) db.Add(ingredient);
        await db.SaveChangesAsync(ct);
        if (ingredientId == null)
        {
            db.Add(new StockBalance { IngredientId = ingredient.IngredientId, QuantityOnHand = 0, QuantityReserved = 0 });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return ingredient.IngredientId;
    }

    public async Task DeleteIngredientAsync(long ingredientId, CancellationToken ct = default)
    {
        var ingredient = await db.Ingredient.SingleOrDefaultAsync(i => i.IngredientId == ingredientId, ct)
            ?? throw new AppException(ApiErrorCodes.NotFound, "Không tìm thấy nguyên liệu.");
        if (await db.RecipeDetail.AnyAsync(r => r.IngredientId == ingredientId, ct))
            throw new AppException(ApiErrorCodes.Conflict, "Nguyên liệu đang nằm trong công thức nên không xóa được; hãy gỡ khỏi công thức hoặc ngừng dùng.");
        var balance = await db.StockBalance.SingleOrDefaultAsync(s => s.IngredientId == ingredientId, ct);
        if ((balance != null && (balance.QuantityOnHand != 0 || balance.QuantityReserved != 0))
            || await db.StockMovement.AnyAsync(m => m.IngredientId == ingredientId, ct)
            || await db.IngredientBatch.AnyAsync(b => b.IngredientId == ingredientId, ct)
            || await db.StockCount.AnyAsync(c => c.IngredientId == ingredientId, ct)
            || await db.PreparationIngredient.AnyAsync(p => p.IngredientId == ingredientId, ct))
            throw new AppException(ApiErrorCodes.Conflict, "Nguyên liệu đã có phát sinh kho nên không xóa được; hãy ngừng dùng.");
        if (balance != null) db.Remove(balance);
        db.Remove(ingredient);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsReference(ex)) { throw new AppException(ApiErrorCodes.Conflict, "Nguyên liệu vừa được dùng ở nơi khác nên không xóa được."); }
        catch (DbUpdateConcurrencyException) { throw new AppException(ApiErrorCodes.Conflict, "Nguyên liệu vừa thay đổi ở nơi khác, vui lòng tải lại."); }
    }
}
