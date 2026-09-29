using Microsoft.Data.SqlClient;

namespace ProductManagement.Api.Tests.Support;

public sealed class SqlServerTestDatabaseFixture : IAsyncLifetime
{
    private const string DefaultBaseConnectionString =
        "Server=localhost,1433;Database=master;User Id=sa;Password=123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True";

    private readonly string _masterConnectionString;
    private bool _createdDatabase;

    public SqlServerTestDatabaseFixture()
    {
        var configured = Environment.GetEnvironmentVariable("PRODUCTMANAGEMENT_TEST_SQLSERVER")
                         ?? Environment.GetEnvironmentVariable("PRODUCTMANAGEMENT_TEST_CONNECTION")
                         ?? DefaultBaseConnectionString;
        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = Environment.GetEnvironmentVariable("PRODUCTMANAGEMENT_TEST_DATABASE")
                             ?? $"ProductManagement_Test_{Guid.NewGuid():N}"
        };

        DatabaseName = builder.InitialCatalog;
        if (IsForbiddenDatabaseName(DatabaseName))
            throw new InvalidOperationException($"Refusing to use '{DatabaseName}' as the integration test database.");

        ConnectionString = builder.ConnectionString;

        builder.InitialCatalog = "master";
        _masterConnectionString = builder.ConnectionString;
    }

    public string DatabaseName { get; }
    public string ConnectionString { get; }

    public async Task InitializeAsync()
    {
        await using var master = new SqlConnection(_masterConnectionString);
        await master.OpenAsync();

        await ExecuteNonQueryAsync(master, $"""
            IF DB_ID(N'{EscapeSqlLiteral(DatabaseName)}') IS NULL
            BEGIN
                CREATE DATABASE {QuoteIdentifier(DatabaseName)};
            END;

            ALTER DATABASE {QuoteIdentifier(DatabaseName)} SET RECOVERY SIMPLE;
            """);
        _createdDatabase = true;

        await ExecuteScriptFileAsync("02_CreateTables.sql");
        await ExecuteScriptFileAsync("03_CreateIndexes.sql");
    }

    public async Task DisposeAsync()
    {
        if (!_createdDatabase) return;

        await using var master = new SqlConnection(_masterConnectionString);
        await master.OpenAsync();
        await ExecuteNonQueryAsync(master, $"""
            IF DB_ID(N'{EscapeSqlLiteral(DatabaseName)}') IS NOT NULL
            BEGIN
                ALTER DATABASE {QuoteIdentifier(DatabaseName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {QuoteIdentifier(DatabaseName)};
            END;
            """);
    }

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await ExecuteNonQueryAsync(connection, """
            DELETE FROM dbo.InventoryTransactions;
            DELETE FROM dbo.AuditLogs;
            DELETE FROM dbo.Products;
            DELETE FROM dbo.Categories;
            DELETE FROM dbo.AspNetUserRoles;
            DELETE FROM dbo.AspNetUsers WHERE Id = N'test-admin-id';
            DELETE FROM dbo.AspNetRoles WHERE Id IN (N'ADMIN', N'STAFF');
            INSERT dbo.AspNetRoles(Id, Name, NormalizedName, ConcurrencyStamp)
            VALUES (N'ADMIN', N'ADMIN', N'ADMIN', NEWID()), (N'STAFF', N'STAFF', N'STAFF', NEWID());
            INSERT dbo.AspNetUsers
                (Id, FullName, IsActive, CreatedAt, UserName, NormalizedUserName, EmailConfirmed,
                 PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES
                (N'test-admin-id', N'Integration Test Admin', 1, SYSUTCDATETIME(), N'admin', N'ADMIN', 0,
                 0, 0, 0, 0);
            INSERT dbo.AspNetUserRoles(UserId, RoleId)
            VALUES (N'test-admin-id', N'ADMIN');
            DBCC CHECKIDENT ('dbo.InventoryTransactions', RESEED, 0) WITH NO_INFOMSGS;
            DBCC CHECKIDENT ('dbo.AuditLogs', RESEED, 0) WITH NO_INFOMSGS;
            DBCC CHECKIDENT ('dbo.Products', RESEED, 0) WITH NO_INFOMSGS;
            DBCC CHECKIDENT ('dbo.Categories', RESEED, 0) WITH NO_INFOMSGS;
            """);
    }

    private async Task ExecuteScriptFileAsync(string fileName)
    {
        var path = FindRepositoryRoot().Combine("database", fileName);
        var script = await File.ReadAllTextAsync(path);
        script = script.Replace("USE [ProductManagementDB];", $"USE {QuoteIdentifier(DatabaseName)};", StringComparison.OrdinalIgnoreCase);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        foreach (var batch in SplitSqlBatches(script))
        {
            if (!string.IsNullOrWhiteSpace(batch))
                await ExecuteNonQueryAsync(connection, batch);
        }
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        using var reader = new StringReader(script);
        var batch = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return string.Join(Environment.NewLine, batch);
                batch.Clear();
                continue;
            }

            batch.Add(line);
        }

        if (batch.Count > 0)
            yield return string.Join(Environment.NewLine, batch);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ProductManagement.sln")))
            directory = directory.Parent;

        return directory ?? throw new InvalidOperationException("Cannot locate ProductManagement.sln.");
    }

    private static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static bool IsForbiddenDatabaseName(string value)
    {
        var forbidden = new[] { "master", "model", "msdb", "tempdb", "ProductManagementDB" };
        return forbidden.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}

internal static class DirectoryInfoExtensions
{
    public static string Combine(this DirectoryInfo directory, params string[] paths)
        => Path.Combine(new[] { directory.FullName }.Concat(paths).ToArray());
}
