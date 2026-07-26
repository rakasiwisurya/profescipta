/* ============================================================
   SP    : dbo.usp_SalesOrderItem_Insert
   Milik : Sales Order Service
   Guna  : Menyimpan satu baris item order.
           Dipanggil berulang (loop) dari dalam satu transaksi
           saat Save Order, sehingga header + semua item
           tersimpan sebagai satu kesatuan.

   Seperti pada header, SALES_SO_LITEM_ID juga dibuat manual
   (MAX + 1) karena kolomnya bukan IDENTITY dan struktur tabel
   tidak boleh diubah. Hint (TABLOCKX, HOLDLOCK) mencegah dua
   request mendapat ID yang sama.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrderItem_Insert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrderItem_Insert;
GO

CREATE PROCEDURE dbo.usp_SalesOrderItem_Insert
    @SalesSoId          INT,
    @ItemName           VARCHAR(100),
    @Quantity           INT,
    @Price              FLOAT,
    @NewSalesSoLitemId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT @NewSalesSoLitemId = ISNULL(MAX(SALES_SO_LITEM_ID), 0) + 1
    FROM dbo.SALES_SO_LITEM WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.SALES_SO_LITEM (SALES_SO_LITEM_ID, SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE)
    VALUES (@NewSalesSoLitemId, @SalesSoId, @ItemName, @Quantity, @Price);
END
GO

PRINT 'SP dbo.usp_SalesOrderItem_Insert siap.';
GO
