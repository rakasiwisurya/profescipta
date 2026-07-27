# Catatan Desain

## 1. Alasan pembagian service

Pembagian mengikuti **domain data**, bukan lapisan teknis. `COM_CUSTOMER` adalah
data master yang jarang berubah dan hanya dibaca (untuk dropdown), sedangkan
`SALES_SO` + `SALES_SO_LITEM` adalah data transaksi dengan aturan bisnis padat
(validasi, kalkulasi, transaksi hapus). Dua kebutuhan ini punya alasan berubah
yang berbeda, jadi wajar dipisah:

- **Customer Service (5001)** — satu endpoint baca. Kalau nanti data pelanggan
  berkembang (alamat, NPWP, status aktif), perubahannya tidak menyentuh
  Sales Order Service.
- **Sales Order Service (5002)** — semua aturan bisnis order. Ini bagian yang
  paling sering berubah, dan dengan dipisah, deploy-nya tidak ikut menurunkan
  layanan data master.
- **Front-End (5000)** — hanya lapisan tampilan. Tidak punya connection string
  dan tidak punya library database sama sekali, sehingga tidak mungkin
  "mengambil jalan pintas" langsung ke database.

Front-end memanggil service lewat typed `HttpClient` (base URL dari konfigurasi),
sehingga alamat service bisa berubah tanpa menyentuh kode.

## 2. Bagian yang dibantu AI vs dikerjakan sendiri

Saya memakai bantuan AI (Claude, ChatGPT) secara cukup luas untuk penulisan kode
proyek ini, jadi saya sebutkan apa adanya. Yang dibantu AI:

- Scaffolding solution dan struktur folder ketiga project.
- Kode Repository (Dapper + stored procedure), `OrderService`, `OrderValidator`,
  `OrderCalculator`, dan filter API Key.
- Script database (schema, view, 9 stored procedure) serta script pembantu
  `install-database.ps1` dan `run-all.ps1`.
- View Razor, CSS, dan JavaScript grid item (`order-form.js`).
- Unit test dan dokumentasi (README dan catatan ini).

Yang tidak diserahkan ke AI: penentuan arah kerja dan pemeriksaan hasil —
memastikan setiap butir FSD terpenuhi (larangan kalkulasi di front-end, larangan
mengubah struktur tabel, transaksi atomik saat hapus), menjalankan tiap endpoint
dan tiap alur UI untuk membuktikan hasilnya benar, serta memutuskan hal-hal yang
tidak dijelaskan FSD (endpoint `calculate`, pembuatan ID di dalam SP, paging di
sisi front-end). Setiap baris kode di repo ini sudah saya baca dan pahami, dan
saya siap menjelaskan maupun memodifikasinya tanpa bantuan AI.

## 3. Keputusan teknis penting

**ID primary key dibuat di dalam SP.** FSD mendefinisikan `SALES_SO_ID` dan
`SALES_SO_LITEM_ID` sebagai `INT NOT NULL PRIMARY KEY` **tanpa IDENTITY**, dan
struktur tabel tidak boleh diubah. Jadi nomor baru dibuat di SP dengan
`ISNULL(MAX(id), 0) + 1` memakai hint `(TABLOCKX, HOLDLOCK)` supaya dua request
bersamaan tidak mendapat angka yang sama. Kalau kolomnya boleh diubah, IDENTITY
tentu pilihan yang lebih baik.

**Kalkulasi memakai `decimal`, walau kolom `PRICE` bertipe `FLOAT`.** `FLOAT`
rawan galat pembulatan untuk nilai uang. Nilai dikonversi ke `DECIMAL(18,2)` di
SP saat dibaca dan dihitung sebagai `decimal` di C#, lalu dikonversi kembali ke
`double` hanya ketika menulis ke kolom `PRICE`.

**Endpoint tambahan `POST /api/orders/calculate`.** FSD melarang kalkulasi di
JavaScript, tetapi tombol ✓ per baris harus langsung menampilkan TOTAL. Solusinya:
front-end mengirim seluruh baris item ke service, service memvalidasi tiap baris
dan mengembalikan TOTAL per baris, Grand Total, serta pesan error per baris.
JavaScript hanya menyalin angka itu ke tabel dan memformat pemisah ribuan —
memformat, bukan menghitung.

**Transaksi dikelola di Repository, bukan di dalam SP.** Simpan/update/hapus
order memanggil beberapa SP, semuanya di dalam satu `SqlTransaction`. Dengan
begitu batas transaksi terlihat jelas di satu tempat, dan SP tetap kecil serta
bisa dipakai ulang. Hapus order karena itu tidak mungkin menyisakan item yatim.

**`ServiceResult` sebagai ganti exception untuk alur normal.** Gagal validasi dan
data tidak ditemukan adalah kejadian yang wajar, bukan kondisi luar biasa.
Service mengembalikan `ServiceResult`, dan Controller-lah yang menerjemahkannya
menjadi 400 atau 404 — sehingga lapisan bisnis sama sekali tidak tahu soal HTTP.

**Paging dilakukan di front-end.** Kontrak FSD menyebut `GET /api/orders`
mengembalikan array, bukan objek berhalaman. Supaya kontrak tidak berubah,
pemotongan per halaman dikerjakan di controller front-end — ini murni urusan
tampilan. Untuk data yang jauh lebih besar, paging memang harus dipindahkan ke
SP (`OFFSET/FETCH`).

## 4. Bagian yang paling menantang

Menjaga aturan "tidak ada kalkulasi di front-end" tetap konsisten pada grid item
yang interaktif. Grid punya dua mode baris (input dan tampil), bisa menambah,
mengedit, dan menghapus baris sebelum order disimpan — dan setiap perubahan itu
mengubah Grand Total. Mengirim satu baris saja ke service tidak cukup, karena
Grand Total butuh konteks seluruh baris.

Akhirnya setiap perubahan mengirim **seluruh daftar item** ke service, dan
respons service dipakai untuk menggambar ulang tabel. Setiap baris mendapat
`rowIndex`, `isValid`, dan daftar `errors`, sehingga baris yang salah bisa tetap
berada di mode input dengan pesan error inline sementara baris lain tidak
terganggu. Efek sampingnya: satu jalur kode yang sama juga dipakai ketika form
tampil ulang setelah validasi server gagal, jadi angka yang muncul di layar
selalu berasal dari service — tidak pernah dari perhitungan browser.
