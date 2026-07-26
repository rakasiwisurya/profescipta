namespace SalesOrderService.Models;

/// <summary>
/// Satu baris pada grid Order List.
/// Field mengikuti kontrak FSD 6.2 (GET /api/orders).
/// </summary>
public class OrderListItemDto
{
    public int SalesSoId { get; set; }

    public string SoNo { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? Address { get; set; }

    /// <summary>Total keseluruhan order, dihitung di database/service.</summary>
    public decimal GrandTotal { get; set; }
}
