using System.Data;
using Microsoft.Data.SqlClient;

namespace CustomerService.Data;

/// <summary>
/// Implementasi factory koneksi untuk SQL Server.
/// Connection string dibaca dari konfigurasi (appsettings.json /
/// environment variable) — tidak pernah di-hardcode di dalam kode.
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        // Throw saat startup kalau konfigurasi lupa diisi, supaya
        // kesalahan konfigurasi ketahuan lebih awal (fail fast).
        _connectionString = configuration.GetConnectionString("SalesOrderDb")
            ?? throw new InvalidOperationException(
                "Connection string 'SalesOrderDb' belum diatur di appsettings.json " +
                "atau environment variable ConnectionStrings__SalesOrderDb.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
