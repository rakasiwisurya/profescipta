# Sales Order Management System

Technical test .NET Developer — Sales Order Management System berbasis
**microservices** dengan C# / .NET 8, SQL Server, dan front-end ASP.NET Core MVC.

---

## 1. Arsitektur

```
┌────────────────────┐        HTTP REST         ┌────────────────────────┐
│  FrontEnd (MVC)    │ ───────────────────────► │  Customer Service      │
│  http://:5000      │      GET /api/customers  │  http://:5001          │
│                    │                          └───────────┬────────────┘
│  - Order List      │                                      │
│  - Order Input     │        HTTP REST         ┌───────────▼────────────┐
│  (tanpa akses DB)  │ ───────────────────────► │  Sales Order Service   │
└────────────────────┘   /api/orders (CRUD,     │  http://:5002          │
                          calculate, export)    └───────────┬────────────┘
                                                            │ Dapper + SP
                                                ┌───────────▼────────────┐
                                                │ SQL Server             │
                                                │ database SalesOrderDb  │
                                                └────────────────────────┘
```

| Komponen               | Port | Tanggung jawab                                                                 | Tabel                          |
| ---------------------- | ---- | ------------------------------------------------------------------------------ | ------------------------------ |
| **CustomerService**    | 5001 | Data master pelanggan untuk dropdown Customer                                   | `COM_CUSTOMER`                 |
| **SalesOrderService**  | 5002 | CRUD Sales Order + item, pencarian, validasi, kalkulasi total, ekspor Excel      | `SALES_SO`, `SALES_SO_LITEM`   |
| **FrontEnd**           | 5000 | Lapisan tampilan; memanggil kedua service via HTTP                               | — (tidak mengakses database)   |

Catatan penting:

- Project **FrontEnd tidak memiliki connection string** dan tidak punya library
  akses database sama sekali. Semua data diambil dari service lewat HTTP.
- **Seluruh validasi dan kalkulasi (TOTAL baris & Grand Total) dijalankan di
  Sales Order Service.** JavaScript di front-end hanya menampilkan angka yang
  dikembalikan service dan memformat pemisah ribuan.
- Struktur internal tiap service: **Controller → Service → Repository → Database**.

---

## 2. Prasyarat

| Kebutuhan          | Versi yang dipakai saat pengembangan                       |
| ------------------ | ---------------------------------------------------------- |
| .NET SDK           | 8.0 (`dotnet --version` ≥ 8.0)                             |
| SQL Server         | SQL Server Express (instance `localhost\SQLEXPRESS`)       |
| Sistem operasi     | Windows (Windows Authentication dipakai secara default)    |

Cek .NET SDK:

```powershell
dotnet --info
```

---

## 3. Setup Database

Seluruh script ada di folder [`/Database`](Database).

### 3.1 Cara cepat (satu perintah, tanpa perlu sqlcmd)

```powershell
cd Database
.\install-database.ps1 -ServerInstance "localhost\SQLEXPRESS"
```

Script menjalankan semua file `.sql` dalam urutan yang benar:
schema → view → stored procedure → data contoh order.

Opsi tambahan:

```powershell
# SQL Authentication
.\install-database.ps1 -ServerInstance "localhost\SQLEXPRESS" -UserId sa -Password "P@ssw0rd"

# tanpa data contoh order (hanya tabel + master pelanggan)
.\install-database.ps1 -ServerInstance "localhost\SQLEXPRESS" -SkipSampleOrders
```

### 3.2 Cara manual (SSMS / Azure Data Studio / sqlcmd)

Jalankan berurutan:

| Urutan | File                                      | Isi                                                        |
| ------ | ----------------------------------------- | ---------------------------------------------------------- |
| 1      | `schema.sql`                              | `CREATE DATABASE SalesOrderDb` + 3 tabel + 5 master pelanggan |
| 2      | `vw_SalesOrderSummary.sql`                | View: header + nama pelanggan + `GRAND_TOTAL`               |
| 3      | `usp_Customer_GetAll.sql`                 | SP daftar pelanggan                                         |
| 4      | `usp_SalesOrder_Search.sql`               | SP pencarian order (keyword + tanggal)                      |
| 5      | `usp_SalesOrder_GetById.sql`              | SP detail order (2 result set: header & item)               |
| 6      | `usp_SalesOrder_Insert.sql`               | SP simpan header, mengembalikan ID baru                     |
| 7      | `usp_SalesOrder_Update.sql`               | SP update header                                            |
| 8      | `usp_SalesOrder_Delete.sql`               | SP hapus order + seluruh item                               |
| 9      | `usp_SalesOrder_SoNoExists.sql`           | SP cek duplikat `SO_NO`                                     |
| 10     | `usp_SalesOrderItem_Insert.sql`           | SP simpan satu item                                         |
| 11     | `usp_SalesOrderItem_DeleteByOrder.sql`    | SP hapus semua item milik satu order                        |
| 12     | `sample-data-orders.sql` *(opsional)*     | 3 order contoh + 5 item                                     |

Contoh dengan sqlcmd:

