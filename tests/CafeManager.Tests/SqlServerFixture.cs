using CafeManager.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAFE_TEST_SQLSERVER")))
            Skip = "Set CAFE_TEST_SQLSERVER to explicitly enable real SQL Server tests.";
    }
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private string? connection;
    private string? master;
    private string? ownedDatabase;
    public CafeDbContext Open() => new(new DbContextOptionsBuilder<CafeDbContext>().UseSqlServer(
        connection ?? throw new InvalidOperationException("SQL tests not enabled.")).Options);

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("CAFE_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured)) return;
        var name = "CafeManager_Group6_Test_" + Guid.NewGuid().ToString("N");
        var b = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master" };
        master = b.ConnectionString;
        await using var sql = new SqlConnection(master);
        await sql.OpenAsync();
        await using var create = sql.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{name}]";
        await create.ExecuteNonQueryAsync(); // fails if already exists; never adopt an existing database
        ownedDatabase = name;
        b.InitialCatalog = name; connection = b.ConnectionString;
        await using var db = Open();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (ownedDatabase == null) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(ownedDatabase, "^CafeManager_Group6_Test_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Unsafe test database name.");
        SqlConnection.ClearAllPools();
        await using var sql = new SqlConnection(master);
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{ownedDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{ownedDatabase}];";
        await command.ExecuteNonQueryAsync();
    }
}

