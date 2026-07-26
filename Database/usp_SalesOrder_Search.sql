/* ============================================================
   SP    : dbo.usp_SalesOrder_Search
   Milik : Sales Order Service
   Guna  : Mengambil daftar order untuk halaman Order List dan
           untuk ekspor Excel, dengan dua filter opsional:
             @Keyword   -> dicocokkan ke SO_NO, CUSTOMER_NAME, ADDRESS
                           (case-insensitive, LIKE %keyword%)
             @OrderDate -> exact date match pada ORDER_DATE
           Jika kedua parameter NULL/kosong -> tampilkan semua data.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrder_Search', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrder_Search;
GO

CREATE PROCEDURE dbo.usp_SalesOrder_Search
    @Keyword   VARCHAR(200) = NULL,
    @OrderDate DATE         = NULL
AS
BEGIN
    SET NOCOUNT ON;

    /* NULLIF(LTRIM(RTRIM(...)), '') menyeragamkan keyword kosong
       dan keyword berisi spasi menjadi NULL, supaya kondisi filter
       di bawah cukup memeriksa NULL saja. */
    DECLARE @kw VARCHAR(200) = NULLIF(LTRIM(RTRIM(@Keyword)), '');

    SELECT
        SALES_SO_ID     AS SalesSoId,
        SO_NO           AS SoNo,
        ORDER_DATE      AS OrderDate,
        COM_CUSTOMER_ID AS CustomerId,
        CUSTOMER_NAME   AS CustomerName,
        ADDRESS         AS Address,
        GRAND_TOTAL     AS GrandTotal
    FROM dbo.vw_SalesOrderSummary
    WHERE
        (
            @kw IS NULL
            OR SO_NO         LIKE '%' + @kw + '%'
            OR CUSTOMER_NAME LIKE '%' + @kw + '%'
            OR ISNULL(ADDRESS, '') LIKE '%' + @kw + '%'
        )
        AND
        (
            @OrderDate IS NULL
            OR CAST(ORDER_DATE AS DATE) = @OrderDate
        )
    ORDER BY ORDER_DATE DESC, SALES_SO_ID DESC;
END
GO

PRINT 'SP dbo.usp_SalesOrder_Search siap.';
GO
