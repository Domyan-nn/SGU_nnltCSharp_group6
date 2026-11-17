using System.Net;
using System.Net.Http.Json;
using CafeManager.Core;
using CafeManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Kiểm thử máy chủ cho hợp đồng A1–A3 (HTTP thật, SQL Server thật):
// A1 danh mục và nguyên liệu, A2 ca và thu tiền mặt, A3 pha chế và tiến độ đơn.
public sealed class StaffA123ApiTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private ApiFactory? factory;
    private ApiFactory Factory => factory ??= new ApiFactory(fixture.Connection);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { if (factory != null) await factory.DisposeAsync(); }

    private HttpClient NewClient() => Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<ApiError> ErrorOf(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<ApiError>())!;

    private async Task<(HttpClient Client, string Token)> StaffClientAsync(string userName)
    {
        var client = NewClient();
        var login = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "secret1"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
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
        Assert.Equal(code, (await ErrorOf(response)).Code);
    }

    // Khách đặt một đơn (2 ly, mỗi ly 30.000 + 1 tùy chọn 5.000 = 70.000).
    private async Task<(HttpClient Customer, CreateOrderResponse Order)> PlaceOrderAsync(Data d, int quantity = 2)
    {
        var (customer, token) = await StaffClientAsync(d.Customer);
        var response = await Send(customer, HttpMethod.Post, "api/orders", token, Order(d, quantity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (customer, (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!);
    }

    private async Task<long> AcceptAsync(Data d, long orderId)
    {
        var (cashier, token) = await StaffClientAsync(d.Cashier);
        var response = await Send(cashier, HttpMethod.Post, $"api/staff/orders/{orderId}/accept", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AcceptOrderResponse>())!.TicketId;
    }

    private async Task<T> ReadDbAsync<T>(Func<CafeDbContext, Task<T>> read)
    {
        using var scope = Factory.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<CafeDbContext>());
    }

    // ---------------------------------------------------------------- A1: danh mục

    [SqlServerFact]
    public async Task A1_categories_crud_search_and_delete_rules()
    {
        var d = await SeedAsync(Factory.Services);
        var (client, token) = await StaffClientAsync(await SeedManagerAsync(Factory.Services, d.Suffix));
        var name = "Trà " + d.Suffix;

        var created = await Send(client, HttpMethod.Post, "api/staff/categories", token, new CategoryUpsertRequest(name, true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await AssertError(await Send(client, HttpMethod.Post, "api/staff/categories", token, new CategoryUpsertRequest(name, true)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Send(client, HttpMethod.Post, "api/staff/categories", token, new CategoryUpsertRequest("  ", true)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(client, HttpMethod.Post, "api/staff/categories", token, new CategoryUpsertRequest(new string('x', 201), true)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);

        var found = (await client.GetFromJsonAsync<List<CategoryDto>>("api/staff/categories?q=" + Uri.EscapeDataString(name)))!;
        var one = Assert.Single(found);
        Assert.Equal((id, name, true, 0), (one.CategoryId, one.CategoryName, one.IsActive, one.ProductCount));
        var seeded = (await client.GetFromJsonAsync<List<CategoryDto>>("api/staff/categories?q=" + Uri.EscapeDataString("DM " + d.Suffix)))!.Single();
        Assert.Equal(2, seeded.ProductCount);   // món cà phê và món thêm sữa của dữ liệu mẫu
        Assert.Contains((await client.GetFromJsonAsync<List<CategoryDto>>("api/staff/categories"))!, c => c.CategoryId == id);

        var renamed = await Send(client, HttpMethod.Put, $"api/staff/categories/{id}", token, new CategoryUpsertRequest("Trà đá " + d.Suffix, false));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal(id, (await renamed.Content.ReadFromJsonAsync<IdResponse>())!.Id);
        var after = (await client.GetFromJsonAsync<List<CategoryDto>>("api/staff/categories?q=" + Uri.EscapeDataString("Trà đá " + d.Suffix)))!.Single();
        Assert.False(after.IsActive);
        await AssertError(await Send(client, HttpMethod.Put, $"api/staff/categories/{id}", token, new CategoryUpsertRequest("DM " + d.Suffix, true)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Send(client, HttpMethod.Put, "api/staff/categories/999999999", token, new CategoryUpsertRequest("Không có", true)), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        await AssertError(await Send(client, HttpMethod.Delete, $"api/staff/categories/{seeded.CategoryId}", token), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);   // còn món
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, $"api/staff/categories/{id}", token)).StatusCode);
        await AssertError(await Send(client, HttpMethod.Delete, $"api/staff/categories/{id}", token), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        var (cashier, _) = await StaffClientAsync(d.Cashier);
        await AssertError(await cashier.GetAsync("api/staff/categories"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await NewClient().GetAsync("api/staff/categories"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);
    }

    // ---------------------------------------------------------------- A1: nguyên liệu

    [SqlServerFact]
    public async Task A1_ingredients_crud_stock_visibility_and_delete_rules()
    {
        var d = await SeedAsync(Factory.Services);
        var (client, token) = await StaffClientAsync(await SeedManagerAsync(Factory.Services, d.Suffix));
        var name = "Đường " + d.Suffix;

        var created = await Send(client, HttpMethod.Post, "api/staff/ingredients", token, new IngredientUpsertRequest(name, "g", true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var listed = (await client.GetFromJsonAsync<List<IngredientDto>>("api/staff/ingredients?q=" + Uri.EscapeDataString(name)))!.Single();
        Assert.Equal((id, "g", true, 0m, 0m, 0), (listed.IngredientId, listed.UnitName, listed.IsActive, listed.QuantityOnHand, listed.QuantityReserved, listed.RecipeCount));
        await AssertError(await Send(client, HttpMethod.Post, "api/staff/ingredients", token, new IngredientUpsertRequest(name, "g", true)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Send(client, HttpMethod.Post, "api/staff/ingredients", token, new IngredientUpsertRequest("Muối " + d.Suffix, " ", true)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(client, HttpMethod.Post, "api/staff/ingredients", token, new IngredientUpsertRequest("Muối " + d.Suffix, new string('g', 31), true)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);

        Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Put, $"api/staff/ingredients/{id}", token, new IngredientUpsertRequest(name, "kg", false))).StatusCode);
        var updated = (await client.GetFromJsonAsync<List<IngredientDto>>("api/staff/ingredients?q=" + Uri.EscapeDataString(name)))!.Single();
        Assert.Equal(("kg", false), (updated.UnitName, updated.IsActive));
        await AssertError(await Send(client, HttpMethod.Put, "api/staff/ingredients/999999999", token, new IngredientUpsertRequest("Không có", "g", true)), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        // Nguyên liệu mẫu: có tồn 1000 và nằm trong hai công thức (món chính và tùy chọn).
        var seeded = (await client.GetFromJsonAsync<List<IngredientDto>>("api/staff/ingredients?q=" + Uri.EscapeDataString("NL " + d.Suffix)))!.Single();
        Assert.Equal((1000m, 0m, 2), (seeded.QuantityOnHand, seeded.QuantityReserved, seeded.RecipeCount));
        await AssertError(await Send(client, HttpMethod.Delete, $"api/staff/ingredients/{d.Ingredient}", token), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);

        // Đã có phát sinh kho thì không xóa.
        var moved = (await (await Send(client, HttpMethod.Post, "api/staff/ingredients", token, new IngredientUpsertRequest("Sữa " + d.Suffix, "ml", true))).Content.ReadFromJsonAsync<IdResponse>())!.Id;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            db.Add(new StockMovement { MovementCode = Guid.NewGuid().ToString("N"), IngredientId = moved, MovementType = "Receipt", Quantity = 5, SourceType = "Test", SourceId = 1, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        await AssertError(await Send(client, HttpMethod.Delete, $"api/staff/ingredients/{moved}", token), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);

        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Delete, $"api/staff/ingredients/{id}", token)).StatusCode);
        Assert.False(await ReadDbAsync(db => db.StockBalance.AnyAsync(x => x.IngredientId == id)));   // hàng tồn rỗng cũng được xóa cùng
        await AssertError(await Send(client, HttpMethod.Delete, $"api/staff/ingredients/{id}", token), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        var (cashier, _) = await StaffClientAsync(d.Cashier);
        await AssertError(await cashier.GetAsync("api/staff/ingredients"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
    }

    // ---------------------------------------------------------------- A2: ca và thu tiền mặt

    [SqlServerFact]
    public async Task A2_shift_open_cash_payment_idempotency_summary_and_close()
    {
        var d = await SeedAsync(Factory.Services);
        var nameA = await SeedStaffAsync(Factory.Services, d.Suffix, "casha", Permissions.PaymentCollect, Permissions.OrderReceive);
        var nameB = await SeedStaffAsync(Factory.Services, d.Suffix, "cashb", Permissions.PaymentCollect);
        var counter1 = await SeedCounterAsync(Factory.Services, "Q1-" + d.Suffix);
        var counter2 = await SeedCounterAsync(Factory.Services, "Q2-" + d.Suffix);
        var (a, ta) = await StaffClientAsync(nameA);
        var (b, tb) = await StaffClientAsync(nameB);

        Assert.Contains((await a.GetFromJsonAsync<List<CounterDto>>("api/staff/counters"))!, c => c.CounterId == counter1 && c.CounterCode == "Q1-" + d.Suffix);
        Assert.Null((await a.GetFromJsonAsync<CurrentShiftResponse>("api/staff/shifts/current"))!.Shift);

        // Đơn chưa tiếp nhận thì chưa thu được.
        var (customer, order) = await PlaceOrderAsync(d);
        Assert.Equal(70000, order.Total);
        Assert.DoesNotContain((await a.GetFromJsonAsync<List<PayableOrderDto>>("api/staff/payments/payable"))!, o => o.OrderId == order.OrderId);
        Assert.Equal(HttpStatusCode.OK, (await Send(a, HttpMethod.Post, $"api/staff/orders/{order.OrderId}/accept", ta)).StatusCode);

        // Chưa mở ca thì không thu được (409), dù đơn đã tiếp nhận.
        var firstRequest = Guid.NewGuid();
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(firstRequest, order.OrderId, 100000)), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);

        // Mở ca: kiểm tra đầu vào, một ca mỗi nhân viên và mỗi quầy.
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/shifts", ta, new OpenShiftRequest(counter1, -1)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/shifts", ta, new OpenShiftRequest(999999999, 0)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        var opened = await Send(a, HttpMethod.Post, "api/staff/shifts", ta, new OpenShiftRequest(counter1, 100000));
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var shift = (await opened.Content.ReadFromJsonAsync<ShiftDto>())!;
        Assert.Equal((counter1, "Q1-" + d.Suffix, 100000m, (DateTime?)null, (decimal?)null), (shift.CounterId, shift.CounterCode, shift.OpeningCash, shift.ClosedAt, shift.CountedCash));
        Assert.Equal("NV casha " + d.Suffix, shift.OpenedByName);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/shifts", ta, new OpenShiftRequest(counter2, 0)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);   // đã có ca
        await AssertError(await Send(b, HttpMethod.Post, "api/staff/shifts", tb, new OpenShiftRequest(counter1, 0)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);   // quầy đã có ca
        Assert.Equal(HttpStatusCode.Created, (await Send(b, HttpMethod.Post, "api/staff/shifts", tb, new OpenShiftRequest(counter2, 50000))).StatusCode);
        Assert.Equal(shift.ShiftId, (await a.GetFromJsonAsync<CurrentShiftResponse>("api/staff/shifts/current"))!.Shift!.ShiftId);

        var payable = (await a.GetFromJsonAsync<List<PayableOrderDto>>("api/staff/payments/payable"))!.Single(o => o.OrderId == order.OrderId);
        Assert.Equal((70000m, 0m, 70000m, "Serving"), (payable.Total, payable.Paid, payable.Remaining, payable.Status));
        Assert.Equal(order.PickupCode, payable.PickupCode);

        // Thu tiền: thiếu tiền và số tiền sai bị từ chối; đủ thì có tiền thối.
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.NewGuid(), order.OrderId, 50000)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.NewGuid(), order.OrderId, 0)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.Empty, order.OrderId, 100000)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        var requestId = Guid.NewGuid();
        var paid = await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(requestId, order.OrderId, 100000));
        Assert.Equal(HttpStatusCode.Created, paid.StatusCode);
        var receipt = (await paid.Content.ReadFromJsonAsync<CashPaymentResponse>())!;
        Assert.Equal((70000m, 100000m, 30000m), (receipt.AmountDue, receipt.AmountReceived, receipt.Change));

        // Gửi lại cùng mã yêu cầu trả đúng phiếu cũ; đổi nội dung thì 409; thu lần nữa khi hết nợ thì 409.
        var replay = (await (await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(requestId, order.OrderId, 100000))).Content.ReadFromJsonAsync<CashPaymentResponse>())!;
        Assert.Equal(receipt, replay);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(requestId, order.OrderId, 120000)), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.NewGuid(), order.OrderId, 100000)), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(1, await ReadDbAsync(db => db.Payment.CountAsync(p => p.OrderId == order.OrderId)));
        Assert.DoesNotContain((await a.GetFromJsonAsync<List<PayableOrderDto>>("api/staff/payments/payable"))!, o => o.OrderId == order.OrderId);

        // Đơn chưa tiếp nhận không thu được.
        var (_, pending) = await PlaceOrderAsync(d);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.NewGuid(), pending.OrderId, 100000)), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.NewGuid(), 999999999, 100000)), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        // Khách thấy số đã trả (B2).
        var detail = (await customer.GetFromJsonAsync<OrderDetailDto>($"api/orders/{order.OrderId}"))!;
        Assert.Equal(70000m, detail.Progress!.Paid);

        // Đối soát: tiền mở ca + thu tiền mặt; người khác không xem hay đóng được ca của mình (404).
        var summary = (await a.GetFromJsonAsync<ShiftSummaryDto>($"api/staff/shifts/{shift.ShiftId}/summary"))!;
        Assert.Equal((100000m, 70000m, 0m, 170000m, (decimal?)null, (decimal?)null), (summary.OpeningCash, summary.CashPayments, summary.CashRefunds, summary.ExpectedCash, summary.CountedCash, summary.Difference));
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"api/staff/shifts/{shift.ShiftId}/summary")).StatusCode);
        await AssertError(await Send(b, HttpMethod.Post, $"api/staff/shifts/{shift.ShiftId}/close", tb, new CloseShiftRequest(1, null)), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        // Đóng ca: kiểm tra đầu vào, nhận bảng đối soát có chênh lệch, đóng lần hai bị từ chối, sau đó không thu được nữa.
        await AssertError(await Send(a, HttpMethod.Post, $"api/staff/shifts/{shift.ShiftId}/close", ta, new CloseShiftRequest(-1, null)), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(a, HttpMethod.Post, $"api/staff/shifts/{shift.ShiftId}/close", ta, new CloseShiftRequest(1, new string('n', 501))), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        var closed = await Send(a, HttpMethod.Post, $"api/staff/shifts/{shift.ShiftId}/close", ta, new CloseShiftRequest(169000, "Thiếu 1.000"));
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        var result = (await closed.Content.ReadFromJsonAsync<ShiftSummaryDto>())!;
        Assert.Equal((170000m, 169000m, -1000m), (result.ExpectedCash, result.CountedCash, result.Difference));
        await AssertError(await Send(a, HttpMethod.Post, $"api/staff/shifts/{shift.ShiftId}/close", ta, new CloseShiftRequest(169000, null)), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Null((await a.GetFromJsonAsync<CurrentShiftResponse>("api/staff/shifts/current"))!.Shift);
        var (_, second) = await PlaceOrderAsync(d);
        Assert.Equal(HttpStatusCode.OK, (await Send(a, HttpMethod.Post, $"api/staff/orders/{second.OrderId}/accept", ta)).StatusCode);
        await AssertError(await Send(a, HttpMethod.Post, "api/staff/payments/cash", ta, new CashPaymentRequest(Guid.NewGuid(), second.OrderId, 100000)), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(HttpStatusCode.Created, (await Send(a, HttpMethod.Post, "api/staff/shifts", ta, new OpenShiftRequest(counter1, 0))).StatusCode);   // quầy trống lại sau khi đóng ca

        // Phân quyền.
        var (cashierOnlyReceive, _) = await StaffClientAsync(d.Cashier);
        await AssertError(await cashierOnlyReceive.GetAsync("api/staff/counters"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await NewClient().GetAsync("api/staff/shifts/current"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);
    }

    // ---------------------------------------------------------------- A3: pha chế

    [SqlServerFact]
    public async Task A3_preparation_queue_unit_lifecycle_stock_ticket_and_progress()
    {
        var d = await SeedAsync(Factory.Services);
        var baristaName = await SeedStaffAsync(Factory.Services, d.Suffix, "barista", Permissions.PreparationWork);
        var (barista, tb) = await StaffClientAsync(baristaName);
        var (customer, order) = await PlaceOrderAsync(d);
        var ticket = await AcceptAsync(d, order.OrderId);

        List<PreparationUnitDto> Mine(IEnumerable<PreparationUnitDto> all) => all.Where(u => u.OrderId == order.OrderId).OrderBy(u => u.UnitNumber).ToList();
        async Task<List<PreparationUnitDto>> QueueAsync() => Mine((await barista.GetFromJsonAsync<List<PreparationUnitDto>>("api/staff/preparation"))!);

        var queue = await QueueAsync();
        Assert.Equal(2, queue.Count);
        Assert.All(queue, u =>
        {
            Assert.Equal((ticket, order.OrderCode, order.PickupCode, "Cà phê " + d.Suffix, "Vừa", 2, PreparationStatuses.Waiting),
                (u.TicketId, u.OrderCode, u.PickupCode, u.ProductName, u.VariantName, u.LineQuantity, u.Status));
            var addon = Assert.Single(u.Addons);
            Assert.Equal(("Thêm sữa " + d.Suffix, 1, 5000m), (addon.Name, addon.QuantityPerUnit, addon.UnitPrice));
            Assert.True(u.AcceptedAt > DateTime.UtcNow.AddMinutes(-5));
        });
        Assert.Equal([1, 2], queue.Select(u => u.UnitNumber));
        var (u1, u2) = (queue[0].UnitId, queue[1].UnitId);

        // Chuyển trạng thái đúng thứ tự; sai thứ tự hoặc lặp lại bị 409; không có thì 404.
        await AssertError(await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u2}/ready", tb), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        await AssertError(await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u2}/deliver", tb), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u1}/start", tb)).StatusCode);
        await AssertError(await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u1}/start", tb), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        await AssertError(await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u1}/deliver", tb), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(PreparationStatuses.Preparing, (await QueueAsync()).Single(u => u.UnitId == u1).Status);
        await AssertError(await Send(barista, HttpMethod.Post, "api/staff/preparation/999999999/start", tb), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);

        // Bắt đầu một ly trừ nguyên liệu của đúng ly đó (20 + 10), phần ly kia vẫn đang giữ.
        var stock = await ReadDbAsync(db => db.StockBalance.AsNoTracking().SingleAsync(x => x.IngredientId == d.Ingredient));
        Assert.Equal((970m, 30m), (stock.QuantityOnHand, stock.QuantityReserved));

        Assert.Equal(HttpStatusCode.NoContent, (await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u1}/ready", tb)).StatusCode);
        await AssertError(await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u1}/ready", tb), HttpStatusCode.Conflict, ApiErrorCodes.InvalidState);
        Assert.Equal(PreparationStatuses.Ready, (await QueueAsync()).Single(u => u.UnitId == u1).Status);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u1}/deliver", tb)).StatusCode);
        Assert.DoesNotContain(await QueueAsync(), u => u.UnitId == u1);   // đã giao thì rời hàng đợi

        var unit = await ReadDbAsync(db => db.PreparationUnit.AsNoTracking().SingleAsync(x => x.UnitId == u1));
        var staffId = await ReadDbAsync(db => (from s in db.StaffAccount join u in db.Users on s.IdentityUserId equals u.Id where u.UserName == baristaName select s.StaffId).SingleAsync());
        Assert.Equal(("Delivered", staffId), (unit.Status, unit.DeliveredBy));
        Assert.NotNull(unit.DeliveredAt);
        Assert.Equal("Completed", await ReadDbAsync(db => db.PreparationAttempt.Where(a => a.UnitId == u1).Select(a => a.Outcome).SingleAsync()));
        Assert.Equal("Preparing", await ReadDbAsync(db => db.PreparationTicket.Where(t => t.TicketId == ticket).Select(t => t.Status).SingleAsync()));

        // Khách thấy tiến độ (B2): một ly chờ, một ly đã giao, chưa trả.
        var progress = (await customer.GetFromJsonAsync<OrderDetailDto>($"api/orders/{order.OrderId}"))!.Progress!;
        Assert.Equal((1, 0, 0, 1, 0m), (progress.Waiting, progress.Preparing, progress.Ready, progress.Delivered, progress.Paid));

        foreach (var step in new[] { "start", "ready", "deliver" })
            Assert.Equal(HttpStatusCode.NoContent, (await Send(barista, HttpMethod.Post, $"api/staff/preparation/{u2}/{step}", tb)).StatusCode);
        Assert.Equal("Completed", await ReadDbAsync(db => db.PreparationTicket.Where(t => t.TicketId == ticket).Select(t => t.Status).SingleAsync()));
        var after = await ReadDbAsync(db => db.StockBalance.AsNoTracking().SingleAsync(x => x.IngredientId == d.Ingredient));
        Assert.Equal((940m, 0m), (after.QuantityOnHand, after.QuantityReserved));
        progress = (await customer.GetFromJsonAsync<OrderDetailDto>($"api/orders/{order.OrderId}"))!.Progress!;
        Assert.Equal((0, 0, 0, 2), (progress.Waiting, progress.Preparing, progress.Ready, progress.Delivered));

        // Phân quyền: thu ngân không vào màn hình pha chế, pha chế không vào quầy thu tiền.
        var (cashier, _) = await StaffClientAsync(d.Cashier);
        await AssertError(await cashier.GetAsync("api/staff/preparation"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await barista.GetAsync("api/staff/payments/payable"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await NewClient().GetAsync("api/staff/preparation"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);
    }

    [SqlServerFact]
    public async Task A3_two_baristas_starting_the_same_unit_get_one_success_and_one_conflict()
    {
        var d = await SeedAsync(Factory.Services);
        var baristaName = await SeedStaffAsync(Factory.Services, d.Suffix, "barista", Permissions.PreparationWork);
        var (first, t1) = await StaffClientAsync(baristaName);
        var (second, t2) = await StaffClientAsync(baristaName);
        var (_, order) = await PlaceOrderAsync(d, quantity: 1);
        await AcceptAsync(d, order.OrderId);
        var unit = (await first.GetFromJsonAsync<List<PreparationUnitDto>>("api/staff/preparation"))!.Single(u => u.OrderId == order.OrderId);

        var results = await Task.WhenAll(
            Send(first, HttpMethod.Post, $"api/staff/preparation/{unit.UnitId}/start", t1),
            Send(second, HttpMethod.Post, $"api/staff/preparation/{unit.UnitId}/start", t2));
        Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Conflict], results.Select(r => r.StatusCode).OrderBy(s => (int)s).ToArray());   // 204 < 409
        var stock = await ReadDbAsync(db => db.StockBalance.AsNoTracking().SingleAsync(x => x.IngredientId == d.Ingredient));
        Assert.Equal(970m, stock.QuantityOnHand);   // chỉ trừ một lần
    }
}
