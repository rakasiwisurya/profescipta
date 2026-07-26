/* ============================================================
   SP    : dbo.usp_SalesOrder_Update
   Milik : Sales Order Service
   Guna  : Memperbarui header Sales Order pada mode Edit.
           Mengembalikan jumlah baris yang ter-update
           (0 = order tidak ditemukan -> service balas HTTP 404).

   Catatan: SO_NO tetap dikirim walaupun di UI mode Edit bersifat
   read-only, supaya SP ini tetap generik dan bisa dipakai jika
   suatu saat nomor order diizinkan berubah.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrder_Update', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrder_Update;
GO

CREATE PROCEDURE dbo.usp_SalesOrder_Update
    @SalesSoId      INT,
    @SoNo           VARCHAR(20),
    @OrderDate      DATETIME,
    @ComCustomerId  INT,
    @Address        VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.SALES_SO
    SET SO_NO           = @SoNo,
        ORDER_DATE      = @OrderDate,
        COM_CUSTOMER_ID = @ComCustomerId,
        ADDRESS         = @Address
    WHERE SALES_SO_ID = @SalesSoId;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

PRINT 'SP dbo.usp_SalesOrder_Update siap.';
GO
