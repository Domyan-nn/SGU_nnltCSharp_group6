using System.Data;
using CafeManager.Core;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure;

public static class DevelopmentSeeder
{
    // Opt-in only. Inserts missing demo objects; never updates menu, prices, stock or accounts.
    public static async Task SeedAsync(CafeDbContext db, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'CafeManager:DemoSeed', @LockMode='Exclusive', @LockOwner='Transaction';
            IF @r < 0 THROW 51000, 'Seed lock failed', 1;
            """, ct);
        var category = await db.Category.SingleOrDefaultAsync(x => x.CategoryName == "[THỬ] Cà phê", ct);
        if (category == null)
        {
            category = new Category { CategoryName = "[THỬ] Cà phê", IsActive = true };
            db.Add(category); await db.SaveChangesAsync(ct);
        }
        var coffee = await IngredientAsync(db, "[THỬ] Cà phê rang", "g", ct);
        var milk = await IngredientAsync(db, "[THỬ] Sữa đặc", "ml", ct);
        await ProductAsync(db, category.CategoryId, "[THỬ] Cà phê đen", "Nhỏ", "Drink", 25000, [(coffee, 20m)], ct);
        await ProductAsync(db, category.CategoryId, "[THỬ] Cà phê sữa", "Vừa", "Drink", 30000, [(coffee, 20m), (milk, 25m)], ct);
        await ProductAsync(db, category.CategoryId, "[THỬ] Thêm sữa", "Một phần", "Addon", 5000, [(milk, 10m)], ct);
        foreach (var code in new[] { "DEMO-T01", "DEMO-T02", "DEMO-T03" })
            if (!await db.CafeTable.AnyAsync(x => x.TableCode == code, ct))
                db.Add(new CafeTable { TableCode = code, Status = "Ready" });
        if (!await db.Counter.AnyAsync(x => x.CounterCode == "DEMO-C01", ct))
            db.Add(new Counter { CounterCode = "DEMO-C01", IsActive = true });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static async Task<long> IngredientAsync(CafeDbContext db, string name, string unit, CancellationToken ct)
    {
        var existing = await db.Ingredient.SingleOrDefaultAsync(x => x.IngredientName == name, ct);
        if (existing != null) return existing.IngredientId;
        var ingredient = new Ingredient { IngredientName = name, UnitName = unit, IsActive = true };
        db.Add(ingredient); await db.SaveChangesAsync(ct);
        db.Add(new StockBalance { IngredientId = ingredient.IngredientId, QuantityOnHand = 10000, QuantityReserved = 0 });
        db.Add(new StockMovement { MovementCode = $"DEMO-INIT-{ingredient.IngredientId}", IngredientId = ingredient.IngredientId,
            MovementType = "Receipt", Quantity = 10000, SourceType = "DemoSeed", SourceId = ingredient.IngredientId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return ingredient.IngredientId;
    }

    private static async Task ProductAsync(CafeDbContext db, long categoryId, string name, string variantName, string type,
        decimal price, (long Ingredient, decimal Qty)[] ingredients, CancellationToken ct)
    {
        // Existing demo product is deliberately left untouched, even if edited/incomplete.
        if (await db.Product.AnyAsync(x => x.ProductName == name && x.CategoryId == categoryId, ct)) return;
        var product = new Product { CategoryId = categoryId, ProductName = name, IsActive = true };
        db.Add(product); await db.SaveChangesAsync(ct);
        var variant = new ProductVariant { ProductId = product.ProductId, VariantName = variantName, VariantType = type, IsActive = true };
        db.Add(variant); await db.SaveChangesAsync(ct);
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Add(new PriceHistory { VariantId = variant.VariantId, UnitPrice = price, EffectiveFrom = from });
        var recipe = new RecipeVersion { VariantId = variant.VariantId, EffectiveFrom = from };
        db.Add(recipe); await db.SaveChangesAsync(ct);
        foreach (var item in ingredients)
            db.Add(new RecipeDetail { RecipeVersionId = recipe.RecipeVersionId, IngredientId = item.Ingredient, QuantityRequired = item.Qty });
        await db.SaveChangesAsync(ct);
    }
}
