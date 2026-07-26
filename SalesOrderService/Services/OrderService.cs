using SalesOrderService.Common;
using SalesOrderService.Domain;
using SalesOrderService.Models;
using SalesOrderService.Models.Requests;
using SalesOrderService.Models.Responses;
using SalesOrderService.Repositories;

namespace SalesOrderService.Services;

/// <summary>
/// Implementasi logika bisnis Sales Order.
///
/// Alur umum tiap operasi tulis:
///   1. Validasi field (OrderValidator)
///   2. Validasi yang butuh database (duplikat SO_NO)
///   3. Simpan lewat Repository (di dalam transaksi)
/// Kalau langkah 1 atau 2 gagal, database tidak disentuh sama sekali.
/// </summary>
public class OrderService : IOrderService
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly OrderValidator _orderValidator;
    private readonly OrderCalculator _orderCalculator;
    private readonly IOrderExcelExporter _excelExporter;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ISalesOrderRepository salesOrderRepository,
        OrderValidator orderValidator,
        OrderCalculator orderCalculator,
        IOrderExcelExporter excelExporter,
        ILogger<OrderService> logger)
    {
        _salesOrderRepository = salesOrderRepository;
        _orderValidator = orderValidator;
        _orderCalculator = orderCalculator;
        _excelExporter = excelExporter;
        _logger = logger;
    }

    public Task<IReadOnlyList<OrderListItemDto>> SearchOrdersAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        return _salesOrderRepository.SearchAsync(keyword, orderDate, cancellationToken);
    }

    public async Task<ServiceResult<OrderDetailDto>> GetOrderByIdAsync(
        int salesSoId, CancellationToken cancellationToken)
    {
        OrderDetailDto? order = await _salesOrderRepository.GetByIdAsync(salesSoId, cancellationToken);

        if (order is null)
        {
            return ServiceResult<OrderDetailDto>.NotFound("Order tidak ditemukan");
        }

        return ServiceResult<OrderDetailDto>.Success(order, "Order ditemukan");
    }

    public async Task<ServiceResult<int>> CreateOrderAsync(
        SaveOrderRequest request, CancellationToken cancellationToken)
    {
        // 1) Validasi field header + semua item.
        List<string> errors = _orderValidator.ValidateOrder(request).ToList();

        // 2) Validasi duplikat SO_NO (butuh database). Hanya dicek kalau
        //    nomor order-nya sendiri sudah lolos validasi "tidak kosong".
        if (!string.IsNullOrWhiteSpace(request.SoNo)
            && await _salesOrderRepository.SoNoExistsAsync(request.SoNo.Trim(), null, cancellationToken))
        {
            errors.Add(OrderValidator.ErrorSoNoDuplicate);
        }

        if (errors.Count > 0)
        {
            return ServiceResult<int>.ValidationFailed("Data order tidak valid", errors);
        }

        int newSalesSoId = await _salesOrderRepository.InsertOrderAsync(request, cancellationToken);

        _logger.LogInformation(
            "Order {SoNo} tersimpan dengan ID {SalesSoId} ({ItemCount} item).",
            request.SoNo, newSalesSoId, request.Items.Count);

        return ServiceResult<int>.Success(newSalesSoId, "Order berhasil dibuat");
    }

    public async Task<ServiceResult> UpdateOrderAsync(
        int salesSoId, SaveOrderRequest request, CancellationToken cancellationToken)
    {
        // Pastikan order-nya ada dulu, supaya bisa membedakan
        // 404 (tidak ditemukan) dari 400 (data tidak valid).
        OrderDetailDto? existingOrder =
            await _salesOrderRepository.GetByIdAsync(salesSoId, cancellationToken);

        if (existingOrder is null)
        {
            return ServiceResult.NotFound("Order tidak ditemukan");
        }

        List<string> errors = _orderValidator.ValidateOrder(request).ToList();

        // Pada mode Edit, order yang sedang diedit dikecualikan dari
        // pengecekan duplikat (tidak duplikat terhadap dirinya sendiri).
        if (!string.IsNullOrWhiteSpace(request.SoNo)
            && await _salesOrderRepository.SoNoExistsAsync(request.SoNo.Trim(), salesSoId, cancellationToken))
        {
            errors.Add(OrderValidator.ErrorSoNoDuplicate);
        }

        if (errors.Count > 0)
        {
            return ServiceResult.ValidationFailed("Data order tidak valid", errors);
        }

        bool updated = await _salesOrderRepository.UpdateOrderAsync(salesSoId, request, cancellationToken);

        if (!updated)
        {
            // Bisa terjadi kalau order dihapus pengguna lain di sela-sela proses.
            return ServiceResult.NotFound("Order tidak ditemukan");
        }

        _logger.LogInformation("Order ID {SalesSoId} diperbarui ({ItemCount} item).",
            salesSoId, request.Items.Count);

        return ServiceResult.Success("Order berhasil diperbarui");
    }

    public async Task<ServiceResult> DeleteOrderAsync(int salesSoId, CancellationToken cancellationToken)
    {
        bool deleted = await _salesOrderRepository.DeleteOrderAsync(salesSoId, cancellationToken);

        if (!deleted)
        {
            return ServiceResult.NotFound("Order tidak ditemukan");
        }

        _logger.LogInformation("Order ID {SalesSoId} dihapus beserta seluruh item-nya.", salesSoId);

        return ServiceResult.Success("Order berhasil dihapus");
    }

    public CalculateItemsResponse CalculateItems(CalculateItemsRequest request)
    {
        var calculatedItems = new List<CalculatedItemDto>();

        for (int index = 0; index < request.Items.Count; index++)
        {
            SaveOrderItemRequest item = request.Items[index];

            IReadOnlyList<string> itemErrors = _orderValidator.ValidateItem(item);
            bool isValid = itemErrors.Count == 0;

            int quantity = item.Quantity ?? 0;
            decimal price = item.Price ?? 0m;

            calculatedItems.Add(new CalculatedItemDto
            {
                RowIndex = index,
                ItemName = item.ItemName?.Trim() ?? string.Empty,
                Quantity = quantity,
                Price = price,

                // Baris tidak valid TOTAL-nya 0 supaya tidak ikut
                // mempengaruhi Grand Total.
                Total = isValid ? _orderCalculator.CalculateLineTotal(quantity, price) : 0m,
                IsValid = isValid,
                Errors = itemErrors
            });
        }

        // Grand Total = penjumlahan TOTAL baris-baris yang valid,
        // dihitung di service (FSD melarang kalkulasi di front-end).
        decimal grandTotal = _orderCalculator.SumLineTotals(
            calculatedItems.Where(item => item.IsValid).Select(item => item.Total));

        bool allValid = calculatedItems.All(item => item.IsValid);

        return new CalculateItemsResponse
        {
            Success = allValid,
            Message = allValid ? "Kalkulasi berhasil" : "Ada baris item yang tidak valid",
            Items = calculatedItems,
            GrandTotal = grandTotal
        };
    }

    public async Task<byte[]> ExportOrdersToExcelAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        // Ekspor memakai filter yang sama dengan grid, sesuai FSD 5.5:
        // yang diekspor adalah data yang sedang tampil, bukan semua data.
        IReadOnlyList<OrderListItemDto> orders =
            await _salesOrderRepository.SearchAsync(keyword, orderDate, cancellationToken);

        _logger.LogInformation("Mengekspor {Count} order ke Excel.", orders.Count);

        return _excelExporter.Export(orders);
    }
}
