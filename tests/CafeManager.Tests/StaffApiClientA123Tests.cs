using System.Net;
using System.Net.Http.Json;
using CafeManager.Client;
using CafeManager.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Lớp StaffApi THẬT của Desktop (ApiClient + cookie + antiforgery) gọi máy chủ thật trong bộ nhớ và SQL Server thật cho A1–A3.
// Bằng chứng rằng đường dẫn, JSON và mã lỗi mà C dựng màn hình dựa vào khớp với máy chủ. Chưa phải kiểm thử giao diện Windows Forms.
public sealed class StaffApiClientA123Tests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private ApiFactory? factory;
    private ApiFactory Factory => factory ??= new ApiFactory(fixture.Connection);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { if (factory != null) await factory.DisposeAsync(); }

    private sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly CookieContainer jar = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var header = jar.GetCookieHeader(request.RequestUri!);
            if (header.Length > 0) request.Headers.TryAddWithoutValidation("Cookie", header);
            var response = await base.SendAsync(request, ct);
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                foreach (var cookie in cookies) jar.SetCookies(request.RequestUri!, cookie);
            return response;
        }
    }

    private async Task<(ApiClient Api, StaffApi Staff)> LoginAsync(string userName)
    {
        var api = new ApiClient(new ApiClientOptions(new Uri("http://localhost/"), TimeSpan.FromSeconds(15)), new CookieHandler(Factory.Server.CreateHandler()));
        var login = await api.LoginAsync(userName, "secret1");
        Assert.True(login.Ok, login.Message);
        return (api, new StaffApi(api));
    }

    private async Task<CreateOrderResponse> PlaceOrderAsync(Data d, int quantity = 2)
    {
        var customer = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsJsonAsync("api/auth/login", new LoginRequest(d.Customer, "secret1"))).StatusCode);
        var token = (await customer.GetFromJsonAsync<AntiforgeryTokenResponse>("api/auth/antiforgery"))!.Token;
        var request = new HttpRequestMessage(HttpMethod.Post, "api/orders") { Content = JsonContent.Create(Order(d, quantity)) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await customer.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!;
    }

    [SqlServerFact]
    public async Task A1_manager_manages_categories_and_ingredients_through_the_desktop_client()
    {
        var d = await SeedAsync(Factory.Services);
        var (api, staff) = await LoginAsync(await SeedManagerAsync(Factory.Services, d.Suffix));
        using var _ = api;

        var saved = await staff.SaveCategoryAsync(null, new CategoryUpsertRequest("Sinh tố " + d.Suffix, true));
        Assert.True(saved.Ok, saved.Message);
        var list = await staff.CategoriesAsync("Sinh tố " + d.Suffix);
        Assert.Equal(saved.Value!.Id, Assert.Single(list.Value!).CategoryId);
        var duplicate = await staff.SaveCategoryAsync(null, new CategoryUpsertRequest("Sinh tố " + d.Suffix, true));
        Assert.Equal((false, 409, ApiErrorCodes.Conflict), (duplicate.Ok, duplicate.StatusCode, duplicate.Error!.Code));
        Assert.True((await staff.SaveCategoryAsync(saved.Value.Id, new CategoryUpsertRequest("Sinh tố mới " + d.Suffix, false))).Ok);
        var inUse = (await staff.CategoriesAsync("DM " + d.Suffix)).Value!.Single();
        var blocked = await staff.DeleteCategoryAsync(inUse.CategoryId);
        Assert.Equal((false, 409, ApiErrorCodes.Conflict), (blocked.Ok, blocked.StatusCode, blocked.Error!.Code));
        Assert.True((await staff.DeleteCategoryAsync(saved.Value.Id)).Ok);
        Assert.Equal(404, (await staff.DeleteCategoryAsync(saved.Value.Id)).StatusCode);

        var ingredient = await staff.SaveIngredientAsync(null, new IngredientUpsertRequest("Trân châu " + d.Suffix, "g", true));
        Assert.True(ingredient.Ok, ingredient.Message);
        var row = (await staff.IngredientsAsync("Trân châu " + d.Suffix)).Value!.Single();
        Assert.Equal((0m, 0), (row.QuantityOnHand, row.RecipeCount));
        Assert.Equal(409, (await staff.DeleteIngredientAsync(d.Ingredient)).StatusCode);   // đang nằm trong công thức
        Assert.True((await staff.DeleteIngredientAsync(ingredient.Value!.Id)).Ok);

    }

    // UC13 qua máy khách thật: nhân viên kho nhập kho và lập phiếu kiểm kê, quản lý duyệt hoặc từ chối.
    [SqlServerFact]
    public async Task UC13_warehouse_receives_and_counts_manager_approves_through_the_desktop_client()
    {
        var d = await SeedAsync(Factory.Services);   // tồn 1000
        var (warehouseApi, warehouse) = await LoginAsync(await SeedStaffAsync(Factory.Services, d.Suffix, "kho", Permissions.InventoryWork));
        var (managerApi, manager) = await LoginAsync(await SeedStaffAsync(Factory.Services, d.Suffix, "ql", Permissions.InventoryWork, Permissions.InventoryApprove));
        using var _1 = warehouseApi; using var _2 = managerApi;

        var requestId = Guid.NewGuid();
        var received = await warehouse.ReceiveStockAsync(d.Ingredient, new StockReceiptRequest(requestId, 250m, null));
        Assert.True(received.Ok, received.Message);
        Assert.Equal(1250m, received.Value!.QuantityOnHand);
        Assert.Equal(received.Value.MovementId, (await warehouse.ReceiveStockAsync(d.Ingredient, new StockReceiptRequest(requestId, 250m, null))).Value!.MovementId);
        var invalid = await warehouse.ReceiveStockAsync(d.Ingredient, new StockReceiptRequest(Guid.NewGuid(), 0m, null));
        Assert.Equal((false, 400, ApiErrorCodes.Validation), (invalid.Ok, invalid.StatusCode, invalid.Error!.Code));

        var item = (await warehouse.InventoryAsync("NL " + d.Suffix)).Value!.Single();
        Assert.Equal((1250m, 1250m), (item.QuantityOnHand, item.QuantityAvailable));
        var count = await warehouse.CreateStockCountAsync(d.Ingredient, new CreateStockCountRequest(1240m, "Hao hụt"));
        Assert.True(count.Ok, count.Message);
        Assert.Equal(-10m, count.Value!.Difference);
        Assert.Equal(403, (await warehouse.ApproveStockCountAsync(count.Value.CountId)).StatusCode);
        Assert.Contains((await manager.StockCountsAsync(StockCountStatuses.Pending)).Value!, c => c.CountId == count.Value.CountId);
        var approved = await manager.ApproveStockCountAsync(count.Value.CountId);
        Assert.True(approved.Ok, approved.Message);
        Assert.Equal((StockCountStatuses.Approved, 1240m), (approved.Value!.Status, approved.Value.CurrentOnHand));

        var second = (await warehouse.CreateStockCountAsync(d.Ingredient, new CreateStockCountRequest(1000m, null))).Value!;
        var rejected = await manager.RejectStockCountAsync(second.CountId, "Đếm lại");
        Assert.Equal(StockCountStatuses.Rejected, rejected.Value!.Status);
        Assert.Equal(1240m, (await warehouse.InventoryAsync("NL " + d.Suffix)).Value!.Single().QuantityOnHand);
        Assert.Equal(409, (await manager.RejectStockCountAsync(second.CountId, "Lần hai")).StatusCode);
    }

    [SqlServerFact]
    public async Task A2_cashier_opens_shift_collects_cash_and_closes_through_the_desktop_client()
    {
        var d = await SeedAsync(Factory.Services);
        var counter = await SeedCounterAsync(Factory.Services, "QT-" + d.Suffix);
        var (api, staff) = await LoginAsync(await SeedStaffAsync(Factory.Services, d.Suffix, "thungan", Permissions.PaymentCollect, Permissions.OrderReceive));
        using var _ = api;
        var order = await PlaceOrderAsync(d);

        Assert.Contains((await staff.CountersAsync()).Value!, c => c.CounterId == counter);
        Assert.Null((await staff.CurrentShiftAsync()).Value!.Shift);
        Assert.True((await staff.AcceptAsync(order.OrderId)).Ok);
        var noShift = await staff.PayCashAsync(Guid.NewGuid(), order.OrderId, 100000);
        Assert.Equal((false, 409, ApiErrorCodes.InvalidState), (noShift.Ok, noShift.StatusCode, noShift.Error!.Code));

        var shift = (await staff.OpenShiftAsync(counter, 50000)).Value!;
        Assert.Equal(shift.ShiftId, (await staff.CurrentShiftAsync()).Value!.Shift!.ShiftId);
        var payable = (await staff.PayableOrdersAsync()).Value!.Single(o => o.OrderId == order.OrderId);
        Assert.Equal(70000m, payable.Remaining);

        var requestId = Guid.NewGuid();   // một mã cho mỗi lần bấm Thu tiền; gửi lại khi chưa biết kết quả
        var short_ = await staff.PayCashAsync(Guid.NewGuid(), order.OrderId, 60000);
        Assert.Equal((false, 400, ApiErrorCodes.Validation), (short_.Ok, short_.StatusCode, short_.Error!.Code));
        var paid = (await staff.PayCashAsync(requestId, order.OrderId, 100000)).Value!;
        Assert.Equal((70000m, 30000m), (paid.AmountDue, paid.Change));
        Assert.Equal(paid, (await staff.PayCashAsync(requestId, order.OrderId, 100000)).Value);
        Assert.DoesNotContain((await staff.PayableOrdersAsync()).Value!, o => o.OrderId == order.OrderId);

        var summary = (await staff.ShiftSummaryAsync(shift.ShiftId)).Value!;
        Assert.Equal(120000m, summary.ExpectedCash);
        var closed = (await staff.CloseShiftAsync(shift.ShiftId, 121000, "Dư 1.000")).Value!;
        Assert.Equal((121000m, 1000m), (closed.CountedCash, closed.Difference));
        Assert.Equal(409, (await staff.CloseShiftAsync(shift.ShiftId, 121000, null)).StatusCode);
        Assert.Null((await staff.CurrentShiftAsync()).Value!.Shift);
    }

    [SqlServerFact]
    public async Task A3_barista_works_the_queue_through_the_desktop_client()
    {
        var d = await SeedAsync(Factory.Services);
        var (cashierApi, cashier) = await LoginAsync(d.Cashier);
        var (baristaApi, barista) = await LoginAsync(await SeedStaffAsync(Factory.Services, d.Suffix, "phache", Permissions.PreparationWork));
        using var _1 = cashierApi; using var _2 = baristaApi;
        var order = await PlaceOrderAsync(d, quantity: 1);
        Assert.True((await cashier.AcceptAsync(order.OrderId)).Ok);

        var unit = (await barista.PreparationQueueAsync()).Value!.Single(u => u.OrderId == order.OrderId);
        Assert.Equal(PreparationStatuses.Waiting, unit.Status);
        var early = await barista.MarkReadyAsync(unit.UnitId);   // chưa bắt đầu thì chưa báo xong được
        Assert.Equal((false, 409, ApiErrorCodes.InvalidState), (early.Ok, early.StatusCode, early.Error!.Code));
        Assert.True((await barista.StartUnitAsync(unit.UnitId)).Ok);
        Assert.Equal(409, (await barista.StartUnitAsync(unit.UnitId)).StatusCode);
        Assert.True((await barista.MarkReadyAsync(unit.UnitId)).Ok);
        Assert.True((await barista.DeliverAsync(unit.UnitId)).Ok);
        Assert.DoesNotContain((await barista.PreparationQueueAsync()).Value!, u => u.OrderId == order.OrderId);
        Assert.Equal(404, (await barista.StartUnitAsync(999999999)).StatusCode);
        Assert.Equal(403, (await cashier.PreparationQueueAsync()).StatusCode);   // thu ngân không có quyền pha chế
    }
}
