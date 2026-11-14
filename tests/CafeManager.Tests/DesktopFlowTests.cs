using System.Net;
using System.Net.Http.Json;
using CafeManager.Client;
using CafeManager.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Kiểm thử luồng của Desktop (D02, D06) bằng ApiClient + StaffApi THẬT gọi máy chủ thật trong bộ nhớ và SQL Server thật.
// Chưa phải kiểm thử giao diện Windows Forms; đây là bằng chứng cho lớp dữ liệu và thông báo mà các màn hình D02/D06 dùng.
public sealed class DesktopFlowTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private ApiFactory? factory;
    private ApiFactory Factory => factory ??= new ApiFactory(fixture.Connection);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { if (factory != null) await factory.DisposeAsync(); }

    // Giữ cookie giữa các lời gọi như HttpClientHandler của Desktop (handler của máy chủ thử không tự giữ cookie).
    private sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly System.Net.CookieContainer jar = new();
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

    private ApiClient NewApi() =>
        new(new ApiClientOptions(new Uri("http://localhost/"), TimeSpan.FromSeconds(15)), new CookieHandler(Factory.Server.CreateHandler()));

    private async Task<(HttpClient Client, string Token)> CustomerAsync(string userName)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "secret1"))).StatusCode);
        return (client, (await client.GetFromJsonAsync<AntiforgeryTokenResponse>("api/auth/antiforgery"))!.Token);
    }

    private static async Task<HttpResponseMessage> CustomerSend(HttpClient client, string token, string path, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body != null) request.Content = JsonContent.Create(body);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    private static async Task<CreateOrderResponse> Place(HttpClient client, string token, CreateOrderRequest order)
    {
        var response = await CustomerSend(client, token, "api/orders", order);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!;
    }

    [SqlServerFact]
    public async Task D02_cashier_lists_accepts_rejects_and_sees_cancelled_orders_through_the_real_client()
    {
        var d = await SeedAsync(Factory.Services);
        var (customer, token) = await CustomerAsync(d.Customer);
        var a = await Place(customer, token, Order(d, quantity: 2));
        var b = await Place(customer, token, Order(d, quantity: 1));
        var c = await Place(customer, token, Order(d, quantity: 1));

        using var api = NewApi();
        var staff = new StaffApi(api);
        var login = await api.LoginAsync(d.Cashier, "secret1");
        Assert.True(login.Ok, login.Message);
        Assert.Contains(Permissions.OrderReceive, api.CurrentUser!.Permissions);
        Assert.False(string.IsNullOrEmpty(api.AntiforgeryToken)); // thao tác ghi bên dưới cần token này

        var pending = await staff.PendingOrdersAsync();
        Assert.True(pending.Ok, pending.Message);
        var mine = pending.Value!.Where(o => new[] { a.OrderId, b.OrderId, c.OrderId }.Contains(o.Detail.Summary.OrderId)).ToList();
        Assert.Equal(3, mine.Count);
        var detailA = mine.Single(o => o.Detail.Summary.OrderId == a.OrderId).Detail;
        Assert.Equal("Pending", detailA.Summary.Status);
        Assert.Equal("Khách nhận", detailA.ContactName);
        Assert.Equal(a.Total, Display.LineTotal(detailA.Lines.Single())); // công thức hiển thị khớp tổng máy chủ tính
        Assert.Equal((30000 + 5000) * 2, a.Total);

        // Tiếp nhận: có phiếu; gọi lại trả đúng phiếu cũ, không tạo phiếu thứ hai.
        var accepted = await staff.AcceptAsync(a.OrderId);
        Assert.True(accepted.Ok, accepted.Message); Assert.True(accepted.Value!.TicketId > 0);
        var replay = await staff.AcceptAsync(a.OrderId);
        Assert.True(replay.Ok); Assert.Equal(accepted.Value.TicketId, replay.Value!.TicketId);
        Assert.DoesNotContain((await staff.PendingOrdersAsync()).Value!, o => o.Detail.Summary.OrderId == a.OrderId);

        // Từ chối: bắt buộc lý do (máy chủ cũng kiểm tra); lần hai báo trạng thái đã đổi.
        var noReason = await staff.RejectAsync(b.OrderId, "  ");
        Assert.False(noReason.Ok); Assert.Equal(400, noReason.StatusCode); Assert.Equal(ApiErrorCodes.Validation, noReason.Error!.Code);
        Assert.True((await staff.RejectAsync(b.OrderId, "Hết nguyên liệu")).Ok);
        var again = await staff.RejectAsync(b.OrderId, "Hết nguyên liệu");
        Assert.False(again.Ok); Assert.Equal(409, again.StatusCode); Assert.Equal(ApiErrorCodes.InvalidState, again.Error!.Code);
        Assert.Contains("tải lại", StaffMessages.ForReject(again));
        Assert.True(StaffMessages.ReloadAfter(again));
        Assert.DoesNotContain((await staff.PendingOrdersAsync()).Value!, o => o.Detail.Summary.OrderId == b.OrderId);

        // Khách hủy trước khi thu ngân bấm tiếp nhận: 409, thông báo nói rõ khách đã hủy.
        Assert.Equal(HttpStatusCode.NoContent, (await CustomerSend(customer, token, $"api/orders/{c.OrderId}/cancel")).StatusCode);
        var late = await staff.AcceptAsync(c.OrderId);
        Assert.False(late.Ok); Assert.Equal(409, late.StatusCode); Assert.Equal(ApiErrorCodes.InvalidState, late.Error!.Code);
        Assert.Contains("Khách đã hủy đơn này.", StaffMessages.ForAccept(late));

        // Kết quả người khách thấy khớp: A đang phục vụ, B bị từ chối kèm lý do, C đã hủy.
        var customerView = (await customer.GetFromJsonAsync<List<OrderSummaryDto>>("api/orders/mine"))!.ToDictionary(o => o.OrderId);
        Assert.Equal("Serving", customerView[a.OrderId].Status);
        Assert.Equal("Rejected", customerView[b.OrderId].Status);
        Assert.Equal("Cancelled", customerView[c.OrderId].Status);
        Assert.Equal("Hết nguyên liệu", (await customer.GetFromJsonAsync<OrderDetailDto>($"api/orders/{b.OrderId}"))!.RejectReason);
    }

    [SqlServerFact]
    public async Task D02_insufficient_stock_keeps_the_order_pending_with_a_clear_message()
    {
        var d = await SeedAsync(Factory.Services, stock: 100);
        var (customer, token) = await CustomerAsync(d.Customer);
        var order = await Place(customer, token, Order(d, quantity: 10)); // cần (20 + 10) × 10 = 300 > 100

        using var api = NewApi();
        var staff = new StaffApi(api);
        Assert.True((await api.LoginAsync(d.Cashier, "secret1")).Ok);
        var result = await staff.AcceptAsync(order.OrderId);
        Assert.False(result.Ok); Assert.Equal(409, result.StatusCode); Assert.Equal(ApiErrorCodes.InsufficientStock, result.Error!.Code);
        Assert.Contains("Không đủ nguyên liệu", StaffMessages.ForAccept(result));
        var list = (await staff.PendingOrdersAsync()).Value!;
        Assert.Equal("Pending", list.Single(o => o.Detail.Summary.OrderId == order.OrderId).Detail.Summary.Status); // không ghi nửa chừng
    }

    [SqlServerFact]
    public async Task D06_manager_builds_a_sellable_item_step_by_step_and_it_reaches_the_public_menu()
    {
        var d = await SeedAsync(Factory.Services);
        var manager = await SeedManagerAsync(Factory.Services, d.Suffix);
        using var api = NewApi();
        var staff = new StaffApi(api);
        Assert.True((await api.LoginAsync(manager, "secret1")).Ok);

        var menu = (await staff.MenuAsync()).Value!;
        var category = menu.Categories.Single(c => c.CategoryName == "DM " + d.Suffix);
        Assert.Contains(menu.Ingredients, i => i.IngredientId == d.Ingredient);

        // Món + biến thể mới: chưa bán được.
        var name = "Trà thử " + d.Suffix;
        var product = await staff.SaveProductAsync(null, new ProductUpsertRequest(name, category.CategoryId, true));
        Assert.True(product.Ok, product.Message);
        var variant = await staff.SaveVariantAsync(null, new VariantUpsertRequest(product.Value!.Id, "Vừa", "Drink", false));
        Assert.True(variant.Ok, variant.Message);
        var variantId = variant.Value!.Id;

        async Task<MenuRow> Row() => MenuRows.Flatten((await staff.MenuAsync()).Value!).Single(r => r.VariantId == variantId);
        Assert.Equal("Chưa bán: thiếu giá và công thức", (await Row()).Status);

        var early = await staff.SaveVariantAsync(variantId, new VariantUpsertRequest(product.Value.Id, "Vừa", "Drink", true));
        Assert.False(early.Ok); Assert.Equal(400, early.StatusCode); // KT: chưa có giá và công thức thì không bật bán

        // Giá: âm bị từ chối (KT05), hợp lệ thì nhận; thiếu công thức vẫn chưa bán được.
        var negative = await staff.SetPriceAsync(variantId, -1, DateTime.UtcNow);
        Assert.False(negative.Ok); Assert.Equal(400, negative.StatusCode);
        Assert.True((await staff.SetPriceAsync(variantId, 35000, DateTime.UtcNow)).Ok);
        Assert.Equal("Chưa bán: thiếu công thức", (await Row()).Status);

        // Công thức: rỗng và trùng nguyên liệu bị từ chối.
        Assert.Equal(400, (await staff.SetRecipeAsync(variantId, DateTime.UtcNow, [])).StatusCode);
        Assert.Equal(400, (await staff.SetRecipeAsync(variantId, DateTime.UtcNow, [new(d.Ingredient, 10), new(d.Ingredient, 5)])).StatusCode);
        Assert.True((await staff.SetRecipeAsync(variantId, DateTime.UtcNow, [new(d.Ingredient, 15)])).Ok);
        var ready = await Row();
        Assert.True(ready.CanActivate); Assert.Equal(35000m, ready.Price); Assert.Equal("NL " + d.Suffix + " 15 g", ready.RecipeSummary);

        // Bật bán: món xuất hiện trên thực đơn công khai (Web) với giá đã đặt.
        Assert.True((await staff.SaveVariantAsync(variantId, new VariantUpsertRequest(product.Value.Id, "Vừa", "Drink", true))).Ok);
        Assert.Equal("Đang bán", (await Row()).Status);
        var publicMenu = (await api.GetAsync<List<MenuProductDto>>("api/menu")).Value!;
        Assert.Equal(35000, publicMenu.Single(p => p.ProductName == name).Variants.Single().UnitPrice);

        // Đổi giá theo ngày hiệu lực: phiên bản trùng ngày bị chặn (KT18), quá khứ bị từ chối; giá tương lai chưa đổi giá hiện hành.
        var future = DateTime.UtcNow.AddMinutes(30);
        Assert.True((await staff.SetPriceAsync(variantId, 40000, future)).Ok);
        var overlap = await staff.SetPriceAsync(variantId, 45000, future);
        Assert.False(overlap.Ok); Assert.Equal(409, overlap.StatusCode); Assert.Equal(ApiErrorCodes.OverlappingInterval, overlap.Error!.Code);
        Assert.Contains("sau phiên bản hiện tại", StaffMessages.ForMenu(overlap, ""));
        var past = await staff.SetPriceAsync(variantId, 45000, DateTime.UtcNow.AddHours(-1));
        Assert.False(past.Ok); Assert.Equal(400, past.StatusCode);
        Assert.Equal(35000m, (await Row()).Price);

        // Ngừng bán: món biến mất khỏi thực đơn khách nhưng vẫn còn trong quản lý và bật lại được.
        Assert.True((await staff.DeactivateAsync(variantId)).Ok);
        Assert.DoesNotContain((await api.GetAsync<List<MenuProductDto>>("api/menu")).Value!, p => p.ProductName == name);
        var stopped = await Row();
        Assert.Equal("Chưa bán: sẵn sàng bật bán", stopped.Status); Assert.True(stopped.CanActivate);
    }

    [SqlServerFact]
    public async Task D06_is_forbidden_without_the_menu_permission_and_expired_sessions_raise_the_event()
    {
        var d = await SeedAsync(Factory.Services);
        using var api = NewApi();
        var staff = new StaffApi(api);
        var expired = 0; api.SessionExpired += () => expired++;
        Assert.True((await api.LoginAsync(d.Cashier, "secret1")).Ok);

        var denied = await staff.MenuAsync();
        Assert.False(denied.Ok); Assert.Equal(403, denied.StatusCode);
        Assert.Equal("Tài khoản không có quyền xem thực đơn quản lý.", StaffMessages.ForLoad(denied, "thực đơn quản lý"));
        Assert.Equal(0, expired); // 403 không phải hết phiên

        await api.LogoutAsync();
        var after = await staff.PendingOrdersAsync();
        Assert.False(after.Ok); Assert.Equal(401, after.StatusCode);
        Assert.Equal(1, expired); // D02/D06 phải đưa người dùng về màn hình đăng nhập
    }
}
