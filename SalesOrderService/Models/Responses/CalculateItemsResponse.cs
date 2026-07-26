namespace SalesOrderService.Models.Responses;

/// <summary>
/// Hasil kalkulasi baris item: dipakai front-end untuk menampilkan
/// kolom TOTAL per baris, Grand Total, dan pesan error inline.
/// </summary>
public class CalculateItemsResponse
{
    /// <summary>True jika SEMUA baris valid.</summary>
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<CalculatedItemDto> Items { get; set; } = new();

    /// <summary>Grand Total = jumlah TOTAL dari seluruh baris yang valid.</summary>
    public decimal GrandTotal { get; set; }
}

/// <summary>
/// Satu baris hasil kalkulasi, lengkap dengan status validasinya.
/// RowIndex dipakai front-end untuk mencocokkan hasil ke baris tabel
/// yang benar (urutan sama dengan urutan yang dikirim).
/// </summary>
public class CalculatedItemDto
{
    public int RowIndex { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal Total { get; set; }

    public bool IsValid { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
