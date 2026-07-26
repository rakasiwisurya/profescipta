using SalesOrderService.Domain;
using SalesOrderService.Models.Requests;
using Xunit;

namespace SalesOrderService.Tests;

/// <summary>
/// Unit test untuk aturan validasi order (FSD bagian 5.1 dan 5.2).
/// Yang diuji bukan hanya "gagal/berhasil", tapi juga pesan errornya
/// supaya tetap sama dengan yang tertulis di FSD.
/// </summary>
public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    /// <summary>Order yang valid, dipakai sebagai titik awal tiap test.</summary>
    private static SaveOrderRequest CreateValidOrder() => new()
    {
        SoNo = "SO-2026-001",
        OrderDate = new DateTime(2026, 1, 15),
        CustomerId = 1,
        Address = "Jl. Sudirman No. 1, Jakarta",
        Items = new List<SaveOrderItemRequest>
        {
            new() { ItemName = "Laptop Dell XPS 13", Quantity = 2, Price = 15000000m }
        }
    };

    [Fact]
    public void ValidateOrder_OrderValid_TidakAdaError()
    {
        IReadOnlyList<string> errors = _validator.ValidateOrder(CreateValidOrder());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateOrder_AddressKosong_TetapValidKarenaOpsional()
    {
        SaveOrderRequest order = CreateValidOrder();
        order.Address = null;

        IReadOnlyList<string> errors = _validator.ValidateOrder(order);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOrder_SoNoKosong_MemberiPesanSesuaiFsd(string? soNo)
    {
        SaveOrderRequest order = CreateValidOrder();
        order.SoNo = soNo;

        IReadOnlyList<string> errors = _validator.ValidateOrder(order);

        Assert.Contains(OrderValidator.ErrorSoNoRequired, errors);
    }

    [Fact]
    public void ValidateOrder_OrderDateKosong_MemberiPesanSesuaiFsd()
    {
        SaveOrderRequest order = CreateValidOrder();
        order.OrderDate = null;

        IReadOnlyList<string> errors = _validator.ValidateOrder(order);

        Assert.Contains(OrderValidator.ErrorOrderDateRequired, errors);
    }

    [Fact]
    public void ValidateOrder_CustomerBelumDipilih_MemberiPesanSesuaiFsd()
    {
        SaveOrderRequest order = CreateValidOrder();
        order.CustomerId = 0;

        IReadOnlyList<string> errors = _validator.ValidateOrder(order);

        Assert.Contains(OrderValidator.ErrorCustomerRequired, errors);
    }

    [Fact]
    public void ValidateOrder_TanpaItem_MemberiPesanMinimalSatuItem()
    {
        SaveOrderRequest order = CreateValidOrder();
        order.Items = new List<SaveOrderItemRequest>();

        IReadOnlyList<string> errors = _validator.ValidateOrder(order);

        Assert.Contains(OrderValidator.ErrorItemRequired, errors);
    }

    [Fact]
    public void ValidateOrder_ItemTidakValid_PesanMenyebutkanNomorBaris()
    {
        SaveOrderRequest order = CreateValidOrder();
        order.Items.Add(new SaveOrderItemRequest { ItemName = "", Quantity = 0, Price = 0m });

        IReadOnlyList<string> errors = _validator.ValidateOrder(order);

        // Baris ke-2 yang bermasalah, baris ke-1 tetap valid.
        Assert.Contains($"Item baris 2: {OrderValidator.ErrorItemNameRequired}", errors);
        Assert.Contains($"Item baris 2: {OrderValidator.ErrorQuantityInvalid}", errors);
        Assert.Contains($"Item baris 2: {OrderValidator.ErrorPriceInvalid}", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(null)]
    public void ValidateItem_QuantityTidakLebihDariNol_Ditolak(int? quantity)
    {
        var item = new SaveOrderItemRequest { ItemName = "Laptop", Quantity = quantity, Price = 100m };

        IReadOnlyList<string> errors = _validator.ValidateItem(item);

        Assert.Contains(OrderValidator.ErrorQuantityInvalid, errors);
    }

    // Literal ditulis dengan akhiran "d" supaya cocok dengan parameter
    // double? (xUnit tidak otomatis mengubah int menjadi double).
    [Theory]
    [InlineData(0d)]
    [InlineData(-5000d)]
    [InlineData(null)]
    public void ValidateItem_PriceTidakLebihDariNol_Ditolak(double? price)
    {
        var item = new SaveOrderItemRequest
        {
            ItemName = "Laptop",
            Quantity = 1,
            Price = price is null ? null : (decimal)price.Value
        };

        IReadOnlyList<string> errors = _validator.ValidateItem(item);

        Assert.Contains(OrderValidator.ErrorPriceInvalid, errors);
    }

    [Fact]
    public void ValidateItem_ItemValid_TidakAdaError()
    {
        var item = new SaveOrderItemRequest { ItemName = "Monitor LG 24 inch", Quantity = 4, Price = 1800000m };

        IReadOnlyList<string> errors = _validator.ValidateItem(item);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateHeader_SoNoLebihDari20Karakter_Ditolak()
    {
        SaveOrderRequest order = CreateValidOrder();
        order.SoNo = new string('X', 21);

        IReadOnlyList<string> errors = _validator.ValidateHeader(order);

        Assert.Contains("Order Number maksimal 20 karakter", errors);
    }
}
