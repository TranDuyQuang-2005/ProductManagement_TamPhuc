using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ProductManagement.Api.Data;

public sealed class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);
}
