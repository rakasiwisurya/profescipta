/* ============================================================
   SP    : dbo.usp_SalesOrder_Delete
   Milik : Sales Order Service
   Guna  : Menghapus satu order beserta SELURUH item-nya.

   Urutan hapus wajib: item dulu (child), lalu header (parent),
   karena ada FK FK_LITEM_SO.

   SP ini dipanggil dari dalam SqlTransaction di Repository,
   jadi kedua DELETE bersifat atomik: kalau salah satu gagal,
   keduanya di-rollback (tidak ada item yatim di database).

   Mengembalikan jumlah header yang terhapus
   (0 = order tidak ditemukan -> service balas HTTP 404).
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrder_Delete', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrder_Delete;
GO

CREATE PROCEDURE dbo.usp_SalesOrder_Delete
    @SalesSoId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DELETE FROM dbo.SALES_SO_LITEM
    WHERE SALES_SO_ID = @SalesSoId;

    DELETE FROM dbo.SALES_SO
    WHERE SALES_SO_ID = @SalesSoId;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

PRINT 'SP dbo.usp_SalesOrder_Delete siap.';
GO
