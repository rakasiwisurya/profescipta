using SalesOrderService.Models;
using SalesOrderService.Models.Requests;

namespace SalesOrderService.Repositories;

/// <summary>
/// Lapisan akses data untuk tabel SALES_SO dan SALES_SO_LITEM.
/// Semua perintah SQL dijalankan lewat Stored Procedure
/// (script ada di folder /Database).
/// </summary>
public interface ISalesOrderRepository
{
    /// <summary>Daftar order dengan filter opsional keyword dan tanggal.</summary>
    Task<IReadOnlyList<OrderListItemDto>> SearchAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken);

    /// <summary>Detail satu order beserta item; null jika tidak ditemukan.</summary>
    Task<OrderDetailDto?> GetByIdAsync(int salesSoId, CancellationToken cancellationToken);

    /// <summary>
    /// True jika SO_NO sudah dipakai order lain.
    /// <paramref name="excludeSalesSoId"/> diisi saat mode Edit agar order
    /// yang sedang diedit tidak dianggap duplikat terhadap dirinya sendiri.
    /// </summary>
    Task<bool> SoNoExistsAsync(
        string soNo, int? excludeSalesSoId, CancellationToken cancellationToken);

    /// <summary>Menyimpan header + seluruh item dalam satu transaksi. Mengembalikan ID order baru.</summary>
    Task<int> InsertOrderAsync(SaveOrderRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Memperbarui header lalu mengganti SELURUH item lama dengan item baru
    /// (replace all), semuanya dalam satu transaksi.
    /// False jika order tidak ditemukan.
    /// </summary>
    Task<bool> UpdateOrderAsync(
        int salesSoId, SaveOrderRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Menghapus order beserta seluruh item-nya dalam satu transaksi (atomik).
    /// False jika order tidak ditemukan.
    /// </summary>
    Task<bool> DeleteOrderAsync(int salesSoId, CancellationToken cancellationToken);
}
