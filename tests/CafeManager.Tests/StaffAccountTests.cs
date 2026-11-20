using System.Net;
using System.Net.Http.Json;
using CafeManager.Client;
using CafeManager.Core;
using CafeManager.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static CafeManager.Tests.TestData;

namespace CafeManager.Tests;

// Kiểm thử A6 (nhân viên và phân quyền, UC16) trên máy chủ thật trong bộ nhớ và SQL Server thật.
public abstract class StaffAccountTestBase(SqlServerFixture fixture) : IAsyncLifetime
{
    private ApiFactory? factory;
    protected ApiFactory Factory => factory ??= new ApiFactory(fixture.Connection);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { if (factory != null) await factory.DisposeAsync(); }

    protected static string Suffix() => Guid.NewGuid().ToString("N")[..8];

    protected HttpClient NewClient() => Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    protected static async Task<ApiError> ErrorOf(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<ApiError>())!;

    protected async Task<(HttpClient Client, string Token)> LoginAsync(string userName, string password = "secret1")
    {
        var client = NewClient();
        var login = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(userName, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await client.GetFromJsonAsync<AntiforgeryTokenResponse>("api/auth/antiforgery"))!.Token;
        return (client, token);
    }

    protected static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return client.SendAsync(request);
    }

    protected static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await ErrorOf(response)).Code);
    }

    protected async Task<T> ReadDbAsync<T>(Func<CafeDbContext, Task<T>> read)
    {
        using var scope = Factory.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<CafeDbContext>());
    }

    // Tạo một vai trò có đúng các quyền chỉ định; trả mã vai trò.
    protected async Task<long> SeedRoleAsync(string roleName, params string[] permissionCodes)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
        var role = new Role { RoleName = roleName };
        db.Add(role); await db.SaveChangesAsync();
        foreach (var code in permissionCodes)
        {
            var permission = await db.Permission.SingleOrDefaultAsync(p => p.PermissionCode == code);
            if (permission == null) { permission = new Permission { PermissionCode = code }; db.Add(permission); await db.SaveChangesAsync(); }
            db.Add(new RolePermission { RoleId = role.RoleId, PermissionId = permission.PermissionId });
        }
        await db.SaveChangesAsync();
        return role.RoleId;
    }

    protected Task<long> RoleIdAsync(string roleName) => ReadDbAsync(db => db.Role.Where(r => r.RoleName == roleName).Select(r => r.RoleId).SingleAsync());

    // Quản trị viên (STAFF_MANAGE); SeedStaffAsync đặt tên đăng nhập "admin.<s>", họ tên "NV admin <s>", vai trò "admin <s>", mật khẩu "secret1".
    protected async Task<(HttpClient Client, string Token, long RoleId)> AdminAsync(string suffix)
    {
        await SeedStaffAsync(Factory.Services, suffix, "admin", Permissions.StaffManage);
        var (client, token) = await LoginAsync("admin." + suffix);
        return (client, token, await RoleIdAsync("admin " + suffix));
    }

    protected static async Task<long> CreateStaffAsync(HttpClient admin, string token, string userName, string fullName, params long[] roleIds)
    {
        var response = await Send(admin, HttpMethod.Post, "api/staff/accounts", token, new CreateStaffRequest(userName, "matkhau1", fullName, roleIds));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }
}

