using System.Data.Common;

namespace SalesOrderService.Data;

/// <summary>
/// Pembuat koneksi database.
///
/// Tipe kembaliannya <see cref="DbConnection"/> (bukan IDbConnection)
/// karena Repository butuh versi async-nya: OpenAsync dan
/// BeginTransactionAsync untuk operasi yang harus atomik.
/// </summary>
public interface ISqlConnectionFactory
{
    DbConnection CreateConnection();
}
