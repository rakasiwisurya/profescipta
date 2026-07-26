namespace SalesOrderService.Models;

/// <summary>
/// Detail satu order beserta seluruh item-nya.
/// Dipakai oleh GET /api/orders/{id} (FSD 6.2) untuk mengisi
/// halaman Order Input mode Edit.
/// </summary>
public class OrderDetailDto
{
    public int SalesSoId { get; set; }

    public string SoNo { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public decimal GrandTotal { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}
