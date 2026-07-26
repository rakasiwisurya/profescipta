/* ============================================================
   SP    : dbo.usp_SalesOrder_Insert
   Milik : Sales Order Service
   Guna  : Menyimpan satu header Sales Order baru dan
           mengembalikan SALES_SO_ID yang baru dibuat.

   Kenapa ID dibuat manual (MAX + 1)?
   Kolom SALES_SO_ID pada FSD didefinisikan "INT NOT NULL PRIMARY KEY"
   TANPA IDENTITY, dan FSD melarang mengubah struktur tabel.
   Jadi nomor urut harus dibuat di sisi SP.

   Agar aman dari race condition saat dua request menyimpan
   bersamaan, baris dibaca dengan hint (TABLOCKX, HOLDLOCK):
   tabel dikunci sampai transaksi pemanggil selesai, sehingga
   tidak ada dua request yang mendapat angka MAX yang sama.
   SP ini selalu dipanggil dari dalam transaksi di Repository.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrder_Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrder_Insert;
GO

CREATE PROCEDURE dbo.usp_SalesOrder_Insert
    @SoNo           VARCHAR(20),
    @OrderDate      DATETIME,
    @ComCustomerId  INT,
    @Address        VARCHAR(500) = NULL,
    @NewSalesSoId   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT @NewSalesSoId = ISNULL(MAX(SALES_SO_ID), 0) + 1
    FROM dbo.SALES_SO WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.SALES_SO (SALES_SO_ID, SO_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS)
    VALUES (@NewSalesSoId, @SoNo, @OrderDate, @ComCustomerId, @Address);
END
GO

PRINT 'SP dbo.usp_SalesOrder_Insert siap.';
GO
