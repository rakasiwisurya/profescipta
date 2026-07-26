using CustomerService.Models;

namespace CustomerService.Repositories;

/// <summary>
/// Lapisan akses data untuk tabel COM_CUSTOMER.
/// Hanya Repository yang boleh menyentuh database.
/// </summary>
public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken);
}