```powershell
sqlcmd -S localhost\SQLEXPRESS -E -i schema.sql
sqlcmd -S localhost\SQLEXPRESS -E -i vw_SalesOrderSummary.sql
# ...dan seterusnya sesuai urutan tabel di atas
```

> Struktur ketiga tabel **persis sama** dengan Appendix A FSD — tidak ada kolom
> atau tabel yang ditambah/diubah/dihapus. Yang ditambahkan hanya View dan
> Stored Procedure, masing-masing dalam file `.sql` terpisah.

### 3.3 Connection string

Diatur di `appsettings.json` masing-masing service (tidak di-hardcode di kode):

```json
"ConnectionStrings": {
  "SalesOrderDb": "Server=localhost\\SQLEXPRESS;Database=SalesOrderDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
}
```

Bisa juga di-override lewat environment variable tanpa mengubah file:

```powershell
$env:ConnectionStrings__SalesOrderDb = "Server=.\SQLEXPRESS;Database=SalesOrderDb;User Id=sa;Password=xxx;TrustServerCertificate=True"
```

---

## 4. Menjalankan Aplikasi

### 4.1 Cara cepat

```powershell
.\run-all.ps1
```

Script membuka tiga jendela PowerShell (satu per service) lalu menampilkan URL
yang bisa dibuka.

### 4.2 Manual — tiga terminal terpisah

```powershell
# Terminal 1 — Customer Service (port 5001)
dotnet run --project CustomerService\CustomerService.csproj

# Terminal 2 — Sales Order Service (port 5002)
dotnet run --project SalesOrderService\SalesOrderService.csproj

# Terminal 3 — Front-End (port 5000)
dotnet run --project FrontEnd\FrontEnd.csproj
```

### 4.3 Alamat penting

| Alamat                             | Isi                                        |
| ---------------------------------- | ------------------------------------------ |
| <http://localhost:5000>            | **UI aplikasi** (langsung ke Order List)   |
| <http://localhost:5000/Orders/Create> | Form tambah order                       |
| <http://localhost:5001/swagger>    | Swagger Customer Service                   |
| <http://localhost:5002/swagger>    | Swagger Sales Order Service                |
| <http://localhost:5001/health>     | Cek Customer Service hidup                 |
| <http://localhost:5002/health>     | Cek Sales Order Service hidup              |

### 4.4 Build / publish

```powershell
# build seluruh solution
dotnet build SalesOrderManagement.sln

# publish per service (hasil di folder publish masing-masing)
dotnet publish CustomerService\CustomerService.csproj   -c Release -o publish\CustomerService
dotnet publish SalesOrderService\SalesOrderService.csproj -c Release -o publish\SalesOrderService
dotnet publish FrontEnd\FrontEnd.csproj                 -c Release -o publish\FrontEnd
```

---

## 5. Kontrak API

### 5.1 Customer Service (port 5001)

| Method | Endpoint         | Keterangan                                    |
| ------ | ---------------- | --------------------------------------------- |
| GET    | `/api/customers` | Semua pelanggan: `customerId`, `customerName` |

### 5.2 Sales Order Service (port 5002)

| Method | Endpoint                                        | Auth        | Keterangan                                            |
| ------ | ----------------------------------------------- | ----------- | ----------------------------------------------------- |
| GET    | `/api/orders?keyword=&orderDate=YYYY-MM-DD`     | —           | Daftar order, kedua filter opsional                    |
| GET    | `/api/orders/{id}`                              | —           | Detail order + seluruh item                            |
| POST   | `/api/orders`                                   | —           | Buat order baru → `201` + `salesSoId`                  |
| PUT    | `/api/orders/{id}`                              | —           | Update order; seluruh item lama diganti item baru      |
| DELETE | `/api/orders/{id}`                              | **API Key** | Hapus order + seluruh item (satu transaksi)            |
| GET    | `/api/orders/export?keyword=&orderDate=`        | **API Key** | Unduh `.xlsx` sesuai filter yang aktif                 |
| POST   | `/api/orders/calculate`                         | —           | *(tambahan)* hitung TOTAL per baris + Grand Total      |

Contoh body `POST /api/orders`:

```json
{
  "soNo": "SO-2026-001",
  "orderDate": "2026-01-15",
  "customerId": 1,
  "address": "Jl. Sudirman No. 1, Jakarta",
  "items": [
    { "itemName": "Laptop Dell XPS 13", "quantity": 2, "price": 15000000 },
    { "itemName": "Mouse Wireless", "quantity": 3, "price": 350000 }
  ]
}
```

Format error seragam untuk semua service dan semua status code:

```json
{
  "success": false,
  "message": "Data order tidak valid",
  "errors": ["Order Number sudah digunakan", "Item baris 1: QTY harus berupa angka lebih dari 0"]
}
```

---

## 6. Endpoint yang Diamankan (API Key)

Dua endpoint berdampak besar diamankan dengan API Key lewat header
**`X-Api-Key`**. Kunci dibaca dari konfigurasi `Security:ApiKey` pada
`SalesOrderService/appsettings.json`:

```json
"Security": { "ApiKey": "profescipta-sales-order-2026" }
```

Untuk produksi, override lewat environment variable:

