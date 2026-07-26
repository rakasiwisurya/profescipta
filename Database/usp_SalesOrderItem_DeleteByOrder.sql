/* ============================================================
   SP    : dbo.usp_SalesOrderItem_DeleteByOrder
   Milik : Sales Order Service
   Guna  : Menghapus seluruh item milik satu order.

   Dipakai pada mode Edit (PUT /api/orders/{id}): sesuai FSD,
   Save pada mode Edit mengirim keseluruhan state item terkini,
   lalu service menghapus semua item lama dan menyimpan ulang
   item yang dikirim ("replace all") agar data konsisten.
   Seluruh proses ini berjalan dalam satu transaksi.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrderItem_DeleteByOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrderItem_DeleteByOrder;
GO

CREATE PROCEDURE dbo.usp_SalesOrderItem_DeleteByOrder
    @SalesSoId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DELETE FROM dbo.SALES_SO_LITEM
    WHERE SALES_SO_ID = @SalesSoId;
END
GO

PRINT 'SP dbo.usp_SalesOrderItem_DeleteByOrder siap.';
GO
