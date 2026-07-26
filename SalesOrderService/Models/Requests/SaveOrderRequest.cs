namespace SalesOrderService.Models.Requests;

/// <summary>
/// Body request untuk POST /api/orders (create) dan
/// PUT /api/orders/{id} (update) — FSD 6.2.
///
/// Semua properti sengaja dibuat nullable / tanpa atribut validasi
/// bawaan supaya validasi dijalankan oleh OrderValidator (logika bisnis
/// terpusat di service, dengan pesan error persis seperti FSD bagian 5),
/// bukan oleh model binder.
/// </summary>
public class SaveOrderRequest
{
    public string? SoNo { get; set; }

    public DateTime? OrderDate { get; set; }

    public int? CustomerId { get; set; }

    public string? Address { get; set; }

    public List<SaveOrderItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Satu baris item yang dikirim front-end saat Save Order.
/// Front-end hanya mengirim data mentah (nama, qty, harga);
/// TOTAL dihitung ulang oleh service.
/// </summary>
public class SaveOrderItemRequest
{
    public string? ItemName { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }
}
