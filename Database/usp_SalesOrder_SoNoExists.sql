/* ============================================================
   SP    : dbo.usp_SalesOrder_SoNoExists
   Milik : Sales Order Service
   Guna  : Memeriksa apakah sebuah SO_NO sudah dipakai order lain
           (business rule: "Order Number tidak boleh duplikat").

   @ExcludeSalesSoId dipakai pada mode Edit: order yang sedang
   diedit tidak boleh dianggap duplikat terhadap dirinya sendiri.

   Mengembalikan 1 (ada duplikat) atau 0 (aman).
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_SalesOrder_SoNoExists', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesOrder_SoNoExists;
GO

CREATE PROCEDURE dbo.usp_SalesOrder_SoNoExists
    @SoNo             VARCHAR(20),
    @ExcludeSalesSoId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.SALES_SO
        WHERE SO_NO = @SoNo
          AND (@ExcludeSalesSoId IS NULL OR SALES_SO_ID <> @ExcludeSalesSoId)
    ) THEN 1 ELSE 0 END AS SoNoExists;
END
GO

PRINT 'SP dbo.usp_SalesOrder_SoNoExists siap.';
GO
