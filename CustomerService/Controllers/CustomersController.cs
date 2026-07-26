using CustomerService.Models;
using CustomerService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers;

/// <summary>
/// Endpoint publik Customer Service.
/// Kontrak API mengikuti FSD bagian 6.1.
/// </summary>
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// GET /api/customers — mengambil semua data pelanggan.
    /// Dipakai Front-End untuk mengisi dropdown Customer di Order Input.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerDto> customers = await _customerService.GetAllCustomersAsync(cancellationToken);

        return Ok(customers);
    }
}
