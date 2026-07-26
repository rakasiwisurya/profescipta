namespace SalesOrderService.Models;

/// <summary>
/// Satu baris item order yang dikirim ke pemanggil API.
/// Total SELALU berasal dari perhitungan service/database
/// (FSD melarang kalkulasi di front-end).
/// </summary>
public class OrderItemDto
{
    public int SalesSoLitemId { get; set; }

    public int SalesSoId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    /// <summary>TOTAL = QUANTITY x PRICE.</summary>
    public decimal Total { get; set; }
}
