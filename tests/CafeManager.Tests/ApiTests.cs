using System.Net;
using System.Net.Http.Json;
using CafeManager.Core;
using CafeManager.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Kiểm thử API qua HTTP thật (máy chủ trong bộ nhớ + SQL Server thật): đăng nhập, cookie, phân quyền, antiforgery,
// đặt đơn, tiếp nhận, hủy, thu hồi quyền và quản lý thực đơn.
public sealed class ApiFactory(string connection) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CafeDatabase", connection);
    }
}

public sealed class ApiTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private ApiFactory? factory;
    private ApiFactory Factory => factory ??= new ApiFactory(fixture.Connection);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { if (factory != null) await factory.DisposeAsync(); }

    private HttpClient NewClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<ApiError> ErrorOf(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<ApiError>())!;

    // Đăng nhập rồi lấy token chống giả mạo (token gắn với danh tính sau đăng nhập), như Desktop sẽ làm.
    private static async Task<(LoginResponse User, string Token)> LoginAsync(HttpClient client, string userName, string password = "secret1")
    {
        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(userName, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        var token = (await client.GetFromJsonAsync<AntiforgeryTokenResponse>("api/auth/antiforgery"))!.Token;
        return (user, token);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        if (token != null) request.Headers.Add("X-CSRF-TOKEN", token);
        return client.SendAsync(request);
    }

    [SqlServerFact]
    public async Task Anonymous_calls_get_401_json_and_the_public_menu_is_open()
    {
        var d = await SeedAsync(Factory.Services);
        var client = NewClient();
        foreach (var path in new[] { "api/orders/mine", "api/staff/orders", "api/staff/menu", "api/auth/me" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(ApiErrorCodes.Unauthorized, (await ErrorOf(response)).Code);
        }
        var menu = await client.GetFromJsonAsync<List<MenuProductDto>>("api/menu");
        Assert.Contains(menu!, p => p.ProductName == "Cà phê " + d.Suffix);
        Assert.DoesNotContain(await client.GetFromJsonAsync<List<MenuProductDto>>("api/menu?q=khongco" + d.Suffix) ?? [], _ => true);
    }

    [SqlServerFact]
    public async Task Login_errors_lockout_and_logout()
    {
        var d = await SeedAsync(Factory.Services);
        var client = NewClient();
        var wrong = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(d.Customer, "sai"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode); Assert.Equal(ApiErrorCodes.Unauthorized, (await ErrorOf(wrong)).Code);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/auth/me")).StatusCode); // đăng nhập sai không cấp phiên
        var (user, token) = await LoginAsync(client, d.Cashier);
        Assert.Equal(UserKinds.Staff, user.UserKind); Assert.Contains(Permissions.OrderReceive, user.Permissions);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, "api/auth/logout", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/auth/me")).StatusCode);
        var other = NewClient();
        for (var i = 0; i < 4; i++) await other.PostAsJsonAsync("api/auth/login", new LoginRequest(d.Customer2, "sai"));
        var locked = await other.PostAsJsonAsync("api/auth/login", new LoginRequest(d.Customer2, "sai"));
        Assert.Equal(ApiErrorCodes.AccountLocked, (await ErrorOf(locked)).Code); Assert.Equal(HttpStatusCode.Forbidden, locked.StatusCode);
        Assert.Equal(ApiErrorCodes.AccountLocked, (await ErrorOf(await other.PostAsJsonAsync("api/auth/login", new LoginRequest(d.Customer2, "secret1")))).Code);
    }

    [SqlServerFact]
    public async Task Permissions_are_enforced_on_the_server_and_writes_need_an_antiforgery_token()
    {
        var d = await SeedAsync(Factory.Services);
        var customer = NewClient();
        var (_, token) = await LoginAsync(customer, d.Customer);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("api/orders/mine")).StatusCode);
        var denied = await customer.GetAsync("api/staff/orders");   // khách gọi API nhân viên: KT03
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); Assert.Equal(ApiErrorCodes.Forbidden, (await ErrorOf(denied)).Code);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(customer, HttpMethod.Post, "api/staff/variants/1/deactivate", token)).StatusCode);
        var body = Order(d);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(customer, HttpMethod.Post, "api/orders", null, body)).StatusCode);          // thiếu token
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(customer, HttpMethod.Post, "api/orders", "token-sai", body)).StatusCode);   // sai token
        Assert.Equal(HttpStatusCode.Created, (await Send(customer, HttpMethod.Post, "api/orders", token, body)).StatusCode);
        var staff = NewClient();
        await LoginAsync(staff, d.Cashier);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("api/orders/mine")).StatusCode);   // nhân viên không dùng API khách
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("api/staff/menu")).StatusCode);    // thu ngân không có MENU_MANAGE
    }

    [SqlServerFact]
    public async Task Order_flow_create_price_change_replay_accept_and_cancel_rules()
    {
        var d = await SeedAsync(Factory.Services);
        var customer = NewClient();
        var (_, token) = await LoginAsync(customer, d.Customer);
        var changed = await Send(customer, HttpMethod.Post, "api/orders", token, Order(d, expected: 28000));
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        var error = await ErrorOf(changed);
        Assert.Equal(ApiErrorCodes.PriceChanged, error.Code); Assert.Equal(30000, error.PriceChanges![0].NewPrice);
        var request = Order(d, id: Guid.NewGuid());
        var created = await Send(customer, HttpMethod.Post, "api/orders", token, request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<CreateOrderResponse>())!;
        Assert.Equal(70000, order.Total); Assert.Equal("Pending", order.Status);
        var replay = (await (await Send(customer, HttpMethod.Post, "api/orders", token, request)).Content.ReadFromJsonAsync<CreateOrderResponse>())!;
        Assert.Equal(order.OrderId, replay.OrderId);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(customer, HttpMethod.Post, "api/orders", token, request with { Lines = Order(d, quantity: 3).Lines })).StatusCode);

        var other = NewClient();
        await LoginAsync(other, d.Customer2);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"api/orders/{order.OrderId}")).StatusCode);   // KT16
        var otherToken = (await other.GetFromJsonAsync<AntiforgeryTokenResponse>("api/auth/antiforgery"))!.Token;
        Assert.Equal(HttpStatusCode.NotFound, (await Send(other, HttpMethod.Post, $"api/orders/{order.OrderId}/cancel", otherToken)).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<List<OrderSummaryDto>>("api/orders/mine"))!);

        var staff = NewClient();
        var (_, staffToken) = await LoginAsync(staff, d.Cashier);
        var pending = (await staff.GetFromJsonAsync<List<StaffOrderDto>>("api/staff/orders?status=Pending"))!;
        Assert.Contains(pending, o => o.Detail.Summary.OrderId == order.OrderId && o.Detail.Lines.Count == 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.GetAsync("api/staff/orders?status=Bậy")).StatusCode);
        var accepted = await Send(staff, HttpMethod.Post, $"api/staff/orders/{order.OrderId}/accept", staffToken);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.True((await accepted.Content.ReadFromJsonAsync<AcceptOrderResponse>())!.TicketId > 0);
        var detail = (await customer.GetFromJsonAsync<OrderDetailDto>($"api/orders/{order.OrderId}"))!;
        Assert.Equal("Serving", detail.Summary.Status);
        var cancel = await Send(customer, HttpMethod.Post, $"api/orders/{order.OrderId}/cancel", token);   // đã tiếp nhận: không tự hủy được (KT14)
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode); Assert.Equal(ApiErrorCodes.InvalidState, (await ErrorOf(cancel)).Code);
        var reject = await Send(staff, HttpMethod.Post, $"api/staff/orders/{order.OrderId}/reject", staffToken, new RejectOrderRequest("muộn"));
        Assert.Equal(HttpStatusCode.Conflict, reject.StatusCode);

        var second = (await (await Send(customer, HttpMethod.Post, "api/orders", token, Order(d))).Content.ReadFromJsonAsync<CreateOrderResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await Send(customer, HttpMethod.Post, $"api/orders/{second.OrderId}/cancel", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, HttpMethod.Post, $"api/staff/orders/{second.OrderId}/accept", staffToken)).StatusCode);   // khách đã hủy
        var third = (await (await Send(customer, HttpMethod.Post, "api/orders", token, Order(d))).Content.ReadFromJsonAsync<CreateOrderResponse>())!;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, HttpMethod.Post, $"api/staff/orders/{third.OrderId}/reject", staffToken, new RejectOrderRequest(" "))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(staff, HttpMethod.Post, $"api/staff/orders/{third.OrderId}/reject", staffToken, new RejectOrderRequest("Hết hàng"))).StatusCode);
        Assert.Equal("Hết hàng", (await customer.GetFromJsonAsync<OrderDetailDto>($"api/orders/{third.OrderId}"))!.RejectReason);
    }

    [SqlServerFact]
    public async Task Revoked_staff_loses_the_session_on_the_next_request()
    {
        var d = await SeedAsync(Factory.Services);
        var staff = NewClient();
        await LoginAsync(staff, d.Cashier);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("api/staff/orders")).StatusCode);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
            var account = await (from s in db.StaffAccount join u in db.Users on s.IdentityUserId equals u.Id where u.UserName == d.Cashier select s).SingleAsync();
            account.IsActive = false; await db.SaveChangesAsync();
        }
        var after = await staff.GetAsync("api/staff/orders");   // TC08: yêu cầu kế tiếp bị từ chối
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode); Assert.Equal(ApiErrorCodes.Unauthorized, (await ErrorOf(after)).Code);
        var relogin = await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(d.Cashier, "secret1"));
        Assert.Equal(ApiErrorCodes.AccountLocked, (await ErrorOf(relogin)).Code);
    }

    [SqlServerFact]
    public async Task Register_customer_over_http_then_login()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var client = NewClient();
        var body = new RegisterCustomerRequest("web." + s, "secret1", "Khách web", "0901234567");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("api/auth/register-customer", body)).StatusCode);
        var duplicate = await client.PostAsJsonAsync("api/auth/register-customer", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/auth/register-customer", body with { UserName = "x", Password = "1" })).StatusCode);
        var (user, _) = await LoginAsync(client, "web." + s);
        Assert.Equal(UserKinds.Customer, user.UserKind); Assert.Equal("Khách web", user.DisplayName);
    }

    [SqlServerFact]
    public async Task Menu_management_requires_price_and_recipe_before_selling_and_rejects_overlaps()
    {
        var d = await SeedAsync(Factory.Services);
        var manager = await SeedManagerAsync(Factory.Services, d.Suffix);
        var client = NewClient();
        var (user, token) = await LoginAsync(client, manager);
        Assert.Contains(Permissions.MenuManage, user.Permissions);
        var menu = (await client.GetFromJsonAsync<StaffMenuResponse>("api/staff/menu"))!;
        var category = menu.Categories.First(c => c.CategoryName == "DM " + d.Suffix);
        var ingredient = menu.Ingredients.First(i => i.IngredientId == d.Ingredient);

        var product = await (await Send(client, HttpMethod.Post, "api/staff/products", token, new ProductUpsertRequest("Trà mới " + d.Suffix, category.CategoryId, true))).Content.ReadFromJsonAsync<IdResponse>();
        var variantResponse = await Send(client, HttpMethod.Post, "api/staff/variants", token, new VariantUpsertRequest(product!.Id, "Vừa", "Drink", false));
        Assert.Equal(HttpStatusCode.Created, variantResponse.StatusCode);
        var variant = (await variantResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Post, "api/staff/variants", token, new VariantUpsertRequest(product.Id, "Lớn", "Drink", true))).StatusCode);   // mới tạo chưa bán được
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, $"api/staff/variants/{variant}", token, new VariantUpsertRequest(product.Id, "Vừa", "Drink", true))).StatusCode); // chưa có giá/công thức
        Assert.DoesNotContain((await client.GetFromJsonAsync<List<MenuProductDto>>("api/menu"))!, p => p.ProductName == "Trà mới " + d.Suffix);

        var now = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Post, $"api/staff/variants/{variant}/price", token, new SetPriceRequest(-1, now))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, $"api/staff/variants/{variant}/price", token, new SetPriceRequest(40000, now))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, HttpMethod.Post, $"api/staff/variants/{variant}/price", token, new SetPriceRequest(41000, now))).StatusCode);   // trùng ngày hiệu lực
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, $"api/staff/variants/{variant}/recipe", token, new SetRecipeRequest(now, []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, HttpMethod.Put, $"api/staff/variants/{variant}/recipe", token, new SetRecipeRequest(now, [new(ingredient.IngredientId, 0)]))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Put, $"api/staff/variants/{variant}/recipe", token, new SetRecipeRequest(now, [new(ingredient.IngredientId, 15)]))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Put, $"api/staff/variants/{variant}", token, new VariantUpsertRequest(product.Id, "Vừa", "Drink", true))).StatusCode);
        var listed = (await client.GetFromJsonAsync<List<MenuProductDto>>("api/menu"))!.Single(p => p.ProductName == "Trà mới " + d.Suffix);
        Assert.Equal(40000, listed.Variants.Single().UnitPrice);

        // Đổi giá bằng phiên bản mới: giá cũ được đóng khoảng hiệu lực, đơn cũ giữ giá đã chốt.
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, $"api/staff/variants/{variant}/price", token, new SetPriceRequest(45000, now.AddHours(1)))).StatusCode);
        Assert.Equal(40000, (await client.GetFromJsonAsync<List<MenuProductDto>>("api/menu"))!.Single(p => p.ProductName == "Trà mới " + d.Suffix).Variants.Single().UnitPrice);
        var staffMenu = (await client.GetFromJsonAsync<StaffMenuResponse>("api/staff/menu"))!;
        var staffVariant = staffMenu.Products.Single(p => p.ProductId == product.Id).Variants.Single();
        Assert.Equal(40000, staffVariant.CurrentPrice); Assert.Equal(15, staffVariant.CurrentRecipe.Single().Quantity);

        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, HttpMethod.Post, $"api/staff/variants/{variant}/deactivate", token)).StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<List<MenuProductDto>>("api/menu"))!, p => p.ProductName == "Trà mới " + d.Suffix);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, HttpMethod.Post, "api/staff/variants/999999999/deactivate", token)).StatusCode);
    }
}
