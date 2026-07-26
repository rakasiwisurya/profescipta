using System.Net.Http.Json;
using FrontEnd.Models;

namespace FrontEnd.ApiClients;

/// <summary>
/// Implementasi pemanggilan Customer Service memakai HttpClient.
///
/// HttpClient di-inject oleh IHttpClientFactory (lihat Program.cs) supaya
/// koneksi HTTP dikelola dengan benar (tidak boros socket) dan base URL
/// service diambil dari konfigurasi, bukan ditulis di dalam kode.
/// </summary>
public class CustomerApiClient : ICustomerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerApiClient> _logger;

    public CustomerApiClient(HttpClient httpClient, ILogger<CustomerApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResult<List<CustomerDto>>> GetCustomersAsync(CancellationToken cancellationToken)
    {
        try
        {
            List<CustomerDto>? customers = await _httpClient
                .GetFromJsonAsync<List<CustomerDto>>("api/customers", cancellationToken);

            return ApiResult<List<CustomerDto>>.Ok(customers ?? new List<CustomerDto>());
        }
        catch (Exception ex)
        {
            // Service mati / tidak bisa dihubungi: front-end tidak boleh
            // ikut error, cukup tampilkan pesan yang jelas ke pengguna.
            _logger.LogError(ex, "Gagal memanggil Customer Service.");

            return ApiResult<List<CustomerDto>>.Fail(
                "Tidak dapat menghubungi Customer Service.",
                new[] { ex.Message });
        }
    }
}
