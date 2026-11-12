using CafeManager.Core;
using CafeManager.Infrastructure;
using CafeManager.Infrastructure.Services;
using CafeManager.Web.Models;
using CafeManager.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
// Cookie chỉ bắt buộc HTTPS khi không phải môi trường phát triển (thử cục bộ qua http://localhost vẫn chạy được).
var cookieSecurity = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // mọi yêu cầu ghi dùng cookie phải kèm token chống giả mạo
    o.Filters.Add<ApiExceptionFilter>();
}).ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = _ =>
    new BadRequestObjectResult(new ApiError(ApiErrorCodes.Validation, "Dữ liệu gửi lên không hợp lệ.")));
builder.Services.AddDbContext<CafeDbContext>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("CafeDatabase") ?? throw new InvalidOperationException("Missing CafeDatabase connection.")));
builder.Services.AddIdentityCore<IdentityUser>(IdentitySetup.Configure).AddEntityFrameworkStores<CafeDbContext>().AddDefaultTokenProviders();
// No Identity roles: StaffRole -> RolePermission is the only business permission source.
builder.Services.AddScoped<ICafeDataCommands, CafeDataCommands>();
builder.Services.AddScoped<StaffPermissionReader>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<MenuQueryService>();
builder.Services.AddScoped<CustomerOrderService>();
builder.Services.AddScoped<StaffOrderService>();
builder.Services.AddScoped<StaffMenuService>();

// Giỏ hàng và trạng thái đăng nhập của Web khách dùng session (B); API cho Desktop dùng cookie xác thực riêng.
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.Name = ".CafeManager.Cart";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = cookieSecurity;
});
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN"; // Desktop lấy token ở GET /api/auth/antiforgery rồi gửi qua header này
    o.Cookie.Name = ".CafeManager.Antiforgery";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = cookieSecurity;
});
builder.Services.AddAuthentication(AuthSetup.Scheme).AddCookie(AuthSetup.Scheme, o => AuthSetup.ConfigureCookie(o, cookieSecurity));
builder.Services.AddAuthorization(AuthSetup.ConfigureAuthorization);
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

var app = builder.Build();
if (args.Contains("--migrate") || args.Contains("--seed") || args.Contains("--seed-accounts"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Development command only.");
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CafeDbContext>();
    var target = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString());
    if (target.InitialCatalog != "CafeManager_Group6_Dev")
        throw new InvalidOperationException("CLI only permits CafeManager_Group6_Dev; review another target manually.");
    Console.WriteLine($"Development database: {target.DataSource} / {target.InitialCatalog}");
    if (args.Contains("--migrate")) await db.Database.MigrateAsync();
    if (args.Contains("--seed")) await DevelopmentSeeder.SeedAsync(db);
    if (args.Contains("--seed-accounts"))
    {
        // Mật khẩu ngẫu nhiên, chỉ in một lần ở đây, không lưu và không ghi log.
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var accounts = await DevelopmentAccountSeeder.SeedAsync(db, users);
        Console.WriteLine("Tài khoản mẫu (mật khẩu chỉ hiện một lần; hãy ghi lại ngay):");
        foreach (var a in accounts)
            Console.WriteLine($"  {a.Kind,-9} {a.UserName,-14} {a.Password ?? "(đã tồn tại, giữ nguyên mật khẩu cũ)"}");
    }
    return;
}

// Giao diện khách (B) gọi nghiệp vụ qua cổng và thực đơn qua MenuSource; mặc định dùng dữ liệu thật.
// Đặt CAFE_FAKE_GATEWAY=1 để chạy giao diện với dữ liệu giả (không cần CSDL).
if (!app.Environment.IsEnvironment("Testing") && Environment.GetEnvironmentVariable("CAFE_FAKE_GATEWAY") != "1")
{
    var scopes = app.Services.GetRequiredService<IServiceScopeFactory>();
    CustomerGatewayProvider.Current = new RealCustomerGateway(scopes);
    MenuSource.Provider = new DbMenuSource(scopes, app.Services.GetRequiredService<IMemoryCache>()).Get;
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
