using CafeManager.Core;
using CafeManager.Infrastructure;
using CafeManager.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Kiểm thử dịch vụ ứng dụng (đăng nhập, thực đơn, đặt/hủy đơn, tiếp nhận/từ chối) trên SQL Server thật.
public sealed class ServiceTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private ServiceProvider Services()
    {
        var s = new ServiceCollection();
        s.AddLogging();
        s.AddScoped(_ => fixture.Open());
        s.AddIdentityCore<IdentityUser>(IdentitySetup.Configure).AddEntityFrameworkStores<CafeDbContext>();
        s.AddScoped<StaffPermissionReader>();
        s.AddScoped<AccountService>();
        s.AddScoped<MenuQueryService>();
        s.AddScoped<CustomerOrderService>();
        s.AddScoped<ICafeDataCommands, CafeDataCommands>();
        s.AddScoped<StaffOrderService>();
        return s.BuildServiceProvider();
    }

    private static async Task<string> UserIdAsync(IServiceScope scope, string userName) =>
        (await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByNameAsync(userName))!.Id;

    private static async Task<AppException> Fails(Func<Task> action)
    {
        var ex = await Record.ExceptionAsync(action);
        return Assert.IsType<AppException>(ex);
    }

    [SqlServerFact]
    public async Task Register_creates_account_with_profile_and_rejects_duplicates_and_bad_input()
    {
        await using var provider = Services();
        var s = Guid.NewGuid().ToString("N")[..8];
        using var scope = provider.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        await accounts.RegisterCustomerAsync(new("reg." + s, "secret1", "Người đăng ký", "0901234567"));
        var session = await accounts.LoginAsync(new("reg." + s, "secret1"), UserKinds.Customer);
        Assert.Equal(UserKinds.Customer, session.Kind); Assert.Equal("Người đăng ký", session.DisplayName); Assert.Empty(session.Permissions);
        Assert.Equal(ApiErrorCodes.Conflict, (await Fails(() => accounts.RegisterCustomerAsync(new("REG." + s, "secret1", "Trùng", "0901234567")))).Code);
        foreach (var bad in new RegisterCustomerRequest[] { new("a", "secret1", "X", "0901234567"), new("ok." + s, "123", "X", "0901234567"),
                     new("ok." + s, "secret1", " ", "0901234567"), new("ok." + s, "secret1", "X", "123"), new("sai tên", "secret1", "X", "0901234567") })
            Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => accounts.RegisterCustomerAsync(bad))).Code);
        var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(u => u.NormalizedUserName == ("REG." + s)));
        Assert.Equal(0, await db.Users.CountAsync(u => u.NormalizedUserName == ("OK." + s)));
    }

    [SqlServerFact]
    public async Task Login_distinguishes_wrong_password_lockout_and_account_kind()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider);
        using var scope = provider.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        var staff = await accounts.LoginAsync(new(d.Cashier, "secret1"));
        Assert.Equal(UserKinds.Staff, staff.Kind); Assert.Contains(Permissions.OrderReceive, staff.Permissions);
        Assert.Equal(ApiErrorCodes.Forbidden, (await Fails(() => accounts.LoginAsync(new(d.Customer, "secret1"), UserKinds.Staff))).Code);
        Assert.Equal(ApiErrorCodes.Forbidden, (await Fails(() => accounts.LoginAsync(new(d.Cashier, "secret1"), UserKinds.Customer))).Code);
        Assert.Equal(ApiErrorCodes.Unauthorized, (await Fails(() => accounts.LoginAsync(new("khong.co." + d.Suffix, "x")))).Code);
        for (var i = 0; i < 4; i++)
            Assert.Equal(ApiErrorCodes.Unauthorized, (await Fails(() => accounts.LoginAsync(new(d.Customer, "sai")))).Code);
        Assert.Equal(ApiErrorCodes.AccountLocked, (await Fails(() => accounts.LoginAsync(new(d.Customer, "sai")))).Code);
        Assert.Equal(ApiErrorCodes.AccountLocked, (await Fails(() => accounts.LoginAsync(new(d.Customer, "secret1")))).Code); // đúng mật khẩu vẫn bị khóa
    }

    [SqlServerFact]
    public async Task Revalidation_fails_after_staff_is_deactivated_or_user_is_locked()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider);
        string id;
        using (var scope = provider.CreateScope())
        {
            id = await UserIdAsync(scope, d.Cashier);
            Assert.NotNull(await scope.ServiceProvider.GetRequiredService<AccountService>().RevalidateAsync(id, UserKinds.Staff));
        }
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            (await db.StaffAccount.SingleAsync(x => x.IdentityUserId == id)).IsActive = false; await db.SaveChangesAsync();
        }
        using (var scope = provider.CreateScope())
            Assert.Null(await scope.ServiceProvider.GetRequiredService<AccountService>().RevalidateAsync(id, UserKinds.Staff));
        using (var scope = provider.CreateScope())
        {
            var custId = await UserIdAsync(scope, d.Customer);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            await users.SetLockoutEndDateAsync((await users.FindByIdAsync(custId))!, DateTimeOffset.UtcNow.AddHours(1));
            Assert.Null(await scope.ServiceProvider.GetRequiredService<AccountService>().RevalidateAsync(custId, UserKinds.Customer));
        }
    }

    [SqlServerFact]
    public async Task Menu_lists_only_sellable_drinks_with_allowed_addons()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider);
        using (var scope = provider.CreateScope())
        {
            var menu = await scope.ServiceProvider.GetRequiredService<MenuQueryService>().GetAsync(DateTime.UtcNow);
            var product = Assert.Single(menu, p => p.ProductName == "Cà phê " + d.Suffix);
            var variant = Assert.Single(product.Variants);
            Assert.Equal(30000, variant.UnitPrice);
            var addon = Assert.Single(variant.Addons);
            Assert.Equal(5000, addon.UnitPrice); Assert.Equal(2, addon.MaxPerUnit); Assert.Equal("Thêm sữa " + d.Suffix, addon.Name);
            Assert.DoesNotContain(menu, p => p.ProductName == "Thêm sữa " + d.Suffix); // tùy chọn không đứng riêng như một món
        }
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            (await db.ProductVariant.SingleAsync(v => v.VariantId == d.Drink)).IsActive = false; await db.SaveChangesAsync();
        }
        using (var scope = provider.CreateScope())
            Assert.DoesNotContain(await scope.ServiceProvider.GetRequiredService<MenuQueryService>().GetAsync(DateTime.UtcNow), p => p.ProductName == "Cà phê " + d.Suffix);
    }

    [SqlServerFact]
    public async Task Create_order_prices_on_server_snapshots_lines_and_is_idempotent()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider);
        var requestId = Guid.NewGuid();
        CreateOrderResponse first;
        using (var scope = provider.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<CustomerOrderService>();
            first = await orders.CreateAsync(await UserIdAsync(scope, d.Customer), Order(d, id: requestId), DateTime.UtcNow);
            Assert.Equal("Pending", first.Status); Assert.Equal((30000 + 5000) * 2, first.Total); Assert.Matches("^WEB-\\d{6}-[A-Z2-9]{8}$", first.OrderCode); Assert.Equal(4, first.PickupCode.Length);
        }
        using (var scope = provider.CreateScope()) // gửi lại cùng mã, cùng nội dung: trả đơn cũ, không tạo thêm
        {
            var again = await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CreateAsync(await UserIdAsync(scope, d.Customer), Order(d, id: requestId, pickup: Pickup()), DateTime.UtcNow);
            Assert.Equal(first.OrderId, again.OrderId); Assert.Equal(first.OrderCode, again.OrderCode); Assert.Equal(first.Total, again.Total);
        }
        using (var scope = provider.CreateScope()) // cùng mã, khác nội dung: bị từ chối
            Assert.Equal(ApiErrorCodes.Conflict, (await Fails(async () => await scope.ServiceProvider.GetRequiredService<CustomerOrderService>()
                .CreateAsync(await UserIdAsync(scope, d.Customer), Order(d, quantity: 3, id: requestId), DateTime.UtcNow))).Code);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            var order = await db.SalesOrder.AsNoTracking().SingleAsync(o => o.OrderId == first.OrderId);
            Assert.Equal("Web", order.OrderType); Assert.Equal("Khách nhận", order.ContactName); Assert.NotNull(order.ExpectedPickupAt);
            var round = await db.OrderRound.AsNoTracking().SingleAsync(r => r.OrderId == order.OrderId);
            Assert.Equal("Draft", round.Status); Assert.Equal(0, await db.PreparationTicket.CountAsync(t => t.RoundId == round.RoundId)); // chưa có phiếu, chưa giữ tồn
            var line = await db.OrderLine.AsNoTracking().SingleAsync(l => l.RoundId == round.RoundId);
            Assert.Equal(30000, line.UnitPriceSnapshot); Assert.Equal("Cà phê " + d.Suffix, line.ProductNameSnapshot); Assert.Equal(2, line.Quantity);
            var addon = await db.OrderLineAddon.AsNoTracking().SingleAsync(a => a.LineId == line.LineId);
            Assert.Equal(1, addon.Quantity); Assert.Equal(5000, addon.UnitPriceSnapshot);
            Assert.Equal(0, (await db.StockBalance.SingleAsync(b => b.IngredientId == d.Ingredient)).QuantityReserved);
            Assert.Equal(1, await db.SalesOrder.CountAsync(o => o.CustomerId == order.CustomerId));
        }
    }

    [SqlServerFact]
    public async Task Create_order_rejects_price_change_bad_addon_hours_past_time_and_non_customers()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider);
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<CustomerOrderService>();
        var user = await UserIdAsync(scope, d.Customer);
        var changed = await Fails(() => orders.CreateAsync(user, Order(d, expected: 28000), DateTime.UtcNow));
        Assert.Equal(ApiErrorCodes.PriceChanged, changed.Code);
        var change = Assert.Single(changed.PriceChanges!); Assert.Equal(28000, change.OldPrice); Assert.Equal(30000, change.NewPrice);
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d, addon: 3), DateTime.UtcNow))).Code);   // vượt MaxPerUnit
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d, quantity: 0), DateTime.UtcNow))).Code);
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d, quantity: 51), DateTime.UtcNow))).Code);
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d, pickup: DateTime.UtcNow.AddHours(-2)), DateTime.UtcNow))).Code);
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d, pickup: Pickup(23)), DateTime.UtcNow))).Code);  // ngoài giờ phục vụ
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d) with { Lines = [] }, DateTime.UtcNow))).Code);
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, Order(d) with { ContactPhone = "12" }, DateTime.UtcNow))).Code);
        var drinkAsAddon = Order(d) with { Lines = [new(d.Drink, 1, 30000, [new(d.Drink, 1)])] };   // một món uống không được dùng làm tùy chọn
        Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => orders.CreateAsync(user, drinkAsAddon, DateTime.UtcNow))).Code);
        Assert.Equal(ApiErrorCodes.Forbidden, (await Fails(async () => await orders.CreateAsync(await UserIdAsync(scope, d.Cashier), Order(d), DateTime.UtcNow))).Code);
        var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
        Assert.Equal(0, await db.SalesOrder.CountAsync(o => db.CustomerProfile.Any(c => c.CustomerId == o.CustomerId && c.IdentityUserId == user)));
    }

    [SqlServerFact]
    public async Task Customers_see_only_their_orders_and_cancel_only_pending_ones()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider);
        long orderId;
        using (var scope = provider.CreateScope())
            orderId = (await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CreateAsync(await UserIdAsync(scope, d.Customer), Order(d), DateTime.UtcNow)).OrderId;
        using (var scope = provider.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<CustomerOrderService>();
            var mine = await orders.ListMineAsync(await UserIdAsync(scope, d.Customer));
            Assert.Equal(orderId, Assert.Single(mine).OrderId);
            var detail = await orders.GetMineAsync(await UserIdAsync(scope, d.Customer), orderId);
            Assert.NotNull(detail); Assert.Equal(70000, detail.Summary.Total); Assert.Single(detail.Lines); Assert.Single(detail.Lines[0].Addons);
            var other = await UserIdAsync(scope, d.Customer2);
            Assert.Empty(await orders.ListMineAsync(other));
            Assert.Null(await orders.GetMineAsync(other, orderId));                    // đơn người khác: như không tồn tại
            Assert.Equal(ApiErrorCodes.NotFound, (await Fails(() => orders.CancelAsync(other, orderId))).Code);
        }
        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CancelAsync(await UserIdAsync(scope, d.Customer), orderId);
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            Assert.Equal("Cancelled", (await db.SalesOrder.AsNoTracking().SingleAsync(o => o.OrderId == orderId)).Status);
            Assert.Equal(0, await db.OrderRound.CountAsync(r => r.OrderId == orderId && r.Status == "Draft"));
        }
        using (var scope = provider.CreateScope()) // đã hủy rồi thì không hủy lại được
            Assert.Equal(ApiErrorCodes.InvalidState, (await Fails(async () => await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CancelAsync(await UserIdAsync(scope, d.Customer), orderId))).Code);
    }

    [SqlServerFact]
    public async Task Staff_accepts_reserving_stock_rejects_with_reason_and_stock_shortage_leaves_order_pending()
    {
        await using var provider = Services();
        var d = await SeedAsync(provider, stock: 100); // mỗi đơn 2 ly cần (20 + 10) x 2 = 60
        long first, second, third;
        using (var scope = provider.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<CustomerOrderService>();
            var user = await UserIdAsync(scope, d.Customer);
            first = (await orders.CreateAsync(user, Order(d), DateTime.UtcNow)).OrderId;
            second = (await orders.CreateAsync(user, Order(d), DateTime.UtcNow)).OrderId;
            third = (await orders.CreateAsync(user, Order(d), DateTime.UtcNow)).OrderId;
        }
        using (var scope = provider.CreateScope())
        {
            var staff = scope.ServiceProvider.GetRequiredService<StaffOrderService>();
            var cashier = await UserIdAsync(scope, d.Cashier);
            var pending = await staff.ListAsync("Pending");
            Assert.Contains(pending, o => o.Detail.Summary.OrderId == first && o.RoundId > 0 && o.Detail.ContactName == "Khách nhận");
            var accepted = await staff.AcceptAsync(cashier, first, DateTime.UtcNow);
            Assert.True(accepted.TicketId > 0);
            Assert.Equal(accepted.TicketId, (await staff.AcceptAsync(cashier, first, DateTime.UtcNow)).TicketId);   // gọi lại: cùng phiếu
        }
        using (var scope = provider.CreateScope())
        {
            var staff = scope.ServiceProvider.GetRequiredService<StaffOrderService>();
            var cashier = await UserIdAsync(scope, d.Cashier);
            Assert.Equal(ApiErrorCodes.InsufficientStock, (await Fails(() => staff.AcceptAsync(cashier, second, DateTime.UtcNow))).Code);
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            Assert.Equal("Pending", (await db.SalesOrder.AsNoTracking().SingleAsync(o => o.OrderId == second)).Status);   // không ghi nửa chừng
            Assert.Equal(60, (await db.StockBalance.AsNoTracking().SingleAsync(b => b.IngredientId == d.Ingredient)).QuantityReserved);
        }
        using (var scope = provider.CreateScope()) // sau lỗi thì dùng scope mới, như mỗi yêu cầu HTTP
        {
            var staff = scope.ServiceProvider.GetRequiredService<StaffOrderService>();
            var cashier = await UserIdAsync(scope, d.Cashier);
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            Assert.Equal(ApiErrorCodes.Validation, (await Fails(() => staff.RejectAsync(cashier, second, "  ", DateTime.UtcNow))).Code);
            await staff.RejectAsync(cashier, second, "Hết nguyên liệu", DateTime.UtcNow);
            var rejected = await db.SalesOrder.AsNoTracking().SingleAsync(o => o.OrderId == second);
            Assert.Equal("Rejected", rejected.Status); Assert.Equal("Hết nguyên liệu", rejected.RejectReason); Assert.NotNull(rejected.RejectedBy); Assert.NotNull(rejected.RejectedAt);
            Assert.Equal(0, await db.PreparationTicket.CountAsync(t => db.OrderRound.Any(r => r.RoundId == t.RoundId && r.OrderId == second)));
            Assert.Equal(ApiErrorCodes.InvalidState, (await Fails(() => staff.RejectAsync(cashier, first, "Muộn", DateTime.UtcNow))).Code);   // đã tiếp nhận: không từ chối được
            Assert.Equal(ApiErrorCodes.InvalidState, (await Fails(() => staff.AcceptAsync(cashier, second, DateTime.UtcNow))).Code);
            var firstCode = await db.SalesOrder.Where(o => o.OrderId == first).Select(o => o.OrderCode).SingleAsync();
            Assert.Equal(1, await db.AuditEvent.CountAsync(a => a.ActionCode == "ORDER_ACCEPT" && a.EntityReference == firstCode));
        }
        using (var scope = provider.CreateScope()) // khách hủy xong thì thu ngân không tiếp nhận được
        {
            await scope.ServiceProvider.GetRequiredService<CustomerOrderService>().CancelAsync(await UserIdAsync(scope, d.Customer), third);
            Assert.Equal(ApiErrorCodes.InvalidState, (await Fails(async () => await scope.ServiceProvider.GetRequiredService<StaffOrderService>().AcceptAsync(await UserIdAsync(scope, d.Cashier), third, DateTime.UtcNow))).Code);
            Assert.Equal(ApiErrorCodes.Forbidden, (await Fails(async () => await scope.ServiceProvider.GetRequiredService<StaffOrderService>().AcceptAsync(await UserIdAsync(scope, d.Customer), third, DateTime.UtcNow))).Code);
        }
    }
}
