using CustomerService.Models;

namespace CustomerService.Services;

/// <summary>
/// Lapisan logika bisnis pelanggan (Controller -> Service -> Repository).
/// Untuk domain ini logikanya masih sederhana (baca data master), tetapi
/// lapisannya tetap dibuat agar konsisten dengan Sales Order Service dan
/// siap menampung aturan bisnis berikutnya.
/// </summary>
public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync(CancellationToken cancellationToken);
}
