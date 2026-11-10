using CafeManager.Core;
using CafeManager.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CafeManager.Tests;

public sealed class DatabaseTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{

    [SqlServerFact]
    public async Task Addon_quantity_is_per_unit_and_ingredients_are_snapshotted()
    {
        var s = await CreateAsync(2);
        await using (var db = Open())
        {
            var product = await db.ProductVariant.SingleAsync(x => x.VariantId == s.Variant);
            var addon = new ProductVariant { ProductId = product.ProductId, VariantName = "Thêm", VariantType = "Addon", IsActive = true };
            db.Add(addon); await db.SaveChangesAsync();
            var recipe = new RecipeVersion { VariantId = addon.VariantId, EffectiveFrom = Now.AddDays(-1) };
            db.Add(recipe); await db.SaveChangesAsync();
            db.AddRange(new RecipeDetail { RecipeVersionId = recipe.RecipeVersionId, IngredientId = s.Ingredient, QuantityRequired = 10 },
                new OrderLineAddon { LineId = s.Line, AddonVariantId = addon.VariantId, Quantity = 2, AddonNameSnapshot = "Thêm", UnitPriceSnapshot = 5000 });
            await db.SaveChangesAsync();
            await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        }
        await using var check = Open();
        Assert.Equal(80, (await check.StockBalance.FindAsync(s.Ingredient))!.QuantityReserved);
        var amounts = await (from p in check.PreparationIngredient join u in check.PreparationUnit on p.UnitId equals u.UnitId
            where u.LineId == s.Line select p.QuantitySnapshot).ToListAsync();
        Assert.Equal(2, amounts.Count); Assert.All(amounts, x => Assert.Equal(40, x));
    }

