using CustomerService.Models;
using CustomerService.Repositories;

namespace CustomerService.Services;

/// <summary>
/// Implementasi <see cref="ICustomerService"/>.
/// Namanya "CustomerAppService" (bukan "CustomerService") supaya tidak
/// bentrok dengan nama namespace root project ini.
/// </summary>
public class CustomerAppService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerAppService> _logger;

    public CustomerAppService(
        ICustomerRepository customerRepository,
        ILogger<CustomerAppService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerDto> customers = await _customerRepository.GetAllAsync(cancellationToken);

        _logger.LogInformation("Mengambil {Count} data pelanggan.", customers.Count);

        return customers;
    }
}
