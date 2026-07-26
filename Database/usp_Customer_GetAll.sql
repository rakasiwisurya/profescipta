/* ============================================================
   SP    : dbo.usp_Customer_GetAll
   Milik : Customer Service
   Guna  : Mengambil seluruh data master pelanggan untuk
           dropdown Customer di halaman Order Input.
   ============================================================ */

USE SalesOrderDb;
GO

IF OBJECT_ID('dbo.usp_Customer_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Customer_GetAll;
GO

CREATE PROCEDURE dbo.usp_Customer_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COM_CUSTOMER_ID AS CustomerId,
        CUSTOMER_NAME   AS CustomerName
    FROM dbo.COM_CUSTOMER
    ORDER BY CUSTOMER_NAME;
END
GO

PRINT 'SP dbo.usp_Customer_GetAll siap.';
GO
