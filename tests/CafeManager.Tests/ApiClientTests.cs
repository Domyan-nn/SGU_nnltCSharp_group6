using System.Net;
using System.Text;
using System.Text.Json;
using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Tests;

// Kiểm thử máy khách API của Desktop bằng handler giả; không cần SQL Server hay máy chủ thật.
public sealed class ApiClientTests
{
    private static readonly Uri Base = new("https://api.test/");

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add((request, request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return await send(request, ct);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, object body) =>
        new(code) { Content = new StringContent(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json") };

    private static ApiClient Client(FakeHandler handler, TimeSpan? timeout = null) =>
        new(new ApiClientOptions(Base, timeout ?? TimeSpan.FromSeconds(5)), handler);

    [Fact]
    public async Task Login_succeeds_for_staff_and_sends_camel_case_json()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, new LoginResponse(UserKinds.Staff, "Thu ngân thử", [Permissions.OrderReceive]))));
        using var api = Client(handler);
        var result = await api.LoginAsync("dev.cashier", "secret");
        Assert.True(result.Ok);
        Assert.Equal("Thu ngân thử", api.CurrentUser!.DisplayName);
        Assert.Contains(Permissions.OrderReceive, api.CurrentUser.Permissions);
        Assert.Equal("https://api.test/api/auth/login", handler.Calls[0].Request.RequestUri!.ToString());
        Assert.Contains("\"userName\":\"dev.cashier\"", handler.Calls[0].Body);
        Assert.Contains("\"password\":\"secret\"", handler.Calls[0].Body);
    }

    [Fact]
    public async Task Staff_login_fetches_the_antiforgery_token_and_sends_it_on_later_writes()
    {
        var handler = new FakeHandler((req, _) => Task.FromResult(req.RequestUri!.AbsolutePath.EndsWith("antiforgery")
            ? Json(HttpStatusCode.OK, new AntiforgeryTokenResponse("tok-123"))
            : req.RequestUri.AbsolutePath.EndsWith("login")
                ? Json(HttpStatusCode.OK, new LoginResponse(UserKinds.Staff, "Thu ngân", [Permissions.OrderReceive]))
                : new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var api = Client(handler);
        Assert.True((await api.LoginAsync("dev.cashier", "x")).Ok);
        Assert.Equal("tok-123", api.AntiforgeryToken);
        await api.PostAsync("api/staff/orders/1/accept", null);
        Assert.Equal("tok-123", handler.Calls[^1].Request.Headers.GetValues("X-CSRF-TOKEN").Single());
        await api.LogoutAsync();
        Assert.Null(api.AntiforgeryToken);
    }

    [Fact]
    public async Task Login_with_customer_account_is_rejected_and_logged_out()
    {
        var handler = new FakeHandler((req, _) => Task.FromResult(req.RequestUri!.AbsolutePath.EndsWith("login")
            ? Json(HttpStatusCode.OK, new LoginResponse(UserKinds.Customer, "Khách thử", []))
            : new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var api = Client(handler);
        var result = await api.LoginAsync("dev.customer", "x");
        Assert.False(result.Ok); Assert.Equal(ApiErrorCodes.Forbidden, result.Error!.Code); Assert.Null(api.CurrentUser);
        Assert.EndsWith("api/auth/logout", handler.Calls[^1].Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Wrong_password_is_401_without_session_expired_event_and_not_unknown()
    {
        using var api = Client(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Unauthorized, new ApiError(ApiErrorCodes.Unauthorized, "Sai thông tin")))));
        var expired = 0; api.SessionExpired += () => expired++;
        var result = await api.LoginAsync("a", "b");
        Assert.False(result.Ok); Assert.False(result.Unknown); Assert.Equal(401, result.StatusCode);
        Assert.Equal(0, expired); Assert.Null(api.CurrentUser);
    }

    [Fact]
    public async Task Other_calls_with_401_raise_session_expired()
    {
        using var api = Client(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Unauthorized, new ApiError(ApiErrorCodes.Unauthorized, "Hết phiên")))));
        var expired = 0; api.SessionExpired += () => expired++;
        var result = await api.GetAsync<object>("api/staff/orders?status=Pending");
        Assert.False(result.Ok); Assert.Equal(1, expired);
    }

    [Fact]
    public async Task Conflict_with_price_changes_is_parsed()
    {
        var error = new ApiError(ApiErrorCodes.PriceChanged, "Giá đã đổi", [new PriceChange(21, 30000, 35000)]);
        using var api = Client(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Conflict, error))));
        var result = await api.PostAsync<CreateOrderResponse>("api/orders", new { });
        Assert.False(result.Ok); Assert.False(result.Unknown); Assert.Equal(409, result.StatusCode);
        Assert.Equal(ApiErrorCodes.PriceChanged, result.Error!.Code);
        Assert.Equal(35000, result.Error.PriceChanges![0].NewPrice);
    }

    [Theory]
    [InlineData(400, ApiErrorCodes.Validation)]
    [InlineData(403, ApiErrorCodes.Forbidden)]
    [InlineData(404, ApiErrorCodes.NotFound)]
    [InlineData(409, ApiErrorCodes.Conflict)]
    public async Task Html_error_pages_fall_back_to_status_code_mapping(int status, string code)
    {
        using var api = Client(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            { Content = new StringContent("<html>login</html>", Encoding.UTF8, "text/html") })));
        var result = await api.GetAsync<object>("api/x");
        Assert.False(result.Ok); Assert.Equal(code, result.Error!.Code); Assert.False(result.Unknown);
    }

    [Fact]
    public async Task Server_error_is_unknown_not_a_definite_failure()
    {
        using var api = Client(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        var result = await api.PostAsync("api/staff/orders/1/accept", null);
        Assert.False(result.Ok); Assert.True(result.Unknown); Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task Network_failure_is_unknown()
    {
        using var api = Client(new FakeHandler((_, _) => throw new HttpRequestException("down")));
        var result = await api.GetAsync<object>("api/x");
        Assert.False(result.Ok); Assert.True(result.Unknown);
        Assert.Contains("Chưa xác định", result.Message);
    }

    [Fact]
    public async Task Timeout_is_unknown_but_caller_cancellation_is_propagated()
    {
        var handler = new FakeHandler(async (_, ct) => { await Task.Delay(TimeSpan.FromSeconds(10), ct); return new HttpResponseMessage(HttpStatusCode.OK); });
        using var api = Client(handler, TimeSpan.FromMilliseconds(100));
        var result = await api.GetAsync<object>("api/slow");
        Assert.True(result.Unknown); Assert.False(result.Ok);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => api.GetAsync<object>("api/slow", cts.Token));
    }

    [Fact]
    public async Task Antiforgery_header_is_sent_only_when_a_token_is_set()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var api = Client(handler);
        await api.PostAsync("api/x", null);
        Assert.False(handler.Calls[0].Request.Headers.Contains("X-CSRF-TOKEN"));
        api.AntiforgeryToken = "tok";
        await api.PostAsync("api/x", null);
        Assert.Equal("tok", handler.Calls[1].Request.Headers.GetValues("X-CSRF-TOKEN").Single());
    }

    [Fact]
    public void Each_client_has_its_own_cookie_container()
    {
        using var a = new ApiClient(new ApiClientOptions(Base, TimeSpan.FromSeconds(5)));
        using var b = new ApiClient(new ApiClientOptions(Base, TimeSpan.FromSeconds(5)));
        Assert.NotNull(a.Cookies); Assert.NotNull(b.Cookies);
        Assert.NotSame(a.Cookies, b.Cookies);
    }
}
