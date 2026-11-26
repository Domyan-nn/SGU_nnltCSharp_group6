using System.Net;
using System.Net.Http.Json;
using CafeManager.Core;
using CafeManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Kho nguyên liệu (UC13) qua HTTP thật và SQL Server thật: nhập kho, kiểm kê, duyệt hoặc từ chối chênh lệch.
// Quy tắc kiểm: chênh lệch chỉ vào tồn sau khi duyệt; người lập phiếu không tự duyệt; không điều chỉnh làm tồn thấp hơn lượng đang giữ.
public sealed class InventoryTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private ApiFactory? factory;
    private ApiFactory Factory => factory ??= new ApiFactory(fixture.Connection);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { if (factory != null) await factory.DisposeAsync(); }

    private HttpClient NewClient() => Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private async Task<(HttpClient Client, string Token)> LoginAsync(string userName)
    {
        var client = NewClient();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "secret1"))).StatusCode);
        var token = (await client.GetFromJsonAsync<AntiforgeryTokenResponse>("api/auth/antiforgery"))!.Token;
        return (client, token);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return client.SendAsync(request);
    }

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
    }

    private async Task<T> ReadDbAsync<T>(Func<CafeDbContext, Task<T>> read)
    {
        using var scope = Factory.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<CafeDbContext>());
    }

    private Task<StockBalance> StockAsync(long ingredientId) =>
        ReadDbAsync(db => db.StockBalance.AsNoTracking().SingleAsync(x => x.IngredientId == ingredientId));

    private async Task<long> PlaceOrderAsync(Data d, int quantity)
    {
        var (customer, token) = await LoginAsync(d.Customer);
        var response = await Send(customer, HttpMethod.Post, "api/orders", token, Order(d, quantity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!.OrderId;
    }

    private Task<string> WarehouseAsync(Data d) => SeedStaffAsync(Factory.Services, d.Suffix, "kho", Permissions.InventoryWork);
    private Task<string> ApproverAsync(Data d, string label) => SeedStaffAsync(Factory.Services, d.Suffix, label, Permissions.InventoryWork, Permissions.InventoryApprove);

    private static async Task<StockCountDto> CountOf(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<StockCountDto>())!;

    // ---------------------------------------------------------------- Nhập kho

    // Hết nguyên liệu thì đơn không tiếp nhận được; nhập kho xong thì tiếp nhận được.
    // Mỗi lần nhập ghi một lô (IngredientBatch), một dòng sổ kho Receipt và một dòng nhật ký; gửi lại cùng mã không cộng tồn lần hai.
    [SqlServerFact]
    public async Task Receipt_unblocks_acceptance_and_is_recorded_once()
    {
        var d = await SeedAsync(Factory.Services, stock: 0);
        var (warehouse, token) = await LoginAsync(await WarehouseAsync(d));
        var orderId = await PlaceOrderAsync(d, quantity: 1);   // 1 ly cần 20 + 10 = 30 g
        var (cashier, cashierToken) = await LoginAsync(d.Cashier);
        await AssertError(await Send(cashier, HttpMethod.Post, $"api/staff/orders/{orderId}/accept", cashierToken), HttpStatusCode.Conflict, ApiErrorCodes.InsufficientStock);

        var path = $"api/staff/inventory/{d.Ingredient}/receipts";
        var requestId = Guid.NewGuid();
        var first = await Send(warehouse, HttpMethod.Post, path, token, new StockReceiptRequest(requestId, 500.5m, "Nhập từ nhà cung cấp A"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var receipt = (await first.Content.ReadFromJsonAsync<StockReceiptResponse>())!;
        Assert.Equal(500.5m, receipt.QuantityOnHand);
        var replay = (await (await Send(warehouse, HttpMethod.Post, path, token, new StockReceiptRequest(requestId, 500.5m, "Nhập từ nhà cung cấp A"))).Content.ReadFromJsonAsync<StockReceiptResponse>())!;
        Assert.Equal((receipt.BatchId, receipt.MovementId), (replay.BatchId, replay.MovementId));
        await AssertError(await Send(warehouse, HttpMethod.Post, path, token, new StockReceiptRequest(requestId, 400m, null)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);

        Assert.Equal((500.5m, 0m), (await StockAsync(d.Ingredient)) is var s ? (s.QuantityOnHand, s.QuantityReserved) : default);
        var batch = await ReadDbAsync(db => db.IngredientBatch.AsNoTracking().SingleAsync(x => x.IngredientId == d.Ingredient));
        Assert.Equal((receipt.BatchId, 500.5m), (batch.BatchId, batch.QuantityReceived));
        var movement = await ReadDbAsync(db => db.StockMovement.AsNoTracking().SingleAsync(x => x.IngredientId == d.Ingredient));
        Assert.Equal((receipt.MovementId, "Receipt", 500.5m, "IngredientBatch", receipt.BatchId),
            (movement.MovementId, movement.MovementType, movement.Quantity, movement.SourceType, movement.SourceId));
        var audit = await ReadDbAsync(db => db.AuditEvent.AsNoTracking().SingleAsync(x => x.ActionCode == "STOCK_RECEIPT" && x.EntityReference == movement.MovementCode));
        Assert.Equal(("IngredientBatch", "Nhập từ nhà cung cấp A"), (audit.EntityName, audit.Reason));

        var item = (await warehouse.GetFromJsonAsync<List<InventoryItemDto>>("api/staff/inventory?q=" + Uri.EscapeDataString("NL " + d.Suffix)))!.Single();
        Assert.Equal((500.5m, 0m, 500.5m, (long?)null), (item.QuantityOnHand, item.QuantityReserved, item.QuantityAvailable, item.PendingCountId));

        Assert.Equal(HttpStatusCode.OK, (await Send(cashier, HttpMethod.Post, $"api/staff/orders/{orderId}/accept", cashierToken)).StatusCode);
        Assert.Equal((500.5m, 30m), (await StockAsync(d.Ingredient)) is var after ? (after.QuantityOnHand, after.QuantityReserved) : default);
        item = (await warehouse.GetFromJsonAsync<List<InventoryItemDto>>("api/staff/inventory?q=" + Uri.EscapeDataString("NL " + d.Suffix)))!.Single();
        Assert.Equal(470.5m, item.QuantityAvailable);   // tồn khả dụng = tồn sổ − đang giữ
    }

    [SqlServerFact]
    public async Task Receipt_validates_input_permission_and_ingredient_state()
    {
        var d = await SeedAsync(Factory.Services);
        var (warehouse, token) = await LoginAsync(await WarehouseAsync(d));
        var path = $"api/staff/inventory/{d.Ingredient}/receipts";

        foreach (var bad in new[] { 0m, -5m, 1.2345m, 1_000_001m })
            await AssertError(await Send(warehouse, HttpMethod.Post, path, token, new StockReceiptRequest(Guid.NewGuid(), bad, null)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(warehouse, HttpMethod.Post, path, token, new StockReceiptRequest(Guid.Empty, 10m, null)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(warehouse, HttpMethod.Post, path, token, new StockReceiptRequest(Guid.NewGuid(), 10m, new string('x', 501))), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(warehouse, HttpMethod.Post, "api/staff/inventory/999999999/receipts", token, new StockReceiptRequest(Guid.NewGuid(), 10m, null)), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        var stopped = await ReadDbAsync(async db =>
        {
            var i = new Ingredient { IngredientName = "Bột " + d.Suffix, UnitName = "g", IsActive = false };
            db.Add(i); await db.SaveChangesAsync();
            return i.IngredientId;
        });
        await AssertError(await Send(warehouse, HttpMethod.Post, $"api/staff/inventory/{stopped}/receipts", token, new StockReceiptRequest(Guid.NewGuid(), 10m, null)), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);

        // Quản lý thực đơn (MENU_MANAGE) hay thu ngân đều không có quyền kho.
        var (menuManager, menuToken) = await LoginAsync(await SeedManagerAsync(Factory.Services, d.Suffix));
        await AssertError(await Send(menuManager, HttpMethod.Post, path, menuToken, new StockReceiptRequest(Guid.NewGuid(), 10m, null)), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        var (cashier, cashierToken) = await LoginAsync(d.Cashier);
        await AssertError(await Send(cashier, HttpMethod.Post, path, cashierToken, new StockReceiptRequest(Guid.NewGuid(), 10m, null)), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await cashier.GetAsync("api/staff/inventory"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await NewClient().GetAsync("api/staff/inventory"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);

        Assert.Equal(1000m, (await StockAsync(d.Ingredient)).QuantityOnHand);   // không lần nào cộng tồn
        Assert.False(await ReadDbAsync(db => db.StockMovement.AnyAsync(x => x.IngredientId == d.Ingredient || x.IngredientId == stopped)));
    }

    // ---------------------------------------------------------------- Kiểm kê và duyệt

    [SqlServerFact]
    public async Task Count_waits_for_approval_by_another_person_then_adjusts_stock_down_and_up()
    {
        var d = await SeedAsync(Factory.Services);   // tồn sổ 1000
        var (warehouse, wToken) = await LoginAsync(await WarehouseAsync(d));
        var (manager, mToken) = await LoginAsync(await ApproverAsync(d, "ql"));
        var (manager2, m2Token) = await LoginAsync(await ApproverAsync(d, "ql2"));
        var counts = $"api/staff/inventory/{d.Ingredient}/counts";

        var created = await Send(warehouse, HttpMethod.Post, counts, wToken, new CreateStockCountRequest(950m, "Đổ vỡ 1 túi"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var down = await CountOf(created);
        Assert.Equal((1000m, 950m, -50m, StockCountStatuses.Pending, (string?)null), (down.BookQty, down.CountedQty, down.Difference, down.Status, down.ApprovedByName));
        Assert.Equal(1000m, (await StockAsync(d.Ingredient)).QuantityOnHand);   // chưa duyệt thì chưa đổi tồn
        var item = (await warehouse.GetFromJsonAsync<List<InventoryItemDto>>("api/staff/inventory?q=" + Uri.EscapeDataString("NL " + d.Suffix)))!.Single();
        Assert.Equal(down.CountId, item.PendingCountId);
        await AssertError(await Send(warehouse, HttpMethod.Post, counts, wToken, new CreateStockCountRequest(900m, null)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);   // một phiếu chờ mỗi nguyên liệu
        await AssertError(await Send(warehouse, HttpMethod.Post, $"api/staff/inventory/counts/{down.CountId}/approve", wToken), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);

        var approved = await Send(manager, HttpMethod.Post, $"api/staff/inventory/counts/{down.CountId}/approve", mToken);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var downDone = await CountOf(approved);
        Assert.Equal((StockCountStatuses.Approved, "NV ql " + d.Suffix, 950m), (downDone.Status, downDone.ApprovedByName, downDone.CurrentOnHand));
        Assert.Equal(950m, (await StockAsync(d.Ingredient)).QuantityOnHand);
        await AssertError(await Send(manager, HttpMethod.Post, $"api/staff/inventory/counts/{down.CountId}/approve", mToken), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);

        // Quản lý tự đếm thì không tự duyệt được; quản lý khác duyệt.
        var up = await CountOf(await Send(manager, HttpMethod.Post, counts, mToken, new CreateStockCountRequest(1000m, null)));
        Assert.Equal((950m, 50m), (up.BookQty, up.Difference));
        await AssertError(await Send(manager, HttpMethod.Post, $"api/staff/inventory/counts/{up.CountId}/approve", mToken), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(HttpStatusCode.OK, (await Send(manager2, HttpMethod.Post, $"api/staff/inventory/counts/{up.CountId}/approve", m2Token)).StatusCode);
        Assert.Equal(1000m, (await StockAsync(d.Ingredient)).QuantityOnHand);

        var movements = await ReadDbAsync(db => db.StockMovement.AsNoTracking().Where(x => x.IngredientId == d.Ingredient).OrderBy(x => x.MovementId).ToListAsync());
        Assert.Equal(new[] { ("AdjustmentOut", 50m, "StockCount", down.CountId), ("AdjustmentIn", 50m, "StockCount", up.CountId) },
            movements.Select(m => (m.MovementType, m.Quantity, m.SourceType, m.SourceId)).ToArray());
        var rows = await ReadDbAsync(db => db.StockCount.AsNoTracking().Where(x => x.IngredientId == d.Ingredient).OrderBy(x => x.CountId).ToListAsync());
        Assert.All(rows, r => Assert.Equal(StockCountStatuses.Approved, r.Status));
        Assert.True(await ReadDbAsync(db => db.AuditEvent.AnyAsync(a => a.ActionCode == "STOCK_COUNT" && a.Reason == "Đổ vỡ 1 túi")));
        Assert.Equal(2, await ReadDbAsync(db => db.AuditEvent.CountAsync(a => a.ActionCode == "STOCK_COUNT_APPROVE" && a.EntityName == "StockCount")));

        var pending = (await manager.GetFromJsonAsync<List<StockCountDto>>("api/staff/inventory/counts?status=Pending"))!;
        Assert.DoesNotContain(pending, c => c.IngredientId == d.Ingredient);
        var all = (await manager.GetFromJsonAsync<List<StockCountDto>>("api/staff/inventory/counts"))!.Where(c => c.IngredientId == d.Ingredient).ToList();
        Assert.Equal(2, all.Count);
    }

    [SqlServerFact]
    public async Task Approval_cannot_push_stock_below_reserved_and_rejection_keeps_stock()
    {
        var d = await SeedAsync(Factory.Services, stock: 100);
        var (warehouse, wToken) = await LoginAsync(await WarehouseAsync(d));
        var (manager, mToken) = await LoginAsync(await ApproverAsync(d, "ql"));
        var orderId = await PlaceOrderAsync(d, quantity: 1);
        var (cashier, cToken) = await LoginAsync(d.Cashier);
        Assert.Equal(HttpStatusCode.OK, (await Send(cashier, HttpMethod.Post, $"api/staff/orders/{orderId}/accept", cToken)).StatusCode);   // giữ 30

        var count = await CountOf(await Send(warehouse, HttpMethod.Post, $"api/staff/inventory/{d.Ingredient}/counts", wToken, new CreateStockCountRequest(20m, null)));
        Assert.Equal(-80m, count.Difference);
        await AssertError(await Send(manager, HttpMethod.Post, $"api/staff/inventory/counts/{count.CountId}/approve", mToken), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal((100m, 30m), (await StockAsync(d.Ingredient)) is var s ? (s.QuantityOnHand, s.QuantityReserved) : default);
        Assert.Equal(StockCountStatuses.Pending, (await ReadDbAsync(db => db.StockCount.AsNoTracking().SingleAsync(x => x.CountId == count.CountId))).Status);

        var reject = $"api/staff/inventory/counts/{count.CountId}/reject";
        await AssertError(await Send(manager, HttpMethod.Post, reject, mToken, new RejectStockCountRequest("  ")), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(warehouse, HttpMethod.Post, reject, wToken, new RejectStockCountRequest("Đếm sai")), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        var rejected = await Send(manager, HttpMethod.Post, reject, mToken, new RejectStockCountRequest("Đếm thiếu kệ sau, kiểm kê lại"));
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.Equal((StockCountStatuses.Rejected, "NV ql " + d.Suffix), (await CountOf(rejected)) is var r ? (r.Status, r.ApprovedByName) : default);
        await AssertError(await Send(manager, HttpMethod.Post, reject, mToken, new RejectStockCountRequest("Lần hai")), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(100m, (await StockAsync(d.Ingredient)).QuantityOnHand);
        Assert.False(await ReadDbAsync(db => db.StockMovement.AnyAsync(x => x.IngredientId == d.Ingredient && x.SourceType == "StockCount")));
        Assert.True(await ReadDbAsync(db => db.AuditEvent.AnyAsync(a => a.ActionCode == "STOCK_COUNT_REJECT" && a.Reason == "Đếm thiếu kệ sau, kiểm kê lại")));

        // Từ chối xong thì được kiểm kê lại; đầu vào sai bị chặn.
        var again = $"api/staff/inventory/{d.Ingredient}/counts";
        foreach (var bad in new[] { -1m, 1.2345m, 1_000_001m })
            await AssertError(await Send(warehouse, HttpMethod.Post, again, wToken, new CreateStockCountRequest(bad, null)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(warehouse, HttpMethod.Post, "api/staff/inventory/999999999/counts", wToken, new CreateStockCountRequest(1m, null)), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);
        await AssertError(await Send(manager, HttpMethod.Post, "api/staff/inventory/counts/999999999/approve", mToken), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);
        await AssertError(await warehouse.GetAsync("api/staff/inventory/counts?status=Xyz"), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        var recount = await Send(warehouse, HttpMethod.Post, again, wToken, new CreateStockCountRequest(0m, null));   // đếm thấy hết sạch vẫn lập được phiếu
        Assert.Equal(HttpStatusCode.Created, recount.StatusCode);
    }
}
