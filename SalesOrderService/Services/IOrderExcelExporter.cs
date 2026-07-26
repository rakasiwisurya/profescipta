using SalesOrderService.Models;

namespace SalesOrderService.Services;

/// <summary>
/// Pembuat file Excel untuk data Order List.
/// Dipisah dari OrderService supaya logika bisnis tidak terikat pada
/// library Excel tertentu (mudah diganti kalau formatnya berubah).
/// </summary>
public interface IOrderExcelExporter
{
    /// <summary>Mengembalikan isi file .xlsx sebagai byte array.</summary>
    byte[] Export(IReadOnlyList<OrderListItemDto> orders);
}
