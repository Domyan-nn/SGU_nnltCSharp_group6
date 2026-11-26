using System.Net;
using System.Text;
using System.Text.Json;
using CafeManager.Client;
using CafeManager.Core;

namespace CafeManager.Tests;

// A6: logic kiểm tra đầu vào, chữ hiển thị và lời gọi API của màn hình Nhân viên. Không cần SQL Server hay Windows Forms.
public sealed class StaffAccountPresentationTests
{
    private static StaffAccountDto Staff(bool active = true, bool self = false, params string[] roles) =>
        new(7, "nv.an", "Nguyễn An", active, roles.Select((_, i) => (long)i + 1).ToList(), roles, self);

    // ---------------------------------------------------------------- Kiểm tra đầu vào

    [Theory]
    [InlineData("nv.an", true)]
    [InlineData("  nv-an_01  ", true)]
    [InlineData("abc", true)]
    [InlineData("ab", false)]              // quá ngắn
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("nguyễn.an", false)]       // có dấu
    [InlineData("nv an", false)]           // có khoảng trắng ở giữa
    [InlineData("nv@an", false)]
    public void UserName_follows_the_same_rule_as_the_server(string text, bool valid)
    {
        Assert.Equal(valid, StaffAccountInput.TryUserName(text, out var userName, out var error));
        if (valid) { Assert.Equal(text.Trim(), userName); Assert.Equal("", error); }
        else Assert.NotEqual("", error);
    }

    [Fact]
    public void UserName_is_limited_to_50_characters()
    {
        Assert.True(StaffAccountInput.TryUserName(new string('a', 50), out _, out _));
        Assert.False(StaffAccountInput.TryUserName(new string('a', 51), out _, out _));
    }

    [Fact]
    public void Password_needs_6_to_100_characters_entered_twice_the_same_and_is_not_trimmed()
    {
        Assert.False(StaffAccountInput.TryPassword("12345", "12345", out var tooShort));
        Assert.Contains("6", tooShort);
        Assert.False(StaffAccountInput.TryPassword(null, null, out _));
        Assert.False(StaffAccountInput.TryPassword(new string('x', 101), new string('x', 101), out _));
        Assert.False(StaffAccountInput.TryPassword("matkhau1", "matkhau2", out var mismatch));
        Assert.Contains("giống", mismatch);
        Assert.False(StaffAccountInput.TryPassword("matkhau1", "Matkhau1", out _));      // phân biệt hoa thường
        Assert.True(StaffAccountInput.TryPassword("matkhau1", "matkhau1", out var ok)); Assert.Equal("", ok);
        Assert.True(StaffAccountInput.TryPassword(" ab cd ", " ab cd ", out _));         // khoảng trắng là ký tự mật khẩu
        Assert.False(StaffAccountInput.TryPassword(" ab cd ", "ab cd", out _));
    }

    [Fact]
    public void FullName_is_required_trimmed_and_limited_to_200_characters()
    {
        Assert.False(StaffAccountInput.TryFullName("   ", out _, out var empty)); Assert.Contains("họ tên", empty);
        Assert.True(StaffAccountInput.TryFullName("  Nguyễn Văn An  ", out var name, out _)); Assert.Equal("Nguyễn Văn An", name);
        Assert.True(StaffAccountInput.TryFullName(new string('a', 200), out _, out _));
        Assert.False(StaffAccountInput.TryFullName(new string('a', 201), out _, out _));
    }

    [Fact]
    public void Roles_need_at_least_one_and_duplicates_are_merged()
    {
        Assert.False(StaffAccountInput.TryRoles([], out _, out var error)); Assert.Contains("vai trò", error);
        Assert.True(StaffAccountInput.TryRoles([3, 3, 5], out var ids, out _)); Assert.Equal([3L, 5L], ids);
    }

    // ---------------------------------------------------------------- Chữ hiển thị

    [Fact]
    public void Every_permission_has_a_friendly_name_so_a_new_permission_cannot_be_forgotten()
    {
        Assert.Contains(Permissions.StaffManage, Permissions.All);
        Assert.All(Permissions.All, code => Assert.NotEqual(code, StaffAccountText.Permission(code)));
        Assert.Equal("Quản lý nhân viên", StaffAccountText.Permission(Permissions.StaffManage));
        Assert.Equal("Kho nguyên liệu", StaffAccountText.Permission(Permissions.InventoryWork));
        Assert.Equal("Duyệt kiểm kê", StaffAccountText.Permission(Permissions.InventoryApprove));
        Assert.Equal("QUYEN_LA", StaffAccountText.Permission("QUYEN_LA"));   // mã lạ hiện nguyên, không giấu
    }

    [Fact]
    public void Status_roles_and_role_label_are_readable()
    {
        Assert.Equal("Đang hoạt động", StaffAccountText.Status(Staff()));
        Assert.Equal("Đã khóa", StaffAccountText.Status(Staff(active: false)));
        Assert.Equal("Cashier, Barista", StaffAccountText.Roles(Staff(true, false, "Cashier", "Barista")));
        Assert.Equal("(chưa có vai trò)", StaffAccountText.Roles(Staff()));
        Assert.Equal("Cashier (Tiếp nhận đơn, Thu ngân)",
            StaffAccountText.RoleLabel(new RoleDto(1, "Cashier", [Permissions.OrderReceive, Permissions.PaymentCollect])));
        Assert.Equal("Rỗng (chưa cấp quyền nào)", StaffAccountText.RoleLabel(new RoleDto(2, "Rỗng", [])));
    }

    [Fact]
    public void Toggle_caption_follows_the_selected_row_and_a_user_cannot_lock_themselves()
    {
        Assert.Equal("Khóa…", StaffAccountText.ToggleCaption(null));
        Assert.Equal("Khóa…", StaffAccountText.ToggleCaption(Staff()));
        Assert.Equal("Mở khóa", StaffAccountText.ToggleCaption(Staff(active: false)));
        Assert.False(StaffAccountText.CanToggle(null));
        Assert.True(StaffAccountText.CanToggle(Staff()));
        Assert.True(StaffAccountText.CanToggle(Staff(active: false)));
        Assert.False(StaffAccountText.CanToggle(Staff(active: true, self: true)));
        Assert.Contains("nv.an", StaffAccountText.ConfirmToggle(Staff()));
        Assert.Contains("thu hồi", StaffAccountText.ConfirmToggle(Staff()));
        Assert.StartsWith("Mở khóa", StaffAccountText.ConfirmToggle(Staff(active: false)));
    }

    [Fact]
    public void Messages_never_report_an_unknown_outcome_as_success_or_failure()
    {
        ApiResult<IdResponse> Result(bool ok, int status, bool unknown, ApiError? error) => new(ok, status, ok ? new IdResponse(1) : null, error, unknown);
        Assert.Equal("Đã lưu.", StaffAccountText.ForSave(Result(true, 200, false, null), "Đã lưu."));
        var unknownText = StaffAccountText.ForSave(Result(false, 0, true, new ApiError("", "Mất kết nối.")), "Đã lưu.");
        Assert.StartsWith("Mất kết nối.", unknownText); Assert.Contains("kiểm tra", unknownText);
        Assert.Equal("Tên đăng nhập đã được dùng.", StaffAccountText.ForSave(Result(false, 409, false, new ApiError(ApiErrorCodes.Conflict, "Tên đăng nhập đã được dùng.")), "x"));
        Assert.Contains("không có quyền", StaffAccountText.ForSave(Result(false, 403, false, new ApiError(ApiErrorCodes.Forbidden, "x")), "x"));
        Assert.Contains("Không tìm thấy", StaffAccountText.ForSave(Result(false, 404, false, new ApiError(ApiErrorCodes.NotFound, "x")), "x"));
        Assert.Equal("Đã khóa tài khoản.", StaffAccountText.ForToggle(new ApiResult<Empty>(true, 204, new Empty(), null, false), activated: false));
        Assert.Equal("Đã mở khóa tài khoản.", StaffAccountText.ForToggle(new ApiResult<Empty>(true, 204, new Empty(), null, false), activated: true));
    }

    // ---------------------------------------------------------------- Lời gọi API (đường dẫn, phương thức, JSON)

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string Body)> Calls { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add((request.Method, request.RequestUri!.PathAndQuery, request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return send(request);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, object body) =>
        new(code) { Content = new StringContent(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json") };

    [Fact]
    public async Task StaffApi_calls_the_documented_paths_with_the_documented_bodies()
    {
        var handler = new FakeHandler(req => req.Method == HttpMethod.Get || req.RequestUri!.AbsolutePath.EndsWith("accounts")
            ? Json(HttpStatusCode.OK, new IdResponse(9)) : new HttpResponseMessage(HttpStatusCode.NoContent));
        using var api = new ApiClient(new ApiClientOptions(new Uri("https://api.test/"), TimeSpan.FromSeconds(5)), handler);
        var staff = new StaffApi(api);

        await staff.StaffAccountsAsync();
        await staff.StaffAccountsAsync("  Nguyễn An ");
        await staff.RolesAsync();
        await staff.CreateStaffAsync(new CreateStaffRequest("nv.an", "matkhau1", "Nguyễn An", [1, 2]));
        await staff.UpdateStaffAsync(9, new UpdateStaffRequest("Nguyễn An B", [2]));
        await staff.SetStaffActiveAsync(9, active: false);
        await staff.SetStaffActiveAsync(9, active: true);

        Assert.Equal(
        [
            (HttpMethod.Get, "/api/staff/accounts"),
            (HttpMethod.Get, "/api/staff/accounts?q=Nguy%E1%BB%85n%20An"),
            (HttpMethod.Get, "/api/staff/roles"),
            (HttpMethod.Post, "/api/staff/accounts"),
            (HttpMethod.Put, "/api/staff/accounts/9"),
            (HttpMethod.Post, "/api/staff/accounts/9/deactivate"),
            (HttpMethod.Post, "/api/staff/accounts/9/activate"),
        ], handler.Calls.Select(c => (c.Method, c.Path)).ToList());
        Assert.Equal("{\"userName\":\"nv.an\",\"password\":\"matkhau1\",\"fullName\":\"Nguyễn An\",\"roleIds\":[1,2]}",
            handler.Calls[3].Body.Replace("\\u1EC5", "ễ"));
        Assert.Equal("{\"fullName\":\"Nguyễn An B\",\"roleIds\":[2]}", handler.Calls[4].Body.Replace("\\u1EC5", "ễ"));
    }
}
