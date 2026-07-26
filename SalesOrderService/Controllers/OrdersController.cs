using Microsoft.AspNetCore.Mvc;
using SalesOrderService.Common;
using SalesOrderService.Models;
using SalesOrderService.Models.Requests;
using SalesOrderService.Models.Responses;
using SalesOrderService.Security;
using SalesOrderService.Services;

namespace SalesOrderService.Controllers;

/// <summary>
/// Endpoint publik Sales Order Service (kontrak FSD bagian 6.2).
///
/// Controller hanya bertugas:
///   - menerima request dan meneruskannya ke IOrderService
///   - menerjemahkan ServiceResult menjadi HTTP status code
/// Tidak ada aturan bisnis maupun kalkulasi di lapisan ini.
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// GET /api/orders?keyword=&amp;orderDate=YYYY-MM-DD
    /// Daftar order untuk grid Order List. Kedua filter opsional;
    /// kalau dua-duanya kosong, semua data dikembalikan.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderListItemDto>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] DateTime? orderDate,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OrderListItemDto> orders =
            await _orderService.SearchOrdersAsync(keyword, orderDate, cancellationToken);

        return Ok(orders);
    }

    /// <summary>
    /// GET /api/orders/{id} — detail satu order beserta seluruh item-nya
    /// (dipakai halaman Order Input mode Edit).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        ServiceResult<OrderDetailDto> result = await _orderService.GetOrderByIdAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(ApiResponse.Fail(result.Message));
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// POST /api/orders — membuat order baru beserta item-nya.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] SaveOrderRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<int> result = await _orderService.CreateOrderAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        var response = new CreateOrderResponse
        {
            Success = true,
            SalesSoId = result.Data,
            Message = result.Message
        };

        // 201 Created + header Location ke detail order yang baru dibuat.
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, response);
    }

    /// <summary>
    /// PUT /api/orders/{id} — memperbarui order; seluruh item lama
    /// digantikan item yang dikirim (replace all).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveOrderRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult result = await _orderService.UpdateOrderAsync(id, request, cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorType == ServiceErrorType.NotFound
                ? NotFound(ApiResponse.Fail(result.Message))
                : BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        return Ok(ApiResponse.Ok(result.Message));
    }

    /// <summary>
    /// DELETE /api/orders/{id} — menghapus order beserta seluruh item-nya
    /// dalam satu transaksi.
    ///
    /// Diamankan dengan API Key: pemanggil wajib mengirim header X-Api-Key.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ApiKeyAuthorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        ServiceResult result = await _orderService.DeleteOrderAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(ApiResponse.Fail(result.Message));
        }

        return Ok(ApiResponse.Ok(result.Message));
    }

    /// <summary>
    /// POST /api/orders/calculate — menghitung TOTAL per baris dan
    /// Grand Total untuk form Order Input (endpoint tambahan).
    ///
    /// Selalu membalas 200: front-end memakai flag isValid tiap baris
    /// untuk menampilkan pesan error inline, dan memakai angka dari
    /// respons ini untuk mengisi kolom Total dan Grand Total.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(CalculateItemsResponse), StatusCodes.Status200OK)]
    public ActionResult<CalculateItemsResponse> Calculate([FromBody] CalculateItemsRequest request)
    {
        CalculateItemsResponse response = _orderService.CalculateItems(request);

        return Ok(response);
    }

    /// <summary>
    /// GET /api/orders/export?keyword=&amp;orderDate=YYYY-MM-DD
    /// Mengunduh data order (sesuai filter aktif) sebagai file .xlsx.
    ///
    /// Diamankan dengan API Key (header X-Api-Key).
    /// </summary>
    [HttpGet("export")]
    [ApiKeyAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Export(
        [FromQuery] string? keyword,
        [FromQuery] DateTime? orderDate,
        CancellationToken cancellationToken)
    {
        byte[] fileContent =
            await _orderService.ExportOrdersToExcelAsync(keyword, orderDate, cancellationToken);

        string fileName = $"SalesOrder_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            fileContent,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
