using FrontEnd.Models;

namespace FrontEnd.ApiClients;

/// <summary>
/// Jembatan front-end ke Customer Service (HTTP REST).
/// </summary>
public interface ICustomerApiClient
{
    /// <summary>
    /// GET /api/customers — daftar pelanggan untuk dropdown Customer.
    /// </summary>
    Task<ApiResult<List<CustomerDto>>> GetCustomersAsync(CancellationToken cancellationToken);
}
