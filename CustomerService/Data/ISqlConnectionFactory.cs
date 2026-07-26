using System.Data;

namespace CustomerService.Data;

/// <summary>
/// Pembuat koneksi database. Dibuat sebagai interface supaya
/// Repository tidak perlu tahu dari mana connection string berasal
/// dan mudah diganti/di-mock saat unit test.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Membuat koneksi baru (belum terbuka). Pemanggil bertanggung jawab
    /// menutup koneksi — biasanya lewat blok using.
    /// </summary>
    IDbConnection CreateConnection();
}
