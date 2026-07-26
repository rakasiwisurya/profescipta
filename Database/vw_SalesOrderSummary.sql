/* ============================================================
   VIEW : dbo.vw_SalesOrderSummary
   Guna : Menggabungkan SALES_SO + COM_CUSTOMER dan menghitung
          GRAND_TOTAL (SUM dari QUANTITY * PRICE tiap item).
          Dipakai oleh usp_SalesOrder_Search agar query
          pencarian tetap sederhana dan mudah dibaca.

   Catatan: view TIDAK mengubah struktur tabel apa pun.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.vw_SalesOrderSummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_SalesOrderSummary;
GO

CREATE VIEW dbo.vw_SalesOrderSummary
AS
SELECT
    so.SALES_SO_ID,
    so.SO_NO,
    so.ORDER_DATE,
    so.COM_CUSTOMER_ID,
    cust.CUSTOMER_NAME,
    so.ADDRESS,
    /* Grand Total dihitung di database, bukan di front-end.
       ISNULL dipakai agar order tanpa item tetap bernilai 0. */
    ISNULL((
        SELECT SUM(CONVERT(DECIMAL(18, 2), li.QUANTITY * li.PRICE))
        FROM   dbo.SALES_SO_LITEM li
        WHERE  li.SALES_SO_ID = so.SALES_SO_ID
    ), 0) AS GRAND_TOTAL
FROM dbo.SALES_SO           so
INNER JOIN dbo.COM_CUSTOMER cust ON cust.COM_CUSTOMER_ID = so.COM_CUSTOMER_ID;
GO

PRINT 'View dbo.vw_SalesOrderSummary siap.';
GO
