using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrontEnd.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace FrontEnd.ApiClients;

/// <summary>
/// Implementasi pemanggilan Sales Order Service memakai HttpClient.
///
/// Tugas kelas ini murni "penerjemah": menyusun URL/body, mengirim
/// request, lalu membaca respons service menjadi <see cref="ApiResult"/>.
/// Tidak ada aturan bisnis maupun kalkulasi di sini.
/// </summary>
public class SalesOrderApiClient : ISalesOrderApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SalesOrderApiClient> _logger;

    /// <summary>
    /// Service memakai camelCase pada JSON-nya; opsi ini membuat
    /// deserialisasi tidak peduli besar-kecil huruf.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SalesOrderApiClient(HttpClient httpClient, ILogger<SalesOrderApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResult<List<OrderListItemDto>>> SearchOrdersAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        try
        {
            string url = BuildFilterUrl("api/orders", keyword, orderDate);

            List<OrderListItemDto>? orders = await _httpClient
                .GetFromJsonAsync<List<OrderListItemDto>>(url, JsonOptions, cancellationToken);

            return ApiResult<List<OrderListItemDto>>.Ok(orders ?? new List<OrderListItemDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mengambil daftar order.");

            return ApiResult<List<OrderListItemDto>>.Fail(
                "Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    public async Task<ApiResult<OrderDetailDto>> GetOrderAsync(
        int salesSoId, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync($"api/orders/{salesSoId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                ServiceEnvelope? notFound = await ReadEnvelopeAsync(response, cancellationToken);

                return ApiResult<OrderDetailDto>.Fail(
                    notFound?.Message ?? "Order tidak ditemukan", notFound?.Errors, notFound: true);
            }

            response.EnsureSuccessStatusCode();

            OrderDetailDto? order = await response.Content
                .ReadFromJsonAsync<OrderDetailDto>(JsonOptions, cancellationToken);

            return order is null
                ? ApiResult<OrderDetailDto>.Fail("Respons service tidak dapat dibaca.")
                : ApiResult<OrderDetailDto>.Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mengambil detail order {SalesSoId}.", salesSoId);

            return ApiResult<OrderDetailDto>.Fail(
                "Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    public async Task<ApiResult> CreateOrderAsync(
        SaveOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync("api/orders", request, cancellationToken);

            return await BuildResultFromEnvelopeAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal menyimpan order baru.");

            return ApiResult.Fail("Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    public async Task<ApiResult> UpdateOrderAsync(
        int salesSoId, SaveOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync($"api/orders/{salesSoId}", request, cancellationToken);

            return await BuildResultFromEnvelopeAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal memperbarui order {SalesSoId}.", salesSoId);

            return ApiResult.Fail("Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    public async Task<ApiResult> DeleteOrderAsync(int salesSoId, CancellationToken cancellationToken)
    {
        try
        {
            // Endpoint DELETE diamankan API Key; header-nya sudah dipasang
            // otomatis untuk HttpClient ini di Program.cs.
            using HttpResponseMessage response =
                await _httpClient.DeleteAsync($"api/orders/{salesSoId}", cancellationToken);

            return await BuildResultFromEnvelopeAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal menghapus order {SalesSoId}.", salesSoId);

            return ApiResult.Fail("Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    public async Task<ApiResult<CalculateItemsResponse>> CalculateItemsAsync(
        CalculateItemsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync("api/orders/calculate", request, cancellationToken);

            response.EnsureSuccessStatusCode();

            CalculateItemsResponse? calculation = await response.Content
                .ReadFromJsonAsync<CalculateItemsResponse>(JsonOptions, cancellationToken);

            return calculation is null
                ? ApiResult<CalculateItemsResponse>.Fail("Respons kalkulasi tidak dapat dibaca.")
                : ApiResult<CalculateItemsResponse>.Ok(calculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal meminta kalkulasi item ke service.");

            return ApiResult<CalculateItemsResponse>.Fail(
                "Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    public async Task<ApiResult<ExportedFile>> ExportOrdersAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        try
        {
            string url = BuildFilterUrl("api/orders/export", keyword, orderDate);

            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                ServiceEnvelope? envelope = await ReadEnvelopeAsync(response, cancellationToken);

                return ApiResult<ExportedFile>.Fail(
                    envelope?.Message ?? "Ekspor Excel gagal.", envelope?.Errors);
            }

            byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            // Nama file mengikuti yang dikirim service (Content-Disposition);
            // kalau tidak ada, pakai pola nama sesuai FSD bagian 5.5.
            string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? $"SalesOrder_{DateTime.Now:yyyyMMdd}.xlsx";

            return ApiResult<ExportedFile>.Ok(new ExportedFile
            {
                Content = content,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mengekspor data order.");

            return ApiResult<ExportedFile>.Fail(
                "Tidak dapat menghubungi Sales Order Service.", new[] { ex.Message });
        }
    }

    /// <summary>
    /// Menyusun query string filter (keyword dan orderDate) hanya untuk
    /// nilai yang benar-benar diisi, supaya URL tetap bersih.
    /// </summary>
    private static string BuildFilterUrl(string path, string? keyword, DateTime? orderDate)
    {
        var queryParameters = new Dictionary<string, string?>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            queryParameters["keyword"] = keyword.Trim();
        }

        if (orderDate.HasValue)
        {
            // Format YYYY-MM-DD sesuai kontrak API pada FSD.
            queryParameters["orderDate"] =
                orderDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return QueryHelpers.AddQueryString(path, queryParameters);
    }

    /// <summary>
    /// Membaca respons { success, message, errors } dari service dan
    /// mengubahnya menjadi ApiResult, termasuk membedakan kasus 404.
    /// </summary>
    private async Task<ApiResult> BuildResultFromEnvelopeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ServiceEnvelope? envelope = await ReadEnvelopeAsync(response, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return ApiResult.Ok(envelope?.Message ?? "Berhasil");
        }

        return ApiResult.Fail(
            envelope?.Message ?? $"Service membalas status {(int)response.StatusCode}.",
            envelope?.Errors,
            notFound: response.StatusCode == HttpStatusCode.NotFound);
    }

    private async Task<ServiceEnvelope?> ReadEnvelopeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ServiceEnvelope>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            // Body bukan JSON (misalnya halaman error HTML) — jangan sampai
            // membuat front-end ikut crash.
            _logger.LogWarning(ex, "Respons service tidak berbentuk JSON standar.");

            return null;
        }
    }
}
