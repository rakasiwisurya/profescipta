using SalesOrderService.Domain;
using SalesOrderService.Models.Requests;
using Xunit;

namespace SalesOrderService.Tests;

/// <summary>
/// Unit test untuk kalkulasi uang (FSD bagian 5.3).
/// Tidak butuh database maupun service yang berjalan karena
/// OrderCalculator adalah kelas murni.
/// </summary>
public class OrderCalculatorTests
{
    private readonly OrderCalculator _calculator = new();

    [Theory]
    [InlineData(2, 15000000, 30000000)]   // contoh pada wireframe FSD
    [InlineData(1, 499000.50, 499000.50)]
    [InlineData(3, 350000, 1050000)]
    [InlineData(10, 55000, 550000)]
    public void CalculateLineTotal_MengalikanQuantityDenganPrice(
        int quantity, decimal price, decimal expectedTotal)
    {
        decimal total = _calculator.CalculateLineTotal(quantity, price);

        Assert.Equal(expectedTotal, total);
    }

    [Fact]
    public void CalculateLineTotal_MembulatkanKeDuaDesimal()
    {
        // 3 x 1000.005 = 3000.015 -> dibulatkan menjadi 3000.02
        decimal total = _calculator.CalculateLineTotal(3, 1000.005m);

        Assert.Equal(3000.02m, total);
    }

    [Fact]
    public void CalculateGrandTotal_MenjumlahkanSeluruhBarisItem()
    {
        var items = new List<SaveOrderItemRequest>
        {
            new() { ItemName = "Laptop Dell XPS 13",      Quantity = 2,  Price = 15000000m },
            new() { ItemName = "Mouse Wireless Logitech", Quantity = 3,  Price = 350000m },
            new() { ItemName = "Kertas A4 80gsm",         Quantity = 10, Price = 55000m }
        };

        decimal grandTotal = _calculator.CalculateGrandTotal(items);

        // 30.000.000 + 1.050.000 + 550.000
        Assert.Equal(31600000m, grandTotal);
    }

    [Fact]
    public void CalculateGrandTotal_TanpaItem_MengembalikanNol()
    {
        decimal grandTotal = _calculator.CalculateGrandTotal(new List<SaveOrderItemRequest>());

        Assert.Equal(0m, grandTotal);
    }

    [Fact]
    public void CalculateGrandTotal_MelewatiBarisDenganQuantityAtauPriceKosong()
    {
        var items = new List<SaveOrderItemRequest>
        {
            new() { ItemName = "Barang valid",     Quantity = 2, Price = 100000m },
            new() { ItemName = "Qty belum diisi",  Quantity = null, Price = 100000m },
            new() { ItemName = "Harga belum diisi", Quantity = 5, Price = null }
        };

        decimal grandTotal = _calculator.CalculateGrandTotal(items);

        // Hanya baris pertama yang dihitung: 2 x 100.000
        Assert.Equal(200000m, grandTotal);
    }

    [Fact]
    public void SumLineTotals_MenjumlahkanTotalYangSudahDihitung()
    {
        decimal grandTotal = _calculator.SumLineTotals(new[] { 30000000m, 1050000m, 499000.50m });

        Assert.Equal(31549000.50m, grandTotal);
    }
}
