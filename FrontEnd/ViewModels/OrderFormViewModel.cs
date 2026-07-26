using FrontEnd.Models;

namespace FrontEnd.ViewModels;

/// <summary>
/// Data untuk halaman Order Input. Dipakai bersama oleh mode Create dan
/// mode Edit karena layout dan komponennya sama (FSD bagian 4.3) —
/// perbedaannya hanya beberapa flag di view model ini.
/// </summary>
public class OrderFormViewModel
{
    /// <summary>True = mode Edit, False = mode Create.</summary>
    public bool IsEditMode { get; set; }

    public int SalesSoId { get; set; }

    // ---- Bagian header ----
    public string? SoNo { get; set; }

    public DateTime? OrderDate { get; set; }

    public int? CustomerId { get; set; }

    public string? Address { get; set; }

    // ---- Bagian detail item ----
    /// <summary>
    /// Item yang sedang ada di form. Nilai Total tiap baris dan
    /// GrandTotal SELALU hasil kalkulasi service, bukan JavaScript.
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();

    public decimal GrandTotal { get; set; }

    // ---- Data pendukung tampilan ----
    /// <summary>Isi dropdown Customer, diambil dari Customer Service.</summary>
    public IReadOnlyList<CustomerDto> Customers { get; set; } = Array.Empty<CustomerDto>();

    /// <summary>Pesan error dari service yang ditampilkan di atas form.</summary>
    public string? ErrorMessage { get; set; }

    public IReadOnlyList<string> ErrorDetails { get; set; } = Array.Empty<string>();

    public string PageTitle => IsEditMode ? "Edit Sales Order" : "Create Sales Order";
}
