/* ============================================================
   SP    : dbo.usp_SalesOrder_GetById
   Milik : Sales Order Service
   Guna  : Mengambil satu order (header) beserta seluruh item-nya
           untuk halaman Order Input mode Edit.

   SP ini mengembalikan DUA result set:
     1) Header order (termasuk GRAND_TOTAL)
     2) Daftar item order (termasuk TOTAL per baris)
   Di sisi C# dibaca dengan Dapper QueryMultiple.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrder_GetById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrder_GetById;
GO

CREATE PROCEDURE dbo.usp_SalesOrder_GetById
    @SalesSoId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: header
    SELECT
        SALES_SO_ID     AS SalesSoId,
        SO_NO           AS SoNo,
        ORDER_DATE      AS OrderDate,
        COM_CUSTOMER_ID AS CustomerId,
        CUSTOMER_NAME   AS CustomerName,
        ADDRESS         AS Address,
        GRAND_TOTAL     AS GrandTotal
    FROM dbo.vw_SalesOrderSummary
    WHERE SALES_SO_ID = @SalesSoId;

    -- Result set 2: item. TOTAL per baris dihitung di database.
    SELECT
        SALES_SO_LITEM_ID AS SalesSoLitemId,
        SALES_SO_ID       AS SalesSoId,
        ITEM_NAME         AS ItemName,
        QUANTITY          AS Quantity,
        /* PRICE disimpan FLOAT sesuai FSD; dikonversi ke DECIMAL(18,2)
           di sini supaya nilai uang yang keluar dari API sudah rapi
           dan bebas galat floating point. */
        CONVERT(DECIMAL(18, 2), PRICE)            AS Price,
        CONVERT(DECIMAL(18, 2), QUANTITY * PRICE) AS Total
    FROM dbo.SALES_SO_LITEM
    WHERE SALES_SO_ID = @SalesSoId
    ORDER BY SALES_SO_LITEM_ID;
END
GO

PRINT 'SP dbo.usp_SalesOrder_GetById siap.';
GO
