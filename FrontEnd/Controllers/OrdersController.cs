using FrontEnd.ApiClients;
using FrontEnd.Models;
using FrontEnd.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Controllers;

/// <summary>
/// Controller halaman Order List dan Order Input.
///
/// Perannya murni lapisan tampilan (FSD bagian 7.2):
///   - mengambil data dari service lewat ApiClient
///   - menyiapkan view model untuk Razor view
///   - meneruskan aksi pengguna (simpan, hapus, ekspor) ke service
/// Tidak ada validasi bisnis maupun kalkulasi angka di sini.
/// </summary>
public class OrdersController : Controller
{
    private readonly ISalesOrderApiClient _salesOrderApi;
    private readonly ICustomerApiClient _customerApi;
    private readonly int _pageSize;

    public OrdersController(
        ISalesOrderApiClient salesOrderApi,
        ICustomerApiClient customerApi,
        IConfiguration configuration)
    {
        _salesOrderApi = salesOrderApi;
        _customerApi = customerApi;

        // Ukuran halaman grid diambil dari konfigurasi (default 10).
        _pageSize = configuration.GetValue<int?>("Ui:PageSize") ?? 10;
    }

    /// <summary>
    /// Halaman Order List: menampilkan hasil pencarian dari service.
    /// Kedua filter opsional; kalau kosong, semua data ditampilkan.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword, DateTime? orderDate, int page = 1, CancellationToken cancellationToken = default)
    {
        ApiResult<List<OrderListItemDto>> result =
            await _salesOrderApi.SearchOrdersAsync(keyword, orderDate, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return View(new OrderListViewModel
            {
                Keyword = keyword,
                OrderDate = orderDate,
                ErrorMessage = result.Message
            });
        }

        List<OrderListItemDto> allOrders = result.Data;

        // Paging dilakukan di sisi server front-end. Ini murni urusan
        // tampilan (memotong daftar), bukan kalkulasi data bisnis.
        int currentPage = page < 1 ? 1 : page;
        int totalPages = allOrders.Count == 0
            ? 1
            : (int)Math.Ceiling(allOrders.Count / (double)_pageSize);

        if (currentPage > totalPages)
        {
            currentPage = totalPages;
        }

        List<OrderListItemDto> pagedOrders = allOrders
            .Skip((currentPage - 1) * _pageSize)
            .Take(_pageSize)
            .ToList();

        return View(new OrderListViewModel
        {
            Orders = pagedOrders,
            Keyword = keyword,
            OrderDate = orderDate,
            CurrentPage = currentPage,
            PageSize = _pageSize,
            TotalOrders = allOrders.Count
        });
    }

    /// <summary>Halaman Order Input mode Create: form kosong.</summary>
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var viewModel = new OrderFormViewModel
        {
            IsEditMode = false,
            OrderDate = DateTime.Today
        };

        await LoadCustomersAsync(viewModel, cancellationToken);

