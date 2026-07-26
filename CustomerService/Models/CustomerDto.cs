namespace CustomerService.Models;

/// <summary>
/// Bentuk data pelanggan yang dikirim ke pemanggil API.
/// Nama properti mengikuti kontrak API pada FSD bagian 6.1
/// (customerId, customerName), bukan nama kolom database.
/// </summary>
public class CustomerDto
{
    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
}
