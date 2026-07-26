using System.Data;
using CustomerService.Data;
using CustomerService.Models;
using Dapper;

namespace CustomerService.Repositories;

/// <summary>
/// Implementasi akses data pelanggan memakai Dapper + Stored Procedure.
/// Semua query lewat SP (script ada di folder /Database) agar SQL
/// terpusat di database dan mudah ditinjau.
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CustomerRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        using IDbConnection connection = _connectionFactory.CreateConnection();

        // CommandDefinition dipakai supaya CancellationToken ikut diteruskan:
        // kalau pemanggil HTTP membatalkan request, query juga dibatalkan.
        var command = new CommandDefinition(
            commandText: "dbo.usp_Customer_GetAll",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        IEnumerable<CustomerDto> customers = await connection.QueryAsync<CustomerDto>(command);

        return customers.ToList();
    }
}
