using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CafeManager.Infrastructure;
public sealed class CafeDbContextFactory : IDesignTimeDbContextFactory<CafeDbContext>
{
    public const string DevelopmentConnection = @"Server=(localdb)\MSSQLLocalDB;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true";
    public CafeDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<CafeDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__CafeDatabase") ?? DevelopmentConnection).Options);
}