```powershell
$env:Security__ApiKey = "kunci-rahasia-produksi"
```

### Contoh pemanggilan

**PowerShell — hapus order (berhasil):**

```powershell
Invoke-RestMethod -Uri "http://localhost:5002/api/orders/1" -Method Delete `
  -Headers @{ "X-Api-Key" = "profescipta-sales-order-2026" }

# → { "success": true, "message": "Order berhasil dihapus" }
```

**PowerShell — tanpa header (ditolak 401):**

```powershell
Invoke-RestMethod -Uri "http://localhost:5002/api/orders/1" -Method Delete

# → HTTP 401
# { "success": false, "message": "Akses ditolak.",
#   "errors": ["Header X-Api-Key wajib dikirim untuk endpoint ini."] }
```

**PowerShell — ekspor Excel:**

```powershell
Invoke-WebRequest -Uri "http://localhost:5002/api/orders/export?keyword=karya" `
  -Headers @{ "X-Api-Key" = "profescipta-sales-order-2026" } `
  -OutFile "SalesOrder.xlsx"
```

**curl:**

```bash
curl -X DELETE "http://localhost:5002/api/orders/1" -H "X-Api-Key: profescipta-sales-order-2026"

curl -OJ "http://localhost:5002/api/orders/export" -H "X-Api-Key: profescipta-sales-order-2026"
```

**Swagger UI:** buka <http://localhost:5002/swagger>, klik **Authorize**,
masukkan API Key, lalu endpoint bertanda kunci bisa dicoba langsung.

Front-end mengirim header ini otomatis (diatur sekali di `FrontEnd/Program.cs`),
jadi tombol Delete dan Export di UI langsung berfungsi.

---

## 7. Unit Test

```powershell
dotnet test SalesOrderService.Tests\SalesOrderService.Tests.csproj
```

26 test, tanpa perlu database:

- `OrderCalculatorTests` — TOTAL per baris, Grand Total, pembulatan 2 desimal,
  dan baris dengan qty/harga belum diisi.
- `OrderValidatorTests` — seluruh aturan validasi FSD bagian 5.1 & 5.2,
  termasuk memastikan teks pesan error tetap sama dengan FSD.

---

## 8. Struktur Repository

```
profescipta/
├── CustomerService/            Web API pelanggan (port 5001)
│   ├── Controllers/            CustomersController
│   ├── Services/               logika bisnis
│   ├── Repositories/           Dapper + stored procedure
│   ├── Data/                   factory koneksi SQL
│   ├── Middleware/             penyeragam format error
│   └── Models/, Common/
├── SalesOrderService/          Web API Sales Order (port 5002)
│   ├── Controllers/            OrdersController
│   ├── Domain/                 OrderValidator, OrderCalculator
│   ├── Services/               OrderService, exporter Excel
│   ├── Repositories/           Dapper + SP + transaksi
│   ├── Security/               ApiKeyAuthorizeAttribute
│   ├── Data/, Middleware/, Models/, Common/
├── SalesOrderService.Tests/    unit test logika bisnis (xUnit)
├── FrontEnd/                   ASP.NET Core MVC (port 5000)
│   ├── Controllers/            OrdersController (lapisan tampilan)
│   ├── ApiClients/             typed HttpClient ke kedua service
│   ├── ViewModels/, Models/
│   ├── Views/Orders/           Index.cshtml, OrderForm.cshtml
│   └── wwwroot/js/             order-list.js, order-form.js
├── Database/
│   ├── schema.sql              tabel + data master pelanggan
│   ├── vw_SalesOrderSummary.sql
│   ├── usp_*.sql               9 stored procedure
│   ├── sample-data-orders.sql  data contoh order (opsional)
│   └── install-database.ps1    jalankan semua script berurutan
├── run-all.ps1                 jalankan ketiga service sekaligus
├── README.md
└── CATATAN-DESAIN.md
```

---

## 9. Troubleshooting

| Gejala                                                            | Penyebab & solusi                                                                                                              |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| UI menampilkan "Tidak dapat menghubungi Sales Order Service"      | Service port 5002 belum jalan. Cek <http://localhost:5002/health>.                                                              |
| Dropdown Customer kosong                                          | Customer Service (5001) belum jalan, atau tabel `COM_CUSTOMER` kosong.                                                          |
| Error "Cannot open database SalesOrderDb"                         | Script database belum dijalankan → jalankan `Database\install-database.ps1`.                                                     |
| Error login / "A network-related or instance-specific error"      | Nama instance berbeda. Sesuaikan `Server=` di `appsettings.json` (mis. `localhost`, `.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`).   |
| Error sertifikat SSL saat konek SQL                               | Pastikan connection string memuat `TrustServerCertificate=True`.                                                                 |
| Port sudah dipakai                                                | Ubah `Urls` di `appsettings.json` service terkait, dan sesuaikan `Services:*BaseUrl` di `FrontEnd/appsettings.json`.             |
| `dotnet build` gagal karena file terkunci                         | Service masih berjalan. Hentikan dulu proses `CustomerService`/`SalesOrderService`/`FrontEnd`.                                   |
