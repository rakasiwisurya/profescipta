using FrontEnd.Models;

namespace FrontEnd.ViewModels;

/// <summary>
/// Data yang dibutuhkan halaman Order List: hasil pencarian, nilai filter
/// yang sedang aktif, dan informasi paging.
/// </summary>
public class OrderListViewModel
{
    /// <summary>Order yang tampil di halaman ini saja (hasil paging).</summary>
    public IReadOnlyList<OrderListItemDto> Orders { get; init; } = Array.Empty<OrderListItemDto>();

    // ---- Nilai filter yang sedang aktif (dikembalikan ke form pencarian) ----
    public string? Keyword { get; init; }

    public DateTime? OrderDate { get; init; }

    // ---- Informasi paging ----
    public int CurrentPage { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    /// <summary>Jumlah seluruh order yang lolos filter (sebelum dipotong per halaman).</summary>
    public int TotalOrders { get; init; }

    public int TotalPages => TotalOrders == 0
        ? 1
        : (int)Math.Ceiling(TotalOrders / (double)PageSize);

    /// <summary>Nomor urut baris pertama di halaman ini (untuk kolom "#").</summary>
    public int FirstRowNumber => ((CurrentPage - 1) * PageSize) + 1;

    /// <summary>Pesan error dari service, kalau pemanggilan API gagal.</summary>
    public string? ErrorMessage { get; init; }
}
