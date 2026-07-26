using SalesOrderService.Models.Requests;

namespace SalesOrderService.Domain;

/// <summary>
/// Semua kalkulasi uang milik Sales Order ada di sini (FSD bagian 5.3).
///
/// Kelas ini sengaja dibuat murni (tidak menyentuh database, HTTP,
/// maupun konfigurasi) supaya:
///   1. Front-end tidak perlu — dan tidak boleh — menghitung apa pun.
///   2. Logikanya bisa diuji dengan unit test tanpa database.
/// </summary>
public class OrderCalculator
{
    /// <summary>
    /// Pembulatan 2 desimal dipakai konsisten di semua hasil kalkulasi.
    /// PRICE di database bertipe FLOAT (bukan DECIMAL) sesuai FSD, jadi
    /// nilainya dikonversi ke decimal dulu sebelum dihitung supaya tidak
    /// terkena galat pembulatan floating point.
    /// </summary>
    private const int MoneyDecimals = 2;

    /// <summary>
    /// TOTAL per baris item = QUANTITY x PRICE (FSD bagian 5.3).
    /// </summary>
    public decimal CalculateLineTotal(int quantity, decimal price)
    {
        decimal total = quantity * price;

        return Math.Round(total, MoneyDecimals, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Grand Total order = jumlah TOTAL seluruh baris (FSD bagian 5.3).
    /// Baris dengan qty/price tidak valid (null) dilewati; validasi
    /// nilainya sendiri ditangani <see cref="OrderValidator"/>.
    /// </summary>
    public decimal CalculateGrandTotal(IEnumerable<SaveOrderItemRequest> items)
    {
        decimal grandTotal = 0m;

        foreach (SaveOrderItemRequest item in items)
        {
            if (item.Quantity is null || item.Price is null)
            {
                continue;
            }

            grandTotal += CalculateLineTotal(item.Quantity.Value, item.Price.Value);
        }

        return Math.Round(grandTotal, MoneyDecimals, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Menjumlahkan TOTAL yang sudah dihitung sebelumnya.
    /// Dipakai saat menyusun respons kalkulasi agar Grand Total dijamin
    /// sama dengan penjumlahan angka yang tampil di tiap baris.
    /// </summary>
    public decimal SumLineTotals(IEnumerable<decimal> lineTotals)
    {
        decimal grandTotal = lineTotals.Sum();

        return Math.Round(grandTotal, MoneyDecimals, MidpointRounding.AwayFromZero);
    }
}
