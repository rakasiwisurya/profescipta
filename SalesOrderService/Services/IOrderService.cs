using SalesOrderService.Common;
using SalesOrderService.Models;
using SalesOrderService.Models.Requests;
using SalesOrderService.Models.Responses;

namespace SalesOrderService.Services;

/// <summary>
/// Logika bisnis Sales Order: validasi, kalkulasi, dan orkestrasi
/// pemanggilan Repository. Controller tidak berisi aturan bisnis apa pun,
/// hanya menerjemahkan hasil di sini menjadi HTTP response.
/// </summary>
public interface IOrderService
{
    Task<IReadOnlyList<OrderListItemDto>> SearchOrdersAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken);

    Task<ServiceResult<OrderDetailDto>> GetOrderByIdAsync(
        int salesSoId, CancellationToken cancellationToken);

    Task<ServiceResult<int>> CreateOrderAsync(
        SaveOrderRequest request, CancellationToken cancellationToken);

    Task<ServiceResult> UpdateOrderAsync(
        int salesSoId, SaveOrderRequest request, CancellationToken cancellationToken);

    Task<ServiceResult> DeleteOrderAsync(int salesSoId, CancellationToken cancellationToken);

    /// <summary>
    /// Menghitung TOTAL per baris dan Grand Total untuk form Order Input,
    /// sekaligus memvalidasi tiap baris (dipakai tombol ✓ per baris).
    /// </summary>
    CalculateItemsResponse CalculateItems(CalculateItemsRequest request);

    /// <summary>
    /// Menghasilkan file Excel (.xlsx) dari data order yang lolos filter
    /// keyword/tanggal — yakni data yang sedang tampil di grid.
    /// </summary>
    Task<byte[]> ExportOrdersToExcelAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken);
}