        return View("OrderForm", viewModel);
    }

    /// <summary>
    /// Simpan order baru: meneruskan header + seluruh item ke
    /// POST /api/orders. Kalau service menolak, form ditampilkan
    /// kembali beserta pesan error dari service.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel form, CancellationToken cancellationToken)
    {
        ApiResult result = await _salesOrderApi.CreateOrderAsync(
            BuildSaveRequest(form), cancellationToken);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        form.IsEditMode = false;
        form.ErrorMessage = result.Message;
        form.ErrorDetails = result.Errors;

        await LoadCustomersAsync(form, cancellationToken);
        await RefreshTotalsFromServiceAsync(form, cancellationToken);

        return View("OrderForm", form);
    }

    /// <summary>
    /// Halaman Order Input mode Edit: form terisi data order terpilih
    /// beserta seluruh item-nya.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ApiResult<OrderDetailDto> result = await _salesOrderApi.GetOrderAsync(id, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        OrderDetailDto order = result.Data;

        var viewModel = new OrderFormViewModel
        {
            IsEditMode = true,
            SalesSoId = order.SalesSoId,
            SoNo = order.SoNo,
            OrderDate = order.OrderDate,
            CustomerId = order.CustomerId,
            Address = order.Address,
            Items = order.Items,

            // Grand Total diambil dari respons service, tidak dihitung ulang.
            GrandTotal = order.GrandTotal
        };

        await LoadCustomersAsync(viewModel, cancellationToken);

        return View("OrderForm", viewModel);
    }

    /// <summary>
    /// Simpan perubahan order: PUT /api/orders/{id}. Seluruh state item
    /// terkini dikirim, dan service yang mengganti item lama.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OrderFormViewModel form, CancellationToken cancellationToken)
    {
        ApiResult result = await _salesOrderApi.UpdateOrderAsync(
            form.SalesSoId, BuildSaveRequest(form), cancellationToken);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        if (result.NotFound)
        {
            TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        form.IsEditMode = true;
        form.ErrorMessage = result.Message;
        form.ErrorDetails = result.Errors;

        await LoadCustomersAsync(form, cancellationToken);
        await RefreshTotalsFromServiceAsync(form, cancellationToken);

        return View("OrderForm", form);
    }

    /// <summary>
    /// Hapus order (dipanggil dari popup konfirmasi di Order List).
    /// Setelah berhasil, grid otomatis ter-refresh karena halaman
    /// Index dimuat ulang, lengkap dengan notifikasi sukses.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id, string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        ApiResult result = await _salesOrderApi.DeleteOrderAsync(id, cancellationToken);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        // Filter yang sedang aktif dipertahankan setelah hapus.
        return RedirectToAction(nameof(Index), new { keyword, orderDate });
    }

    /// <summary>
    /// Unduh Excel. Front-end hanya meneruskan file yang dibuat service
    /// (front-end tidak membuat file Excel sendiri).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        ApiResult<ExportedFile> result =
            await _salesOrderApi.ExportOrdersAsync(keyword, orderDate, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index), new { keyword, orderDate });
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Endpoint AJAX untuk tombol ✓ pada baris item.
    ///
    /// Front-end (JavaScript) TIDAK menghitung apa pun: ia mengirim daftar
    /// item ke sini, action ini meneruskannya ke Sales Order Service, dan
    /// angka TOTAL + Grand Total yang dikembalikan service itulah yang
    /// ditampilkan di tabel.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CalculateItems(
        [FromBody] CalculateItemsRequest request, CancellationToken cancellationToken)
    {
        ApiResult<CalculateItemsResponse> result =
            await _salesOrderApi.CalculateItemsAsync(request, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                message = result.Message,
                errors = result.Errors
            });
        }

        return Json(result.Data);
    }

    /// <summary>
    /// Mengubah isi form menjadi body request untuk service.
    /// Baris item kosong (semua field kosong) dibuang agar tidak ikut
    /// terkirim — misalnya baris yang ditinggalkan pengguna.
    /// </summary>
    private static SaveOrderRequest BuildSaveRequest(OrderFormViewModel form)
    {
        return new SaveOrderRequest
        {
            SoNo = form.SoNo,
            OrderDate = form.OrderDate,
            CustomerId = form.CustomerId,
            Address = form.Address,
            Items = (form.Items ?? new List<OrderItemDto>())
                .Where(item => !string.IsNullOrWhiteSpace(item.ItemName)
                               || item.Quantity != 0
                               || item.Price != 0m)
                .Select(item => new SaveOrderItemRequest
                {
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    Price = item.Price
                })
                .ToList()
        };
    }

    /// <summary>Mengisi dropdown Customer dari Customer Service.</summary>
    private async Task LoadCustomersAsync(OrderFormViewModel viewModel, CancellationToken cancellationToken)
    {
        ApiResult<List<CustomerDto>> customers = await _customerApi.GetCustomersAsync(cancellationToken);

        if (customers.Success && customers.Data is not null)
        {
            viewModel.Customers = customers.Data;
            return;
        }

        // Dropdown gagal dimuat: tampilkan penyebabnya, form tetap muncul.
        viewModel.Customers = Array.Empty<CustomerDto>();
        viewModel.ErrorMessage ??= customers.Message;
    }

    /// <summary>
    /// Saat form ditampilkan ulang karena error, TOTAL per baris dan
    /// Grand Total diminta ulang ke service supaya angka yang tampil
    /// tetap berasal dari service (bukan dihitung front-end).
    /// </summary>
    private async Task RefreshTotalsFromServiceAsync(
        OrderFormViewModel form, CancellationToken cancellationToken)
    {
        if (form.Items is null || form.Items.Count == 0)
        {
            form.GrandTotal = 0m;
            return;
        }

        var request = new CalculateItemsRequest
        {
            Items = form.Items
                .Select(item => new SaveOrderItemRequest
                {
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    Price = item.Price
                })
                .ToList()
        };

        ApiResult<CalculateItemsResponse> calculation =
            await _salesOrderApi.CalculateItemsAsync(request, cancellationToken);

        if (!calculation.Success || calculation.Data is null)
        {
            return;
        }

        foreach (CalculatedItemDto calculated in calculation.Data.Items)
        {
            if (calculated.RowIndex >= 0 && calculated.RowIndex < form.Items.Count)
            {
                form.Items[calculated.RowIndex].Total = calculated.Total;
            }
        }

        form.GrandTotal = calculation.Data.GrandTotal;
    }
}