    [SqlServerFact]
    public async Task Refund_replay_preserves_result_and_request_history()
    {
        var s = await CreateAsync(); var payment = await PayAsync(s);
        var refund = await ApproveRefundAsync(s, payment, 10000);
        var request = Guid.NewGuid(); long result;
        await using (var db = Open()) result = await new CafeDataCommands(db).RecordCashRefundAsync(request, refund, s.Shift, s.Staff);
        await using (var db = Open()) Assert.Equal(result, await new CafeDataCommands(db).RecordCashRefundAsync(request, refund, s.Shift, s.Staff));
        await using var check = Open();
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.RefundRequest SET Amount=1 WHERE RefundId={refund}"), 51003);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.RefundTransaction WHERE RefundTransactionId={result}"), 51003);
    }

    [SqlServerFact]
    public async Task Cancellation_units_cannot_cross_orders_and_decisions_are_immutable()
    {
        var first = await CreateAsync(); var second = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(second.Round, Now);
        await using var check = Open();
        var request = new CancellationRequest { OrderId = first.Order, RequestedBy = first.Staff, Reason = "Thử", Status = "Pending", CreatedAt = Now };
        check.Add(request); await check.SaveChangesAsync();
        var unit = await check.PreparationUnit.SingleAsync(x => x.LineId == second.Line);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.CancellationItem(CancellationId,UnitId) VALUES({request.CancellationId},{unit.UnitId})"), 51007);
        await check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CancellationRequest SET Status=N'Rejected',ApprovedBy={first.Approver},ApprovedAt={Now} WHERE CancellationId={request.CancellationId}");
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CancellationRequest SET Reason=N'Đổi' WHERE CancellationId={request.CancellationId}"), 51003);
    }

    [SqlServerFact]
    public async Task Stock_movements_and_audits_are_append_only()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        await using var check = Open();
        var audit = new AuditEvent { StaffId = s.Staff, ActionCode = "TEST", EntityName = "SalesOrder", EntityReference = s.Order.ToString(), CreatedAt = Now };
        check.Add(audit); await check.SaveChangesAsync();
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.AuditEvent SET ActionCode=N'EDIT' WHERE AuditId={audit.AuditId}"), 51003);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.StockMovement WHERE IngredientId={s.Ingredient}"), 51003);
    }

    [SqlServerFact]
    public async Task Identity_UserManager_creates_hash_and_profiles_reference_same_user()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Open());
        services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<CafeDbContext>();
        await using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<UserManager<IdentityUser>>();
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        var user = new IdentityUser { UserName = Guid.NewGuid().ToString("N") };
        var result = await manager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(x => x.Description)));
        Assert.NotEqual(password, user.PasswordHash);
        Assert.True(await manager.CheckPasswordAsync(user, password));
        var db = provider.GetRequiredService<CafeDbContext>();
        db.Add(new CustomerProfile { IdentityUserId = user.Id, FullName = "Khách đăng ký thử", Phone = "000" });
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.CustomerProfile.CountAsync(x => x.IdentityUserId == user.Id));
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(CustomerProfile))!.GetProperties(), p => p.Name.Contains("Password"));
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(StaffAccount))!.GetProperties(), p => p.Name.Contains("Password"));
    }

    private static readonly DateTime Now = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
    private CafeDbContext Open() => fixture.Open();
    private static async Task Rejected(Func<Task> action, params int[] numbers)
    {
        var error = await Record.ExceptionAsync(action);
        Assert.NotNull(error);
        var sql = error as SqlException ?? error.InnerException as SqlException;
        Assert.NotNull(sql);
        Assert.Contains(sql.Number, numbers);
    }

    private sealed record Scenario(long Staff, long Approver, long Customer, long Shift, long Table, long Order,
        long Round, long Line, long Variant, long Recipe, long Ingredient, long Counter);

    private async Task<Scenario> CreateAsync(int quantity = 1, decimal stock = 100, decimal recipeQty = 20)
    {
        await using var db = Open();
        var code = Guid.NewGuid().ToString("N");
        var user = new IdentityUser { UserName = code, NormalizedUserName = code.ToUpperInvariant() };
        var manager = new IdentityUser { UserName = code + "M", NormalizedUserName = code.ToUpperInvariant() + "M" };
        db.AddRange(user, manager);
        await db.SaveChangesAsync();
        var staff = new StaffAccount { IdentityUserId = user.Id, FullName = "Nhân viên thử", IsActive = true };
        var approver = new StaffAccount { IdentityUserId = manager.Id, FullName = "Quản lý thử", IsActive = true };
        var customer = new CustomerProfile { IdentityUserId = user.Id, FullName = "Khách thử", Phone = "0000000000" };
        var category = new Category { CategoryName = code, IsActive = true };
        var counter = new Counter { CounterCode = code, IsActive = true };
        var table = new CafeTable { TableCode = code, Status = "Ready" };
        var ingredient = new Ingredient { IngredientName = code, UnitName = "g", IsActive = true };
        db.AddRange(staff, approver, customer, category, counter, table, ingredient); await db.SaveChangesAsync();
        var product = new Product { CategoryId = category.CategoryId, ProductName = "Cà phê gốc", IsActive = true };
        var shift = new Shift { CounterId = counter.CounterId, OpenedBy = staff.StaffId, OpenedAt = Now, OpeningCash = 100000 };
        var order = new SalesOrder { OrderCode = code, CustomerId = customer.CustomerId, OrderType = "Web", Status = "Pending", CreatedAt = Now };
        db.AddRange(product, shift, order, new StockBalance { IngredientId = ingredient.IngredientId, QuantityOnHand = stock });
        await db.SaveChangesAsync();
        var variant = new ProductVariant { ProductId = product.ProductId, VariantName = "Nhỏ", VariantType = "Drink", IsActive = true };
        var round = new OrderRound { OrderId = order.OrderId, RoundNumber = 1, Status = "Draft" };
        db.AddRange(variant, round); await db.SaveChangesAsync();
        var recipe = new RecipeVersion { VariantId = variant.VariantId, EffectiveFrom = Now.AddDays(-1) };
        var line = new OrderLine { RoundId = round.RoundId, VariantId = variant.VariantId, Quantity = quantity,
            ProductNameSnapshot = product.ProductName, VariantNameSnapshot = variant.VariantName, UnitPriceSnapshot = 30000 };
        db.AddRange(recipe, line, new PriceHistory { VariantId = variant.VariantId, UnitPrice = 30000, EffectiveFrom = Now.AddDays(-1) });
        await db.SaveChangesAsync();
        db.Add(new RecipeDetail { RecipeVersionId = recipe.RecipeVersionId, IngredientId = ingredient.IngredientId, QuantityRequired = recipeQty });
        await db.SaveChangesAsync();
        return new(staff.StaffId, approver.StaffId, customer.CustomerId, shift.ShiftId, table.TableId, order.OrderId,
            round.RoundId, line.LineId, variant.VariantId, recipe.RecipeVersionId, ingredient.IngredientId, counter.CounterId);
    }

    private async Task<long> PayAsync(Scenario s, decimal amount = 30000)
    {
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        await using var context = Open();
        return await new CafeDataCommands(context).RecordCashPaymentAsync(Guid.NewGuid(), s.Order, s.Shift, s.Staff, amount);
    }

    private async Task<long> ApproveRefundAsync(Scenario s, long payment, decimal amount)
    {
        await using var db = Open();
        var request = new RefundRequest { RefundCode = Guid.NewGuid().ToString("N"), PaymentId = payment, Amount = amount,
            Status = "Approved", RequestedBy = s.Staff, ApprovedBy = s.Approver, ApprovedAt = Now };
        db.Add(request); await db.SaveChangesAsync(); return request.RefundId;
    }

    [SqlServerFact]
    public async Task Migration_creates_expected_schema_and_no_pending_model_changes()
    {
        await using var db = Open();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(44, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name<>N'__EFMigrationsHistory'").SingleAsync());
        Assert.Equal(44, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.key_constraints WHERE type='PK' AND OBJECT_NAME(parent_object_id)<>N'__EFMigrationsHistory'").SingleAsync());
        Assert.Equal(13, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE is_ms_shipped=0").SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.check_constraints").SingleAsync() >= 40);
        Assert.Equal(8, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE system_type_id=189").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE delete_referential_action<>0 OR is_disabled=1 OR is_not_trusted=1").SingleAsync());
    }

    [SqlServerFact]
    public async Task Seed_can_repeat_and_does_not_overwrite_edited_demo_or_create_accounts()
    {
        await using var db = Open();
        var accounts = await db.Users.CountAsync();
        await DevelopmentSeeder.SeedAsync(db);
        var count = await db.Product.CountAsync();
        var product = await db.Product.SingleAsync(x => x.ProductName == "[THỬ] Cà phê đen");
        product.IsActive = false; await db.SaveChangesAsync();
        await DevelopmentSeeder.SeedAsync(db);
        Assert.Equal(count, await db.Product.CountAsync());
        Assert.False((await db.Product.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId)).IsActive);
        Assert.Equal(accounts, await db.Users.CountAsync());
    }

    [SqlServerFact]
    public async Task Foreign_key_and_history_delete_are_rejected()
    {
        var s = await CreateAsync();
        await using var db = Open();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.ProductVariant SET ProductId={long.MaxValue} WHERE VariantId={s.Variant}"), 547);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.ProductVariant WHERE VariantId={s.Variant}"), 547);
    }

    [SqlServerFact]
    public async Task Compound_keys_and_positive_quantity_are_enforced()
    {
        var s = await CreateAsync();
        await using var db = Open();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.RecipeDetail(RecipeVersionId,IngredientId,QuantityRequired) VALUES ({s.Recipe},{s.Ingredient},20)"), 2627, 2601);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.OrderLine SET Quantity=0 WHERE LineId={s.Line}"), 547);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.OrderRound(OrderId,RoundNumber,Status) VALUES ({s.Order},1,N'Draft')"), 2627, 2601);
    }

    [SqlServerFact]
    public async Task Only_one_open_session_and_one_open_shift()
    {
        var s = await CreateAsync();
        await using var db = Open();
        db.Add(new ServiceSession { CurrentTableId = s.Table, OpenedAt = Now }); await db.SaveChangesAsync();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.ServiceSession(CurrentTableId,OpenedAt) VALUES({s.Table},{Now})"), 2601, 2627);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.Shift(CounterId,OpenedBy,OpenedAt,OpeningCash) VALUES({s.Counter},{s.Staff},{Now},0)"), 2601, 2627);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Shift SET ClosedAt={Now} WHERE ShiftId={s.Shift}"), 547);
    }

    [SqlServerFact]
    public async Task Concurrent_open_table_allows_one_session()
    {
        var s = await CreateAsync();
        async Task<bool> OpenTable()
        {
            await using var db = Open();
            try { db.Add(new ServiceSession { CurrentTableId = s.Table, OpenedAt = Now }); await db.SaveChangesAsync(); return true; }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return false; }
        }
        Assert.Single(await Task.WhenAll(OpenTable(), OpenTable()), x => x);
    }

    [SqlServerFact]
    public async Task Effective_price_and_recipe_intervals_cannot_overlap()
    {
        var s = await CreateAsync();
        await using var db = Open();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.PriceHistory(VariantId,UnitPrice,EffectiveFrom,EffectiveTo) VALUES({s.Variant},35000,{Now},{Now.AddDays(1)})"), 51001);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.RecipeVersion(VariantId,EffectiveFrom) VALUES({s.Variant},{Now})"), 51001);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PriceHistory SET EffectiveTo={Now} WHERE VariantId={s.Variant}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.PriceHistory(VariantId,UnitPrice,EffectiveFrom) VALUES({s.Variant},35000,{Now})");
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PriceHistory SET EffectiveTo=EffectiveFrom WHERE VariantId={s.Variant}"), 547);
    }

    [SqlServerFact]
    public async Task Concurrent_overlapping_prices_allow_only_one_commit()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.PriceHistory WHERE VariantId={s.Variant}");
        async Task<bool> Insert(DateTime from)
        {
            await using var db = Open();
            try
            {
                db.Add(new PriceHistory { VariantId = s.Variant, UnitPrice = 1, EffectiveFrom = from });
                await db.SaveChangesAsync(); return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 51001 or 1205 }) { return false; }
        }
        Assert.Single(await Task.WhenAll(Insert(Now), Insert(Now.AddHours(1))), x => x);
    }

    [SqlServerFact]
    public async Task Snapshot_survives_menu_and_price_changes()
    {
        var s = await CreateAsync();
        await using var db = Open();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE p SET ProductName=N'Tên mới' FROM dbo.Product p JOIN dbo.ProductVariant v ON v.ProductId=p.ProductId WHERE v.VariantId={s.Variant}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PriceHistory SET UnitPrice=50000 WHERE VariantId={s.Variant}");
        var line = await db.OrderLine.SingleAsync(x => x.LineId == s.Line);
        Assert.Equal("Cà phê gốc", line.ProductNameSnapshot);
        Assert.Equal(30000, line.UnitPriceSnapshot);
    }

    [SqlServerFact]
    public async Task Order_channel_requires_customer_or_session_correctly()
    {
        var s = await CreateAsync();
        await using var db = Open();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SalesOrder SET CustomerId=NULL WHERE OrderId={s.Order}"), 547);
        db.Add(new ServiceSession { CurrentTableId = s.Table, OpenedAt = Now }); await db.SaveChangesAsync();
        var session = await db.ServiceSession.SingleAsync(x => x.CurrentTableId == s.Table);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SalesOrder SET SessionId={session.SessionId} WHERE OrderId={s.Order}"), 547);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SalesOrder SET OrderType=N'Table' WHERE OrderId={s.Order}"), 547);
    }

    [SqlServerFact]
    public async Task Pending_round_has_no_ticket_then_accept_is_atomic_and_repeatable()
    {
        var s = await CreateAsync(2);
        await using (var db = Open())
        {
            Assert.False(await db.PreparationTicket.AnyAsync(x => x.RoundId == s.Round));
            Assert.Equal(0, (await db.StockBalance.FindAsync(s.Ingredient))!.QuantityReserved);
            await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.PreparationTicket(RoundId,Status,CreatedAt) VALUES({s.Round},N'Waiting',{Now})"), 51002);
        }
        long ticket;
        await using (var db = Open()) ticket = await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        await using (var db = Open()) Assert.Equal(ticket, await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now));
        await using var check = Open();
        Assert.Equal(2, await check.PreparationUnit.CountAsync(x => x.TicketId == ticket));
        Assert.Equal(40, (await check.StockBalance.FindAsync(s.Ingredient))!.QuantityReserved);
        Assert.Equal(2, await check.StockMovement.CountAsync(x => x.IngredientId == s.Ingredient && x.MovementType == "Reserve"));
    }

    [SqlServerFact]
    public async Task Insufficient_stock_rolls_back_entire_acceptance()
    {
        var s = await CreateAsync(6);
        await using (var db = Open())
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now));
        await using var check = Open();
        Assert.False(await check.PreparationTicket.AnyAsync(x => x.RoundId == s.Round));
        Assert.Equal("Draft", (await check.OrderRound.FindAsync(s.Round))!.Status);
        Assert.Equal("Pending", (await check.SalesOrder.FindAsync(s.Order))!.Status);
        Assert.Equal(0, (await check.StockBalance.FindAsync(s.Ingredient))!.QuantityReserved);
    }

    [SqlServerFact]
    public async Task Concurrent_acceptance_cannot_overreserve_last_stock()
    {
        var s = await CreateAsync(4);
        long secondRound;
        await using (var db = Open())
        {
            var order = new SalesOrder { OrderCode = Guid.NewGuid().ToString("N"), CustomerId = s.Customer, OrderType = "Web", Status = "Pending", CreatedAt = Now };
            db.Add(order); await db.SaveChangesAsync();
            var round = new OrderRound { OrderId = order.OrderId, RoundNumber = 1, Status = "Draft" };
            db.Add(round); await db.SaveChangesAsync(); secondRound = round.RoundId;
            db.Add(new OrderLine { RoundId = round.RoundId, VariantId = s.Variant, Quantity = 4,
                ProductNameSnapshot = "Thử", VariantNameSnapshot = "Thử", UnitPriceSnapshot = 30000 });
            await db.SaveChangesAsync();
        }
        async Task<bool> Accept(long id)
        {
            await using var db = Open();
            try { await new CafeDataCommands(db).AcceptRoundAsync(id, Now); return true; }
            catch (InvalidOperationException) { return false; }
            catch (SqlException ex) when (ex.Number == 1205) { return false; }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 1205 }) { return false; }
        }
        Assert.Single(await Task.WhenAll(Accept(s.Round), Accept(secondRound)), x => x);
        await using var check = Open();
        Assert.Equal(80, (await check.StockBalance.FindAsync(s.Ingredient))!.QuantityReserved);
        Assert.Equal(1, await check.PreparationTicket.CountAsync(x => x.RoundId == s.Round || x.RoundId == secondRound));
    }

    [SqlServerFact]
    public async Task Stock_check_and_rowversion_prevent_invalid_or_stale_write()
    {
        var s = await CreateAsync();
        await using var a = Open(); await using var b = Open();
        var first = (await a.StockBalance.FindAsync(s.Ingredient))!;
        var stale = (await b.StockBalance.FindAsync(s.Ingredient))!;
        first.QuantityReserved = 1; await a.SaveChangesAsync();
        stale.QuantityReserved = 2;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => b.SaveChangesAsync());
        await Rejected(() => a.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.StockBalance SET QuantityReserved=101 WHERE IngredientId={s.Ingredient}"), 547);
    }

    [SqlServerFact]
    public async Task Preparation_unit_cannot_reference_another_round_line()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        var other = await CreateAsync();
        await using var check = Open();
        var unit = await check.PreparationUnit.SingleAsync(x => x.LineId == s.Line);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PreparationUnit SET LineId={other.Line} WHERE UnitId={unit.UnitId}"), 51002);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.OrderLine SET RoundId={other.Round} WHERE LineId={s.Line}"), 51002);
    }

    [SqlServerFact]
    public async Task Starting_unit_consumes_snapshot_once()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        long unit;
        await using (var db = Open()) unit = (await db.PreparationUnit.SingleAsync(x => x.LineId == s.Line)).UnitId;
        await using (var db = Open())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.RecipeDetail SET QuantityRequired=50 WHERE RecipeVersionId={s.Recipe}");
            await new CafeDataCommands(db).StartUnitAsync(unit, Now);
        }
        await using (var db = Open())
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CafeDataCommands(db).StartUnitAsync(unit, Now));
        await using var check = Open();
        var stock = (await check.StockBalance.FindAsync(s.Ingredient))!;
        Assert.Equal(80, stock.QuantityOnHand); Assert.Equal(0, stock.QuantityReserved);
        Assert.Equal(1, await check.StockMovement.CountAsync(x => x.IngredientId == s.Ingredient && x.MovementType == "Consume"));
    }

    [SqlServerFact]
    public async Task Payment_replay_returns_same_result_and_different_payload_is_rejected()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        var request = Guid.NewGuid();
        async Task<long> Pay(decimal amount) { await using var db = Open(); return await new CafeDataCommands(db).RecordCashPaymentAsync(request, s.Order, s.Shift, s.Staff, amount); }
        var results = await Task.WhenAll(Pay(30000m), Pay(30000.00m));
        Assert.Equal(results[0], results[1]);
        await using var check = Open();
        Assert.Equal(1, await check.Payment.CountAsync(x => x.RequestId == request));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CafeDataCommands(check).RecordCashPaymentAsync(request, s.Order, s.Shift, s.Staff, 1));
    }

    [SqlServerFact]
    public async Task Successful_payment_cannot_be_edited_or_deleted()
    {
        var s = await CreateAsync(); var payment = await PayAsync(s);
        await using var db = Open();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Payment SET Amount=1 WHERE PaymentId={payment}"), 51003);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Payment WHERE PaymentId={payment}"), 51003);
    }

    [SqlServerFact]
    public async Task Refund_cannot_exceed_original_payment_even_across_requests()
    {
        var s = await CreateAsync(); var payment = await PayAsync(s);
        var r1 = await ApproveRefundAsync(s, payment, 20000);
        var r2 = await ApproveRefundAsync(s, payment, 20000);
        await using (var db = Open()) await new CafeDataCommands(db).RecordCashRefundAsync(Guid.NewGuid(), r1, s.Shift, s.Staff);
        await using (var db = Open())
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CafeDataCommands(db).RecordCashRefundAsync(Guid.NewGuid(), r2, s.Shift, s.Staff));
        await using var check = Open();
        var id = Guid.NewGuid();
        check.Add(new IdempotencyRecord { RequestId = id, RequestHash = "SQL-test", CreatedAt = Now }); await check.SaveChangesAsync();
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.RefundTransaction(RefundId,ShiftId,Amount,Method,Status,RequestId,ExecutedAt,CreatedBy) VALUES({r2},{s.Shift},20000,N'Cash',N'Succeeded',{id},{Now},{s.Staff})"), 51006);
    }

    [SqlServerFact]
    public async Task Concurrent_refunds_serialize_on_original_payment()
    {
        var s = await CreateAsync(); var payment = await PayAsync(s);
        var r1 = await ApproveRefundAsync(s, payment, 20000); var r2 = await ApproveRefundAsync(s, payment, 20000);
        async Task<bool> Refund(long id)
        {
            await using var db = Open();
            try { await new CafeDataCommands(db).RecordCashRefundAsync(Guid.NewGuid(), id, s.Shift, s.Staff); return true; }
            catch (InvalidOperationException) { return false; }
        }
        Assert.Single(await Task.WhenAll(Refund(r1), Refund(r2)), x => x);
    }

    [SqlServerFact]
    public async Task Reconciliation_counts_successful_cash_and_other_posted_entries_only()
    {
        var s = await CreateAsync(); var payment = await PayAsync(s, 30000);
        var refund = await ApproveRefundAsync(s, payment, 10000);
        await using (var db = Open())
        {
            await new CafeDataCommands(db).RecordCashRefundAsync(Guid.NewGuid(), refund, s.Shift, s.Staff);
            foreach (var (type, amount, status) in new[] { ("OtherIn",5000m,"Posted"), ("OtherOut",2000m,"Posted"), ("OtherIn",999m,"Voided") })
                db.Add(new ShiftCashEntry { CashEntryCode = Guid.NewGuid().ToString("N"), ShiftId = s.Shift, EntryType = type,
                    Amount = amount, Status = status, CreatedBy = s.Staff, CreatedAt = Now, Reason = "Chứng từ thử khác bán hàng" });
            var request = Guid.NewGuid();
            db.Add(new IdempotencyRecord { RequestId = request, RequestHash = "transfer-test", CreatedAt = Now }); await db.SaveChangesAsync();
            db.Add(new Payment { PaymentCode = Guid.NewGuid().ToString("N"), OrderId = s.Order, ShiftId = s.Shift,
                Amount = 40000, Method = "BankTransfer", Status = "Succeeded", PaidAt = Now, RequestId = request, CreatedBy = s.Staff });
            await db.SaveChangesAsync();
        }
        await using var check = Open();
        var result = await new CafeDataCommands(check).ReconcileShiftAsync(s.Shift);
        Assert.Equal(123000, result.ExpectedCash); Assert.Null(result.Difference);
    }

    [SqlServerFact]
    public async Task Closed_shift_rejects_receipts()
    {
        var s = await CreateAsync();
        await using (var db = Open())
        {
            await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Shift SET ClosedAt={Now},ClosedBy={s.Approver},CountedCash=100000 WHERE ShiftId={s.Shift}");
        }
        await using var check = Open();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CafeDataCommands(check).RecordCashPaymentAsync(Guid.NewGuid(), s.Order, s.Shift, s.Staff, 30000));
    }

    [SqlServerFact]
    public async Task Identity_profiles_are_unique_and_business_permissions_use_only_business_roles()
    {
        var s = await CreateAsync();
        await using var db = Open();
        var staff = (await db.StaffAccount.FindAsync(s.Staff))!;
        Assert.Equal((await db.CustomerProfile.FindAsync(s.Customer))!.IdentityUserId, staff.IdentityUserId);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.CustomerProfile(IdentityUserId,FullName,Phone) VALUES({staff.IdentityUserId},N'Trùng',N'000')"), 2601, 2627);
        var role = new Role { RoleName = "Quyền thử" }; var permission = new Permission { PermissionCode = Guid.NewGuid().ToString("N") };
        db.AddRange(role, permission); await db.SaveChangesAsync();
        db.AddRange(new StaffRole { StaffId = s.Staff, RoleId = role.RoleId },
            new RolePermission { RoleId = role.RoleId, PermissionId = permission.PermissionId }); await db.SaveChangesAsync();
        Assert.True(await new StaffPermissionReader(db).HasAsync(staff.IdentityUserId, permission.PermissionCode));
        staff.IsActive = false; await db.SaveChangesAsync();
        Assert.False(await new StaffPermissionReader(db).HasAsync(staff.IdentityUserId, permission.PermissionCode));
        Assert.DoesNotContain(db.Model.GetEntityTypes(), x => x.GetTableName() is "AspNetRoles" or "AspNetUserRoles");
    }

    [SqlServerFact]
    public async Task Independent_approval_and_per_unit_cancellation_key_are_enforced()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        await using var check = Open();
        var request = new CancellationRequest { OrderId = s.Order, RequestedBy = s.Staff, Reason = "Thử", Status = "Pending", CreatedAt = Now };
        check.Add(request); await check.SaveChangesAsync();
        var unit = await check.PreparationUnit.SingleAsync(x => x.LineId == s.Line);
        check.Add(new CancellationItem { CancellationId = request.CancellationId, UnitId = unit.UnitId }); await check.SaveChangesAsync();
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.CancellationItem(CancellationId,UnitId) VALUES({request.CancellationId},{unit.UnitId})"), 2601, 2627);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CancellationRequest SET Status=N'Approved',ApprovedBy={s.Staff},ApprovedAt={Now} WHERE CancellationId={request.CancellationId}"), 547);
    }

    [SqlServerFact]
    public async Task Allowed_addon_requires_addon_variant_and_positive_limit()
    {
        var s = await CreateAsync();
        await using var db = Open();
        var productId = (await db.ProductVariant.SingleAsync(x => x.VariantId == s.Variant)).ProductId;
        var addon = new ProductVariant { ProductId = productId, VariantName = "Thêm shot", VariantType = "Addon", IsActive = true };
        db.Add(addon); await db.SaveChangesAsync();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.AllowedAddon(ProductId,AddonVariantId,MaxPerUnit) VALUES({productId},{s.Variant},2)"), 51008);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.AllowedAddon(ProductId,AddonVariantId,MaxPerUnit) VALUES({productId},{addon.VariantId},0)"), 547);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.AllowedAddon(ProductId,AddonVariantId,MaxPerUnit) VALUES({productId},{addon.VariantId},2)");
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.AllowedAddon(ProductId,AddonVariantId,MaxPerUnit) VALUES({productId},{addon.VariantId},3)"), 2627, 2601);
        Assert.Equal(1, await db.AllowedAddon.CountAsync(x => x.ProductId == productId));
    }

    [SqlServerFact]
    public async Task Order_rejection_fields_are_all_or_nothing_and_need_rejected_status()
    {
        var s = await CreateAsync();
        await using var db = Open();
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SalesOrder SET RejectReason=N'Hết nguyên liệu' WHERE OrderId={s.Order}"), 547);
        await Rejected(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SalesOrder SET RejectReason=N'Hết nguyên liệu',RejectedBy={s.Staff},RejectedAt={Now} WHERE OrderId={s.Order}"), 547);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.SalesOrder SET Status=N'Rejected',RejectReason=N'Hết nguyên liệu',RejectedBy={s.Staff},RejectedAt={Now},ContactName=N'Khách nhận',ContactPhone=N'0900000000',PickupCode=N'A01' WHERE OrderId={s.Order}");
        var order = await db.SalesOrder.AsNoTracking().SingleAsync(x => x.OrderId == s.Order);
        Assert.Equal("Rejected", order.Status); Assert.Equal(s.Staff, order.RejectedBy); Assert.Equal("A01", order.PickupCode);
    }

    [SqlServerFact]
    public async Task Unit_delivery_fields_are_paired_and_need_delivered_status()
    {
        var s = await CreateAsync();
        await using (var db = Open()) await new CafeDataCommands(db).AcceptRoundAsync(s.Round, Now);
        await using var check = Open();
        var unit = await check.PreparationUnit.SingleAsync(x => x.LineId == s.Line);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PreparationUnit SET DeliveredBy={s.Staff} WHERE UnitId={unit.UnitId}"), 547);
        await Rejected(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PreparationUnit SET DeliveredBy={s.Staff},DeliveredAt={Now} WHERE UnitId={unit.UnitId}"), 547);
        await check.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PreparationUnit SET Status=N'Delivered',DeliveredBy={s.Staff},DeliveredAt={Now} WHERE UnitId={unit.UnitId}");
    }

    [SqlServerFact]
    public async Task Payment_external_reference_is_unique_when_present()
    {
        var s = await CreateAsync();
        await using var db = Open();
        var first = Guid.NewGuid(); var second = Guid.NewGuid(); var third = Guid.NewGuid();
        db.AddRange(new IdempotencyRecord { RequestId = first, RequestHash = "ref-1", CreatedAt = Now },
            new IdempotencyRecord { RequestId = second, RequestHash = "ref-2", CreatedAt = Now },
            new IdempotencyRecord { RequestId = third, RequestHash = "ref-3", CreatedAt = Now });
        await db.SaveChangesAsync();
        Payment Transfer(Guid request, string? reference) => new() { PaymentCode = Guid.NewGuid().ToString("N"), OrderId = s.Order, ShiftId = s.Shift,
            Amount = 10000, Method = "BankTransfer", Status = "Pending", RequestId = request, CreatedBy = s.Staff, ExternalReference = reference };
        db.Add(Transfer(first, "FT-" + first.ToString("N"))); db.Add(Transfer(second, null)); db.Add(Transfer(third, null));
        await db.SaveChangesAsync();
        var duplicate = (await db.Payment.AsNoTracking().SingleAsync(x => x.RequestId == first)).ExternalReference;
        var fourth = Guid.NewGuid(); var fifth = Guid.NewGuid();
        db.AddRange(new IdempotencyRecord { RequestId = fourth, RequestHash = "ref-4", CreatedAt = Now });
        await db.SaveChangesAsync();
        db.Add(Transfer(fourth, duplicate));
        var error = await Record.ExceptionAsync(() => db.SaveChangesAsync());
        var sql = error?.InnerException as SqlException;
        Assert.NotNull(sql); Assert.Contains(sql.Number, new[] { 2601, 2627 });
    }
}

