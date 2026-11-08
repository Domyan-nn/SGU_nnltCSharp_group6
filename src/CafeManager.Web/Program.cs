using CafeManager.Core;
using CafeManager.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CafeDbContext>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("CafeDatabase") ?? throw new InvalidOperationException("Missing CafeDatabase connection.")));
builder.Services.AddIdentityCore<IdentityUser>().AddEntityFrameworkStores<CafeDbContext>().AddDefaultTokenProviders();
// No Identity roles: StaffRole -> RolePermission is the only business permission source.
builder.Services.AddScoped<ICafeDataCommands, CafeDataCommands>();
builder.Services.AddScoped<StaffPermissionReader>();
var app = builder.Build();
if (args.Contains("--migrate") || args.Contains("--seed"))
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
    return;
}
app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/", () => "CafeManager Nhóm 6 — máy chủ dữ liệu. Các màn hình và API tác nghiệp sẽ được bổ sung sau.");
app.Run();