public sealed class StaffAccountTests(SqlServerFixture fixture) : StaffAccountTestBase(fixture), IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task A6_manager_creates_a_staff_member_who_logs_in_with_only_the_assigned_permissions()
    {
        var s = Suffix();
        var (admin, token, _) = await AdminAsync(s);
        var cashierRole = await SeedRoleAsync("Thu ngân " + s, Permissions.PaymentCollect);
        var userName = "nv." + s;
        var fullName = "Nguyễn Văn " + s;

        var id = await CreateStaffAsync(admin, token, "  " + userName + "  ", "  " + fullName + "  ", cashierRole);   // tên được cắt khoảng trắng

        var staff = NewClient();
        var login = await staff.PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "matkhau1"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var user = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.Equal((UserKinds.Staff, fullName), (user.UserKind, user.DisplayName));
        Assert.Equal([Permissions.PaymentCollect], user.Permissions);

        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("api/staff/counters")).StatusCode);
        await AssertError(await staff.GetAsync("api/staff/accounts"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await staff.GetAsync("api/staff/menu"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        await AssertError(await staff.GetAsync("api/staff/preparation"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);

        var found = Assert.Single((await admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString(userName)))!);
        Assert.Equal((id, userName, fullName, true, false), (found.StaffId, found.UserName, found.FullName, found.IsActive, found.IsCurrentUser));
        Assert.Equal([cashierRole], found.RoleIds);
        Assert.Equal(["Thu ngân " + s], found.RoleNames);

        var me = Assert.Single((await admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString("admin." + s)))!);
        Assert.True(me.IsCurrentUser);

        var adminStaffId = await ReadDbAsync(db => db.StaffAccount.Where(x => x.FullName == "NV admin " + s).Select(x => x.StaffId).SingleAsync());
        var audit = Assert.Single(await ReadDbAsync(db => db.AuditEvent.AsNoTracking().Where(a => a.ActionCode == "STAFF_CREATE" && a.EntityReference == userName).ToListAsync()));
        Assert.Equal((adminStaffId, "StaffAccount"), (audit.StaffId, audit.EntityName));
        Assert.Contains("Thu ngân " + s, audit.Reason);
    }

    [SqlServerFact]
    public async Task A6_create_rejects_bad_input_and_duplicates_and_leaves_no_half_created_account()
    {
        var d = await SeedAsync(Factory.Services);
        var (admin, token, adminRole) = await AdminAsync(d.Suffix);
        var userName = "moi." + d.Suffix;
        Task<HttpResponseMessage> Create(string user, string password, string name, long[] roles) =>
            Send(admin, HttpMethod.Post, "api/staff/accounts", token, new CreateStaffRequest(user, password, name, roles));

        Assert.Equal(HttpStatusCode.Created, (await Create(userName, "matkhau1", "Người mới", [adminRole])).StatusCode);
        await AssertError(await Create(userName, "matkhau1", "Trùng", [adminRole]), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Create(userName.ToUpperInvariant(), "matkhau1", "Trùng hoa thường", [adminRole]), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Create(d.Customer, "matkhau1", "Trùng khách", [adminRole]), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);   // tên đã là của một khách

        var bad = "loi." + d.Suffix;
        await AssertError(await Create("ab", "matkhau1", "Tên ngắn", [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create("nguyễn.an", "matkhau1", "Tên có dấu", [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(new string('a', 51), "matkhau1", "Tên dài", [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(bad, "12345", "Mật khẩu ngắn", [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(bad, new string('x', 101), "Mật khẩu dài", [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(bad, "matkhau1", "   ", [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(bad, "matkhau1", new string('a', 201), [adminRole]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(bad, "matkhau1", "Không vai trò", []), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Create(bad, "matkhau1", "Vai trò lạ", [999_999_999]), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);

        Assert.False(await ReadDbAsync(db => db.Users.AnyAsync(u => u.UserName == bad)));               // không tài khoản Identity mồ côi
        Assert.False(await ReadDbAsync(db => db.Users.AnyAsync(u => u.UserName == "ab")));
        Assert.Equal(1, await ReadDbAsync(db => db.StaffAccount.CountAsync(x => x.FullName == "Người mới")));
        Assert.Equal(1, await ReadDbAsync(db => db.AuditEvent.CountAsync(a => a.ActionCode == "STAFF_CREATE" && a.EntityReference == userName)));
    }

    [SqlServerFact]
    public async Task A6_changing_roles_takes_effect_immediately_on_an_open_session()
    {
        var s = Suffix();
        var (admin, token, adminRole) = await AdminAsync(s);
        var cashierRole = await SeedRoleAsync("Thu ngân " + s, Permissions.PaymentCollect);
        var baristaRole = await SeedRoleAsync("Pha chế " + s, Permissions.PreparationWork);
        var userName = "nv." + s;
        var id = await CreateStaffAsync(admin, token, userName, "Nhân viên " + s, cashierRole);

        var (staff, _) = await LoginAsync(userName, "matkhau1");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("api/staff/counters")).StatusCode);
        await AssertError(await staff.GetAsync("api/staff/preparation"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);

        var renamed = await Send(admin, HttpMethod.Put, $"api/staff/accounts/{id}", token, new UpdateStaffRequest("  Đổi tên " + s + "  ", [baristaRole]));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal(id, (await renamed.Content.ReadFromJsonAsync<IdResponse>())!.Id);

        // Cùng phiên cũ, không đăng nhập lại: quyền cũ mất, quyền mới có ngay.
        await AssertError(await staff.GetAsync("api/staff/counters"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("api/staff/preparation")).StatusCode);

        var row = Assert.Single((await admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString(userName)))!);
        Assert.Equal("Đổi tên " + s, row.FullName);                 // họ tên được cắt khoảng trắng
        Assert.Equal(new[] { baristaRole }, row.RoleIds.ToArray());
        Assert.Equal(new[] { "Pha chế " + s }, row.RoleNames.ToArray());

        // Nhiều vai trò: quyền là hợp của các vai trò, không trùng lặp.
        Assert.Equal(HttpStatusCode.OK, (await Send(admin, HttpMethod.Put, $"api/staff/accounts/{id}", token, new UpdateStaffRequest("Đổi tên " + s, [cashierRole, baristaRole, cashierRole]))).StatusCode);
        var relogin = (await (await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "matkhau1"))).Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.Equal([Permissions.PaymentCollect, Permissions.PreparationWork], relogin.Permissions);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("api/staff/counters")).StatusCode);

        Assert.Equal(2, await ReadDbAsync(db => db.AuditEvent.CountAsync(a => a.ActionCode == "STAFF_UPDATE" && a.EntityReference == userName)));

        await AssertError(await Send(admin, HttpMethod.Put, "api/staff/accounts/999999999", token, new UpdateStaffRequest("Không có", [adminRole])), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);
        await AssertError(await Send(admin, HttpMethod.Put, $"api/staff/accounts/{id}", token, new UpdateStaffRequest("Không vai trò", [])), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(admin, HttpMethod.Put, $"api/staff/accounts/{id}", token, new UpdateStaffRequest("  ", [adminRole])), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        await AssertError(await Send(admin, HttpMethod.Put, $"api/staff/accounts/{id}", token, new UpdateStaffRequest("Vai trò lạ", [999_999_999])), HttpStatusCode.BadRequest, ApiErrorCodes.Validation);
        Assert.Equal(2, (await admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString(userName)))!.Single().RoleIds.Count);   // lỗi không làm đổi dữ liệu
    }

    [SqlServerFact]
    public async Task A6_deactivate_blocks_login_ends_open_sessions_and_activate_restores_access()
    {
        var s = Suffix();
        var (admin, token, _) = await AdminAsync(s);
        var cashierRole = await SeedRoleAsync("Thu ngân " + s, Permissions.PaymentCollect);
        var userName = "nv." + s;
        var id = await CreateStaffAsync(admin, token, userName, "Nhân viên " + s, cashierRole);
        var (staff, _) = await LoginAsync(userName, "matkhau1");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("api/staff/counters")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, HttpMethod.Post, $"api/staff/accounts/{id}/deactivate", token)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, HttpMethod.Post, $"api/staff/accounts/{id}/deactivate", token)).StatusCode);   // gọi lại không lỗi

        await AssertError(await staff.GetAsync("api/staff/counters"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);               // phiên đang mở bị thu hồi
        await AssertError(await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "matkhau1")), HttpStatusCode.Forbidden, ApiErrorCodes.AccountLocked);
        Assert.False((await admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString(userName)))!.Single().IsActive);

        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, HttpMethod.Post, $"api/staff/accounts/{id}/activate", token)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, HttpMethod.Post, $"api/staff/accounts/{id}/activate", token)).StatusCode);     // gọi lại không lỗi
        var (again, _) = await LoginAsync(userName, "matkhau1");
        Assert.Equal(HttpStatusCode.OK, (await again.GetAsync("api/staff/counters")).StatusCode);

        // Mỗi lần đổi trạng thái thật chỉ ghi nhật ký một lần.
        Assert.Equal(1, await ReadDbAsync(db => db.AuditEvent.CountAsync(a => a.ActionCode == "STAFF_DEACTIVATE" && a.EntityReference == userName)));
        Assert.Equal(1, await ReadDbAsync(db => db.AuditEvent.CountAsync(a => a.ActionCode == "STAFF_ACTIVATE" && a.EntityReference == userName)));

        await AssertError(await Send(admin, HttpMethod.Post, "api/staff/accounts/999999999/deactivate", token), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);
        await AssertError(await Send(admin, HttpMethod.Post, "api/staff/accounts/999999999/activate", token), HttpStatusCode.NotFound, ApiErrorCodes.NotFound);
    }

    [SqlServerFact]
    public async Task A6_activate_also_clears_the_temporary_lockout_after_too_many_wrong_passwords()
    {
        var s = Suffix();
        var (admin, token, _) = await AdminAsync(s);
        var cashierRole = await SeedRoleAsync("Thu ngân " + s, Permissions.PaymentCollect);
        var userName = "nv." + s;
        var id = await CreateStaffAsync(admin, token, userName, "Nhân viên " + s, cashierRole);

        for (var attempt = 1; attempt <= 4; attempt++)
            await AssertError(await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "sai-mat-khau")), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);
        await AssertError(await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "sai-mat-khau")), HttpStatusCode.Forbidden, ApiErrorCodes.AccountLocked);
        await AssertError(await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "matkhau1")), HttpStatusCode.Forbidden, ApiErrorCodes.AccountLocked);   // đúng mật khẩu vẫn bị khóa tạm

        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, HttpMethod.Post, $"api/staff/accounts/{id}/activate", token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await NewClient().PostAsJsonAsync("api/auth/login", new LoginRequest(userName, "matkhau1"))).StatusCode);
    }

    [SqlServerFact]
    public async Task A6_only_staff_with_the_staff_manage_permission_can_use_the_endpoints()
    {
        var d = await SeedAsync(Factory.Services);
        var (admin, token, adminRole) = await AdminAsync(d.Suffix);
        var id = await CreateStaffAsync(admin, token, "nv." + d.Suffix, "Nhân viên", adminRole);

        var anonymous = NewClient();
        await AssertError(await anonymous.GetAsync("api/staff/accounts"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);
        await AssertError(await anonymous.GetAsync("api/staff/roles"), HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized);

        var menuManager = await SeedManagerAsync(Factory.Services, d.Suffix);   // MENU_MANAGE không bao gồm quản lý nhân viên
        foreach (var userName in new[] { d.Cashier, menuManager, d.Customer })
        {
            var (client, t) = await LoginAsync(userName);
            await AssertError(await client.GetAsync("api/staff/accounts"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
            await AssertError(await client.GetAsync("api/staff/roles"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
            await AssertError(await Send(client, HttpMethod.Post, "api/staff/accounts", t, new CreateStaffRequest("x." + userName, "matkhau1", "X", [adminRole])), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
            await AssertError(await Send(client, HttpMethod.Put, $"api/staff/accounts/{id}", t, new UpdateStaffRequest("X", [adminRole])), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
            await AssertError(await Send(client, HttpMethod.Post, $"api/staff/accounts/{id}/deactivate", t), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
            await AssertError(await Send(client, HttpMethod.Post, $"api/staff/accounts/{id}/activate", t), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);
        }
        Assert.True((await admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString("nv." + d.Suffix)))!.Single().IsActive);   // không bị đổi
    }

    [SqlServerFact]
    public async Task A6_roles_list_shows_permission_codes_and_search_matches_user_name_or_full_name()
    {
        var s = Suffix();
        var (admin, token, adminRole) = await AdminAsync(s);
        var both = await SeedRoleAsync("Ca đôi " + s, Permissions.PreparationWork, Permissions.OrderReceive);
        var none = await SeedRoleAsync("Chưa quyền " + s);

        var roles = (await admin.GetFromJsonAsync<List<RoleDto>>("api/staff/roles"))!;
        Assert.Equal([Permissions.OrderReceive, Permissions.PreparationWork], roles.Single(r => r.RoleId == both).Permissions);   // sắp theo mã
        Assert.Empty(roles.Single(r => r.RoleId == none).Permissions);
        Assert.Equal([Permissions.StaffManage], roles.Single(r => r.RoleId == adminRole).Permissions);

        await CreateStaffAsync(admin, token, "lan." + s, "Trần Thị Lan " + s, both);
        await CreateStaffAsync(admin, token, "minh." + s, "Lê Văn Minh " + s, none);
        Task<List<StaffAccountDto>> Search(string q) => admin.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts?q=" + Uri.EscapeDataString(q))!;

        Assert.Equal(["lan." + s], (await Search("Thị Lan " + s)).Select(x => x.UserName));          // theo họ tên
        Assert.Equal(["minh." + s], (await Search("MINH." + s)).Select(x => x.UserName));            // theo tên đăng nhập, không phân biệt hoa thường
        Assert.Empty(await Search("khong-co-ai-ten-nay-" + s));
        Assert.True((await Search("")).Count >= 3);                                                  // admin và hai nhân viên vừa tạo
        Assert.Equal(new[] { "Chưa quyền " + s }, (await Search("minh." + s)).Single().RoleNames.ToArray());
    }
}

// Lớp riêng để có cơ sở dữ liệu riêng: phép kiểm "quản trị viên cuối cùng" chỉ đúng khi không có quản trị viên của ca kiểm thử khác.
public sealed class StaffAccountLastAdminTests(SqlServerFixture fixture) : StaffAccountTestBase(fixture), IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task A6_nobody_can_lock_themselves_or_remove_the_last_active_administrator()
    {
        var s = Suffix();
        var (a, tokenA, adminRole) = await AdminAsync(s);
        var cashierRole = await SeedRoleAsync("Thu ngân " + s, Permissions.PaymentCollect);
        var aId = (await a.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts"))!.Single(x => x.IsCurrentUser).StaffId;

        // A là quản trị viên duy nhất: không tự khóa, không tự bỏ vai trò quản trị.
        await AssertError(await Send(a, HttpMethod.Post, $"api/staff/accounts/{aId}/deactivate", tokenA), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Send(a, HttpMethod.Put, $"api/staff/accounts/{aId}", tokenA, new UpdateStaffRequest("NV admin " + s, [cashierRole])), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("api/staff/accounts")).StatusCode);   // vẫn nguyên quyền

        // Thêm B làm quản trị viên thứ hai: A mới được bỏ vai trò quản trị của chính mình.
        var bId = await CreateStaffAsync(a, tokenA, "b." + s, "Quản trị B " + s, adminRole);
        Assert.Equal(HttpStatusCode.OK, (await Send(a, HttpMethod.Put, $"api/staff/accounts/{aId}", tokenA, new UpdateStaffRequest("NV admin " + s, [cashierRole]))).StatusCode);
        await AssertError(await a.GetAsync("api/staff/accounts"), HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden);   // A mất quyền ngay

        // B giờ là người cuối cùng.
        var (b, tokenB) = await LoginAsync("b." + s, "matkhau1");
        await AssertError(await Send(b, HttpMethod.Post, $"api/staff/accounts/{bId}/deactivate", tokenB), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        await AssertError(await Send(b, HttpMethod.Put, $"api/staff/accounts/{bId}", tokenB, new UpdateStaffRequest("Quản trị B " + s, [cashierRole])), HttpStatusCode.Conflict, ApiErrorCodes.Conflict);
        Assert.Equal(HttpStatusCode.OK, (await Send(b, HttpMethod.Put, $"api/staff/accounts/{bId}", tokenB, new UpdateStaffRequest("Quản trị B đổi tên " + s, [adminRole, cashierRole]))).StatusCode);   // đổi tên, thêm vai trò thì được

        // B khóa A (không còn là quản trị viên) thì được; B vẫn nguyên.
        Assert.Equal(HttpStatusCode.NoContent, (await Send(b, HttpMethod.Post, $"api/staff/accounts/{aId}/deactivate", tokenB)).StatusCode);
        var rows = (await b.GetFromJsonAsync<List<StaffAccountDto>>("api/staff/accounts"))!;
        Assert.False(rows.Single(x => x.StaffId == aId).IsActive);
        Assert.True(rows.Single(x => x.StaffId == bId).IsActive);
    }
}

// Lớp StaffApi THẬT của Desktop (ApiClient + cookie + antiforgery) gọi máy chủ thật: đường dẫn, JSON và mã lỗi mà màn hình Nhân viên dựa vào.
// Chưa phải kiểm thử giao diện Windows Forms.
public sealed class StaffAccountClientTests(SqlServerFixture fixture) : StaffAccountTestBase(fixture), IClassFixture<SqlServerFixture>
{
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

    private ApiClient NewApi() => new(new ApiClientOptions(new Uri("http://localhost/"), TimeSpan.FromSeconds(15)), new CookieHandler(Factory.Server.CreateHandler()));

    [SqlServerFact]
    public async Task A6_desktop_client_creates_edits_locks_and_unlocks_staff()
    {
        var s = Suffix();
        await SeedStaffAsync(Factory.Services, s, "admin", Permissions.StaffManage);
        var cashierRole = await SeedRoleAsync("Thu ngân " + s, Permissions.PaymentCollect);
        using var adminApi = NewApi();
        var login = await adminApi.LoginAsync("admin." + s, "secret1");
        Assert.True(login.Ok, login.Message);
        Assert.Contains(Permissions.StaffManage, adminApi.CurrentUser!.Permissions);   // Form1 dùng quyền này để hiện nút Nhân viên
        var staff = new StaffApi(adminApi);

        var roles = await staff.RolesAsync();
        Assert.True(roles.Ok, roles.Message);
        Assert.Contains(roles.Value!, r => r.RoleId == cashierRole && r.Permissions.SequenceEqual([Permissions.PaymentCollect]));

        var userName = "nv." + s;
        var created = await staff.CreateStaffAsync(new CreateStaffRequest(userName, "matkhau1", "Nguyễn " + s, [cashierRole]));
        Assert.True(created.Ok, created.Message);
        var duplicate = await staff.CreateStaffAsync(new CreateStaffRequest(userName, "matkhau1", "Trùng", [cashierRole]));
        Assert.Equal((false, 409, ApiErrorCodes.Conflict), (duplicate.Ok, duplicate.StatusCode, duplicate.Error!.Code));
        Assert.Equal("Tên đăng nhập đã được dùng.", StaffAccountText.ForSave(duplicate, "x"));

        var listed = await staff.StaffAccountsAsync(userName);
        var row = Assert.Single(listed.Value!);
        Assert.Equal((created.Value!.Id, true, "Đang hoạt động"), (row.StaffId, row.IsActive, StaffAccountText.Status(row)));
        Assert.Equal("Thu ngân " + s, StaffAccountText.Roles(row));

        var edited = await staff.UpdateStaffAsync(row.StaffId, new UpdateStaffRequest("Nguyễn mới " + s, [cashierRole]));
        Assert.True(edited.Ok, edited.Message);

        // Khóa: không đăng nhập được; ApiClient báo mã ACCOUNT_LOCKED.
        var locked = await staff.SetStaffActiveAsync(row.StaffId, false);
        Assert.True(locked.Ok, locked.Message);
        Assert.Equal("Đã khóa tài khoản.", StaffAccountText.ForToggle(locked, activated: false));
        using (var blocked = NewApi())
        {
            var attempt = await blocked.LoginAsync(userName, "matkhau1");
            Assert.Equal((false, 403, ApiErrorCodes.AccountLocked), (attempt.Ok, attempt.StatusCode, attempt.Error!.Code));
        }
        Assert.Equal("Đã khóa", StaffAccountText.Status((await staff.StaffAccountsAsync(userName)).Value!.Single()));

        // Mở lại: đăng nhập được và quyền đúng như đã gán.
        Assert.True((await staff.SetStaffActiveAsync(row.StaffId, true)).Ok);
        using var staffApi = NewApi();
        var back = await staffApi.LoginAsync(userName, "matkhau1");
        Assert.True(back.Ok, back.Message);
        Assert.Equal("Nguyễn mới " + s, staffApi.CurrentUser!.DisplayName);
        Assert.Equal([Permissions.PaymentCollect], staffApi.CurrentUser.Permissions);
        var forbidden = await new StaffApi(staffApi).StaffAccountsAsync();
        Assert.Equal((false, 403), (forbidden.Ok, forbidden.StatusCode));
        Assert.Contains("không có quyền", StaffMessages.ForLoad(forbidden, "danh sách nhân viên"));

        // Tự khóa mình bị chặn kể cả khi còn người khác: máy chủ trả 409, và giao diện cũng không cho bấm.
        var me = (await staff.StaffAccountsAsync("admin." + s)).Value!.Single();
        Assert.True(me.IsCurrentUser);
        Assert.False(StaffAccountText.CanToggle(me));
        var self = await staff.SetStaffActiveAsync(me.StaffId, false);
        Assert.Equal((false, 409), (self.Ok, self.StatusCode));
        Assert.Contains("tự khóa", self.Message);
    }
}

// Lớp riêng để có cơ sở dữ liệu riêng (lệnh seed tạo vai trò và tài khoản dev.* theo tên cố định).
public sealed class StaffAccountSeedTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    // CSDL Dev cũ: vai trò Manager chỉ có MENU_MANAGE và dev.manager đã có mật khẩu riêng. Chạy lại --seed-accounts phải cấp thêm STAFF_MANAGE
    // cho vai trò đó mà không đổi mật khẩu hay tạo trùng tài khoản (đúng điều docs/hop-dong-api.md hướng dẫn).
    [SqlServerFact]
    public async Task A6_reseeding_gives_an_existing_manager_role_the_new_permission_and_keeps_passwords()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => fixture.Open());
        services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<CafeDbContext>();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var role = new Role { RoleName = "Manager" };
        var menuManage = new Permission { PermissionCode = Permissions.MenuManage };
        db.AddRange(role, menuManage); await db.SaveChangesAsync();
        db.Add(new RolePermission { RoleId = role.RoleId, PermissionId = menuManage.PermissionId });
        var user = new IdentityUser { UserName = "dev.manager" };
        Assert.True((await users.CreateAsync(user, "Mat-khau-cu-1")).Succeeded);
        var staff = new StaffAccount { IdentityUserId = user.Id, FullName = "Quản lý cũ", IsActive = true };
        db.Add(staff); await db.SaveChangesAsync();
        db.Add(new StaffRole { StaffId = staff.StaffId, RoleId = role.RoleId });
        await db.SaveChangesAsync();
        var reader = new StaffPermissionReader(db);
        Assert.False(await reader.HasAsync(user.Id, Permissions.StaffManage));

        var seeded = await DevelopmentAccountSeeder.SeedAsync(db, users);

        Assert.Null(seeded.Single(a => a.UserName == "dev.manager").Password);                 // tài khoản đã có: không đổi, không in mật khẩu
        Assert.True(await users.CheckPasswordAsync(user, "Mat-khau-cu-1"));
        Assert.True(await reader.HasAsync(user.Id, Permissions.StaffManage));
        Assert.True(await reader.HasAsync(user.Id, Permissions.MenuManage));
        Assert.Equal(1, await db.Role.CountAsync(r => r.RoleName == "Manager"));
        Assert.Equal(1, await db.StaffAccount.CountAsync(x => x.IdentityUserId == user.Id));
        Assert.Equal("Quản lý cũ", (await db.StaffAccount.AsNoTracking().SingleAsync(x => x.IdentityUserId == user.Id)).FullName);
    }
}
