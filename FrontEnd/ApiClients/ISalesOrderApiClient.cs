using FrontEnd.Models;

namespace FrontEnd.ApiClients;

/// <summary>
/// Jembatan front-end ke Sales Order Service (HTTP REST).
/// Semua aksi pada halaman Order List dan Order Input melewati sini.
/// </summary>
public interface ISalesOrderApiClient
{
    Task<ApiResult<List<OrderListItemDto>>> SearchOrdersAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken);

    Task<ApiResult<OrderDetailDto>> GetOrderAsync(int salesSoId, CancellationToken cancellationToken);

    Task<ApiResult> CreateOrderAsync(SaveOrderRequest request, CancellationToken cancellationToken);

    Task<ApiResult> UpdateOrderAsync(
        int salesSoId, SaveOrderRequest request, CancellationToken cancellationToken);

    Task<ApiResult> DeleteOrderAsync(int salesSoId, CancellationToken cancellationToken);

    /// <summary>Meminta service menghitung TOTAL per baris dan Grand Total.</summary>
    Task<ApiResult<CalculateItemsResponse>> CalculateItemsAsync(
        CalculateItemsRequest request, CancellationToken cancellationToken);

    /// <summary>Mengambil file Excel hasil ekspor (mengikuti filter grid).</summary>
    Task<ApiResult<ExportedFile>> ExportOrdersAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken);
}

/// <summary>File hasil ekspor yang diteruskan front-end ke browser.</summary>
public class ExportedFile
{
    public byte[] Content { get; init; } = Array.Empty<byte>();

    public string FileName { get; init; } = "SalesOrder.xlsx";

    public string ContentType { get; init; } =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
