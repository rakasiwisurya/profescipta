/* ============================================================
   Sales Order Management System — Database Schema
   Target : SQL Server 2019 / 2022 (Express pun cukup)
   Isi    : CREATE DATABASE, 3 tabel sesuai Appendix A FSD,
            dan data contoh master pelanggan (3 record).

   Catatan penting:
   Struktur tabel di bawah PERSIS sama dengan Appendix A FSD.
   Tidak ada kolom/tabel yang ditambah, diubah, atau dihapus.
   Script ini idempotent: aman dijalankan ulang dari nol.

   Cara jalankan:
     sqlcmd -S localhost\SQLEXPRESS -E -i schema.sql
   atau buka & execute di SSMS / Azure Data Studio.
   ============================================================ */

IF DB_ID('SalesOrderDb') IS NULL
BEGIN
    CREATE DATABASE SalesOrderDb;
END
GO

USE SalesOrderDb;
GO

/* ------------------------------------------------------------
   Bersihkan objek lama (urutan drop mengikuti dependensi FK)
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.SALES_SO_LITEM', 'U') IS NOT NULL DROP TABLE dbo.SALES_SO_LITEM;
IF OBJECT_ID('dbo.SALES_SO', 'U')       IS NOT NULL DROP TABLE dbo.SALES_SO;
IF OBJECT_ID('dbo.COM_CUSTOMER', 'U')   IS NOT NULL DROP TABLE dbo.COM_CUSTOMER;
GO

/* ------------------------------------------------------------
   Tabel Master Pelanggan
   ------------------------------------------------------------ */
CREATE TABLE COM_CUSTOMER (
    COM_CUSTOMER_ID  INT          NOT NULL PRIMARY KEY,
    CUSTOMER_NAME    VARCHAR(100) NOT NULL
);
GO

/* ------------------------------------------------------------
   Tabel Sales Order Header
   ------------------------------------------------------------ */
CREATE TABLE SALES_SO (
    SALES_SO_ID      INT          NOT NULL PRIMARY KEY,
    SO_NO            VARCHAR(20)  NOT NULL,
    ORDER_DATE       DATETIME     NOT NULL,
    COM_CUSTOMER_ID  INT          NOT NULL,
    ADDRESS          VARCHAR(500) NULL,
    CONSTRAINT FK_SO_CUSTOMER FOREIGN KEY (COM_CUSTOMER_ID)
        REFERENCES COM_CUSTOMER(COM_CUSTOMER_ID)
);
GO

/* ------------------------------------------------------------
   Tabel Sales Order Detail Item
   ------------------------------------------------------------ */
CREATE TABLE SALES_SO_LITEM (
    SALES_SO_LITEM_ID  INT          NOT NULL PRIMARY KEY,
    SALES_SO_ID        INT          NOT NULL,
    ITEM_NAME          VARCHAR(100) NOT NULL,
    QUANTITY           INT          NOT NULL,
    PRICE              FLOAT        NOT NULL,
    CONSTRAINT FK_LITEM_SO FOREIGN KEY (SALES_SO_ID)
        REFERENCES SALES_SO(SALES_SO_ID)
);
GO

/* ------------------------------------------------------------
   Data contoh master pelanggan (wajib disertakan)
   ------------------------------------------------------------ */
INSERT INTO COM_CUSTOMER VALUES (1, 'PT Maju Bersama');
INSERT INTO COM_CUSTOMER VALUES (2, 'CV Sejahtera Abadi');
INSERT INTO COM_CUSTOMER VALUES (3, 'PT Karya Utama');
INSERT INTO COM_CUSTOMER VALUES (4, 'PT Sinar Rejeki');
INSERT INTO COM_CUSTOMER VALUES (5, 'CV Bangun Persada');
GO

PRINT 'schema.sql selesai: database SalesOrderDb + 3 tabel + data master pelanggan siap.';
GO
