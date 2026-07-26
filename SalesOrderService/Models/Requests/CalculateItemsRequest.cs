namespace SalesOrderService.Models.Requests;

/// <summary>
/// Body request untuk POST /api/orders/calculate.
///
/// Endpoint ini adalah endpoint tambahan (diizinkan FSD bagian 6) yang
/// dipakai tombol ✓ pada baris item: front-end mengirim seluruh baris
/// item yang sedang ada di form, service memvalidasi tiap baris,
/// menghitung TOTAL per baris dan Grand Total, lalu mengembalikannya.
///
/// Dengan cara ini front-end sama sekali tidak melakukan kalkulasi —
/// angka yang ditampilkan murni hasil respons service.
/// </summary>
public class CalculateItemsRequest
{
    public List<SaveOrderItemRequest> Items { get; set; } = new();
}
