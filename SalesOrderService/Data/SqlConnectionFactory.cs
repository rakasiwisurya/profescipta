using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SalesOrderService.Data;

/// <summary>
/// Implementasi factory koneksi untuk SQL Server.
/// Connection string dibaca dari konfigurasi (appsettings.json atau
/// environment variable ConnectionStrings__SalesOrderDb) — tidak
/// pernah di-hardcode di dalam kode.
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SalesOrderDb")
            ?? throw new InvalidOperationException(
                "Connection string 'SalesOrderDb' belum diatur di appsettings.json " +
                "atau environment variable ConnectionStrings__SalesOrderDb.");
    }

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);
}
