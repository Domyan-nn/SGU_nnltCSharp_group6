using CafeManager.Core;
using CafeManager.Infrastructure;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CafeManager.Tests;

// Dữ liệu thử dùng chung cho kiểm thử dịch vụ và kiểm thử API: một món có tùy chọn, hai khách, một thu ngân.
public static class TestData
{
    public sealed record Data(string Suffix, long DrinkProduct, long Drink, long Addon, long Ingredient, string Customer, string Customer2, string Cashier);

    public static DateTime Pickup(int hourLocal = 10) =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7).Date.AddDays(1).AddHours(hourLocal).AddHours(-7), DateTimeKind.Utc);

    public static CreateOrderRequest Order(Data d, int quantity = 2, decimal expected = 30000, int addon = 1, Guid? id = null, DateTime? pickup = null) =>
        new(id ?? Guid.NewGuid(), pickup ?? Pickup(), "Khách nhận", "0901234567",
            [new OrderLineRequest(d.Drink, quantity, expected, [new OrderAddonRequest(d.Addon, addon)])]);

    public static async Task<Data> SeedAsync(IServiceProvider provider, decimal stock = 1000)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
        var category = new Category { CategoryName = "DM " + s, IsActive = true };
        var ingredient = new Ingredient { IngredientName = "NL " + s, UnitName = "g", IsActive = true };
        db.AddRange(category, ingredient); await db.SaveChangesAsync();
        db.Add(new StockBalance { IngredientId = ingredient.IngredientId, QuantityOnHand = stock });
        var drinkProduct = new Product { CategoryId = category.CategoryId, ProductName = "Cà phê " + s, IsActive = true };
        var addonProduct = new Product { CategoryId = category.CategoryId, ProductName = "Thêm sữa " + s, IsActive = true };
        db.AddRange(drinkProduct, addonProduct); await db.SaveChangesAsync();
        var drink = new ProductVariant { ProductId = drinkProduct.ProductId, VariantName = "Vừa", VariantType = "Drink", IsActive = true };
        var addon = new ProductVariant { ProductId = addonProduct.ProductId, VariantName = "Một phần", VariantType = "Addon", IsActive = true };
        db.AddRange(drink, addon); await db.SaveChangesAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        foreach (var (variant, price, qty) in new[] { (drink, 30000m, 20m), (addon, 5000m, 10m) })
        {
            db.Add(new PriceHistory { VariantId = variant.VariantId, UnitPrice = price, EffectiveFrom = from });
            var recipe = new RecipeVersion { VariantId = variant.VariantId, EffectiveFrom = from };
            db.Add(recipe); await db.SaveChangesAsync();
            db.Add(new RecipeDetail { RecipeVersionId = recipe.RecipeVersionId, IngredientId = ingredient.IngredientId, QuantityRequired = qty });
        }
        db.Add(new AllowedAddon { ProductId = drinkProduct.ProductId, AddonVariantId = addon.VariantId, MaxPerUnit = 2 });
        await db.SaveChangesAsync();

        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        await accounts.RegisterCustomerAsync(new("cust." + s, "secret1", "Khách " + s, "0901234567"));
        await accounts.RegisterCustomerAsync(new("other." + s, "secret1", "Khách khác " + s, "0907654321"));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var cashier = new IdentityUser { UserName = "cashier." + s };
        Assert.True((await users.CreateAsync(cashier, "secret1")).Succeeded);
        var staff = new StaffAccount { IdentityUserId = cashier.Id, FullName = "Thu ngân " + s, IsActive = true };
        var role = new Role { RoleName = "Cashier " + s };
        var permission = await db.Permission.SingleOrDefaultAsync(p => p.PermissionCode == Permissions.OrderReceive);
        if (permission == null) { permission = new Permission { PermissionCode = Permissions.OrderReceive }; db.Add(permission); }
        db.AddRange(staff, role); await db.SaveChangesAsync();
        db.AddRange(new StaffRole { StaffId = staff.StaffId, RoleId = role.RoleId }, new RolePermission { RoleId = role.RoleId, PermissionId = permission.PermissionId });
        await db.SaveChangesAsync();
        return new(s, drinkProduct.ProductId, drink.VariantId, addon.VariantId, ingredient.IngredientId, "cust." + s, "other." + s, "cashier." + s);
    }

    // Thêm một quản lý có quyền MENU_MANAGE để thử API quản lý thực đơn.
    public static async Task<string> SeedManagerAsync(IServiceProvider provider, string suffix)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = "manager." + suffix };
        Assert.True((await users.CreateAsync(user, "secret1")).Succeeded);
        var staff = new StaffAccount { IdentityUserId = user.Id, FullName = "Quản lý " + suffix, IsActive = true };
        var role = new Role { RoleName = "Manager " + suffix };
        var permission = await db.Permission.SingleOrDefaultAsync(p => p.PermissionCode == Permissions.MenuManage);
        if (permission == null) { permission = new Permission { PermissionCode = Permissions.MenuManage }; db.Add(permission); }
        db.AddRange(staff, role); await db.SaveChangesAsync();
        db.AddRange(new StaffRole { StaffId = staff.StaffId, RoleId = role.RoleId }, new RolePermission { RoleId = role.RoleId, PermissionId = permission.PermissionId });
        await db.SaveChangesAsync();
        return user.UserName;
    }
}
