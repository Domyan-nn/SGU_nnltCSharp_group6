using System.Net;
using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Tests;

// Khóa đường dẫn và phương thức HTTP của hợp đồng A1–A3 phía Desktop.
// Máy chủ hiện thực sau phải khớp đúng các đường dẫn này; đổi hợp đồng thì test này đỏ trước.
public sealed class StaffApiContractTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Method, string Path, string Body, string? Csrf)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
            var csrf = request.Headers.TryGetValues("X-CSRF-TOKEN", out var values) ? values.Single() : null;
            Calls.Add((request.Method.Method, request.RequestUri!.PathAndQuery, body, csrf));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        public string[] Routes() => Calls.Select(c => c.Method + " " + c.Path).ToArray();
    }

    private static ApiClient NewApi(RecordingHandler handler) =>
        new(new ApiClientOptions(new Uri("https://api.test/"), TimeSpan.FromSeconds(5)), handler) { AntiforgeryToken = "tok-1" };

    [Fact]
    public async Task Category_and_ingredient_calls_use_the_agreed_routes()
    {
        var handler = new RecordingHandler();
        using var api = NewApi(handler);
        var staff = new StaffApi(api);
        await staff.CategoriesAsync();
        await staff.CategoriesAsync(" cà phê ");
        await staff.SaveCategoryAsync(null, new CategoryUpsertRequest("Trà", true));
        await staff.SaveCategoryAsync(5, new CategoryUpsertRequest("Trà", false));
        await staff.DeleteCategoryAsync(5);
        await staff.IngredientsAsync();
        await staff.SaveIngredientAsync(null, new IngredientUpsertRequest("Đường", "g", true));
        await staff.SaveIngredientAsync(7, new IngredientUpsertRequest("Đường", "g", true));
        await staff.DeleteIngredientAsync(7);

        Assert.Equal(new[]
        {
            "GET /api/staff/categories",
            "GET /api/staff/categories?q=c%C3%A0%20ph%C3%AA",
            "POST /api/staff/categories",
            "PUT /api/staff/categories/5",
            "DELETE /api/staff/categories/5",
            "GET /api/staff/ingredients",
            "POST /api/staff/ingredients",
            "PUT /api/staff/ingredients/7",
            "DELETE /api/staff/ingredients/7",
        }, handler.Routes());
        Assert.Equal("tok-1", handler.Calls[4].Csrf);
        Assert.Contains("\"categoryName\":", handler.Calls[2].Body);
        Assert.Contains("\"isActive\":true", handler.Calls[2].Body);
        Assert.Contains("\"unitName\":\"g\"", handler.Calls[6].Body);
    }

    [Fact]
    public async Task Shift_and_cash_payment_calls_use_the_agreed_routes()
    {
        var handler = new RecordingHandler();
        using var api = NewApi(handler);
        var staff = new StaffApi(api);
        var requestId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        await staff.CountersAsync();
        await staff.CurrentShiftAsync();
        await staff.OpenShiftAsync(1, 500000m);
        await staff.ShiftSummaryAsync(9);
        await staff.CloseShiftAsync(9, 845000m, null);
        await staff.PayableOrdersAsync();
        await staff.PayCashAsync(requestId, 42, 100000m);

        Assert.Equal(new[]
        {
            "GET /api/staff/counters",
            "GET /api/staff/shifts/current",
            "POST /api/staff/shifts",
            "GET /api/staff/shifts/9/summary",
            "POST /api/staff/shifts/9/close",
            "GET /api/staff/payments/payable",
            "POST /api/staff/payments/cash",
        }, handler.Routes());
        Assert.Contains("\"counterId\":1", handler.Calls[2].Body);
        Assert.Contains("\"openingCash\":500000", handler.Calls[2].Body);
        Assert.Contains("\"countedCash\":845000", handler.Calls[4].Body);
        var pay = handler.Calls[^1].Body;
        Assert.Contains("\"requestId\":\"11111111-2222-3333-4444-555555555555\"", pay);
        Assert.Contains("\"orderId\":42", pay);
        Assert.Contains("\"amountReceived\":100000", pay);
    }

    [Fact]
    public async Task Preparation_calls_use_the_agreed_routes()
    {
        var handler = new RecordingHandler();
        using var api = NewApi(handler);
        var staff = new StaffApi(api);
        await staff.PreparationQueueAsync();
        await staff.StartUnitAsync(3);
        await staff.MarkReadyAsync(3);
        await staff.DeliverAsync(3);

        Assert.Equal(new[]
        {
            "GET /api/staff/preparation",
            "POST /api/staff/preparation/3/start",
            "POST /api/staff/preparation/3/ready",
            "POST /api/staff/preparation/3/deliver",
        }, handler.Routes());
        Assert.All(handler.Calls.Skip(1), c => Assert.Equal("tok-1", c.Csrf));
    }

    [Fact]
    public void Every_permission_has_a_policy_entry()
    {
        Assert.Equal(new[] { "MENU_MANAGE", "ORDER_RECEIVE", "PAYMENT_COLLECT", "PREPARATION_WORK", "STAFF_MANAGE", "INVENTORY_WORK", "INVENTORY_APPROVE" },
            Permissions.All.ToArray());
    }
}
