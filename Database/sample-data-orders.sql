/* ============================================================
   Data contoh Sales Order (OPSIONAL)
   Guna : mengisi grid Order List dengan beberapa order contoh
          supaya aplikasi langsung bisa dicoba tanpa harus
          input manual dulu. Boleh dilewati.

   Jalankan SETELAH schema.sql.
   ============================================================ */

USE SalesOrderDb;
GO

DELETE FROM dbo.SALES_SO_LITEM;
DELETE FROM dbo.SALES_SO;
GO

INSERT INTO dbo.SALES_SO (SALES_SO_ID, SO_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS) VALUES
    (1, 'SO-2024-001', '2024-01-01', 1, 'Jl. Sudirman No. 1, Jakarta'),
    (2, 'SO-2024-002', '2024-01-03', 2, 'Jl. Gatot Subroto No. 5, Jakarta'),
    (3, 'SO-2024-003', '2024-01-05', 3, 'Jl. Thamrin No. 10, Jakarta');
GO

INSERT INTO dbo.SALES_SO_LITEM (SALES_SO_LITEM_ID, SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE) VALUES
    (1, 1, 'Laptop Dell XPS 13',     2, 15000000),
    (2, 1, 'Mouse Wireless Logitech', 3,   350000),
    (3, 2, 'Monitor LG 24 inch',      4,  1800000),
    (4, 3, 'Printer Epson L3210',     1,  2650000),
    (5, 3, 'Kertas A4 80gsm (rim)',  10,    55000);
GO

PRINT 'sample-data-orders.sql selesai: 3 order contoh + 5 item.';
GO
