using System.Data;
using System.Data.Common;
using Dapper;
using SalesOrderService.Data;
using SalesOrderService.Models;
using SalesOrderService.Models.Requests;

namespace SalesOrderService.Repositories;

/// <summary>
/// Implementasi akses data Sales Order memakai Dapper + Stored Procedure.
///
/// Operasi yang menyentuh lebih dari satu tabel (simpan order, update
/// order, hapus order) dijalankan di dalam SqlTransaction sehingga
/// bersifat atomik: kalau ada satu perintah gagal, semuanya dibatalkan
/// dan database tidak meninggalkan data setengah jadi.
/// </summary>
public class SalesOrderRepository : ISalesOrderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SalesOrderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<OrderListItemDto>> SearchAsync(
        string? keyword, DateTime? orderDate, CancellationToken cancellationToken)
    {
        await using DbConnection connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Keyword", keyword, DbType.String, size: 200);

        // Hanya bagian tanggalnya yang dipakai (exact date match),
        // jam diabaikan — sesuai FSD filter Order Date.
        parameters.Add("@OrderDate", orderDate?.Date, DbType.Date);

        var command = new CommandDefinition(
            "dbo.usp_SalesOrder_Search",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        IEnumerable<OrderListItemDto> orders =
            await connection.QueryAsync<OrderListItemDto>(command);

        return orders.ToList();
    }

    public async Task<OrderDetailDto?> GetByIdAsync(int salesSoId, CancellationToken cancellationToken)
    {
        await using DbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            "dbo.usp_SalesOrder_GetById",
            new { SalesSoId = salesSoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        // SP mengembalikan dua result set: header lalu daftar item.
        await using SqlMapper.GridReader gridReader = await connection.QueryMultipleAsync(command);

        OrderDetailDto? order = await gridReader.ReadFirstOrDefaultAsync<OrderDetailDto>();

        if (order is null)
        {
            // Order tidak ada -> service akan membalas HTTP 404.
            return null;
        }

        IEnumerable<OrderItemDto> items = await gridReader.ReadAsync<OrderItemDto>();
        order.Items = items.ToList();

        return order;
    }

    public async Task<bool> SoNoExistsAsync(
        string soNo, int? excludeSalesSoId, CancellationToken cancellationToken)
    {
        await using DbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            "dbo.usp_SalesOrder_SoNoExists",
            new { SoNo = soNo, ExcludeSalesSoId = excludeSalesSoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        int exists = await connection.ExecuteScalarAsync<int>(command);

        return exists == 1;
    }

    public async Task<int> InsertOrderAsync(SaveOrderRequest request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using DbTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1) Simpan header. SP mengembalikan ID baru lewat parameter OUTPUT.
            var headerParameters = new DynamicParameters();
            headerParameters.Add("@SoNo", request.SoNo!.Trim(), DbType.String, size: 20);
            headerParameters.Add("@OrderDate", request.OrderDate!.Value, DbType.DateTime);
            headerParameters.Add("@ComCustomerId", request.CustomerId!.Value, DbType.Int32);
            headerParameters.Add("@Address", NormalizeAddress(request.Address), DbType.String, size: 500);
            headerParameters.Add("@NewSalesSoId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SalesOrder_Insert",
                headerParameters,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            int newSalesSoId = headerParameters.Get<int>("@NewSalesSoId");

            // 2) Simpan seluruh item pada transaksi yang sama.
            await InsertItemsAsync(connection, transaction, newSalesSoId, request.Items, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return newSalesSoId;
        }
        catch
        {
            // Batalkan semua perubahan kalau ada satu langkah pun gagal.
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateOrderAsync(
        int salesSoId, SaveOrderRequest request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using DbTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1) Update header. AffectedRows = 0 berarti order tidak ada.
            var headerParameters = new DynamicParameters();
            headerParameters.Add("@SalesSoId", salesSoId, DbType.Int32);
            headerParameters.Add("@SoNo", request.SoNo!.Trim(), DbType.String, size: 20);
            headerParameters.Add("@OrderDate", request.OrderDate!.Value, DbType.DateTime);
            headerParameters.Add("@ComCustomerId", request.CustomerId!.Value, DbType.Int32);
            headerParameters.Add("@Address", NormalizeAddress(request.Address), DbType.String, size: 500);

            int affectedRows = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "dbo.usp_SalesOrder_Update",
                headerParameters,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // 2) Hapus semua item lama, lalu simpan ulang item terkini.
            //    Strategi "replace all" ini diminta FSD bagian 4.3 supaya
            //    isi database persis sama dengan state form saat disimpan.
            await connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SalesOrderItem_DeleteByOrder",
                new { SalesSoId = salesSoId },
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            await InsertItemsAsync(connection, transaction, salesSoId, request.Items, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(int salesSoId, CancellationToken cancellationToken)
    {
        await using DbConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Hapus item + header harus atomik (FSD bagian 5.4).
        await using DbTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int affectedRows = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "dbo.usp_SalesOrder_Delete",
                new { SalesSoId = salesSoId },
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Menyimpan daftar item satu per satu memakai SP insert item.
    /// Dipakai baik oleh Insert maupun Update, selalu dengan connection
    /// dan transaction milik pemanggil.
    /// </summary>
    private static async Task InsertItemsAsync(
        DbConnection connection,
        DbTransaction transaction,
        int salesSoId,
        IEnumerable<SaveOrderItemRequest> items,
        CancellationToken cancellationToken)
    {
        foreach (SaveOrderItemRequest item in items)
        {
            var itemParameters = new DynamicParameters();
            itemParameters.Add("@SalesSoId", salesSoId, DbType.Int32);
            itemParameters.Add("@ItemName", item.ItemName!.Trim(), DbType.String, size: 100);
            itemParameters.Add("@Quantity", item.Quantity!.Value, DbType.Int32);

            // Kolom PRICE bertipe FLOAT di database (sesuai FSD), jadi nilai
            // decimal dari request dikonversi eksplisit ke double di sini.
            itemParameters.Add("@Price", (double)item.Price!.Value, DbType.Double);
            itemParameters.Add("@NewSalesSoLitemId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SalesOrderItem_Insert",
                itemParameters,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        }
    }

    /// <summary>
    /// Address opsional: string kosong/spasi disimpan sebagai NULL agar
    /// data di database bersih dan konsisten.
    /// </summary>
    private static string? NormalizeAddress(string? address) =>
        string.IsNullOrWhiteSpace(address) ? null : address.Trim();
}
