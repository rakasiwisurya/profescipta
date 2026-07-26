using SalesOrderService.Models.Requests;

namespace SalesOrderService.Domain;

/// <summary>
/// Seluruh aturan validasi Sales Order (FSD bagian 5.1 dan 5.2).
/// Pesan error ditulis PERSIS seperti di FSD supaya front-end cukup
/// menampilkan apa yang dikirim service.
///
/// Catatan: validasi "Order Number tidak boleh duplikat" TIDAK ada di
/// kelas ini karena butuh query database. Pemeriksaan itu dilakukan
/// OrderService lewat Repository, sedangkan kelas ini tetap murni
/// (tanpa dependensi) supaya mudah di-unit test.
/// </summary>
public class OrderValidator
{
    public const string ErrorSoNoRequired = "Order Number tidak boleh kosong";
    public const string ErrorSoNoDuplicate = "Order Number sudah digunakan";
    public const string ErrorOrderDateRequired = "Order Date tidak boleh kosong";
    public const string ErrorCustomerRequired = "Customer harus dipilih";
    public const string ErrorItemRequired = "Order harus memiliki minimal 1 item";
    public const string ErrorItemNameRequired = "Item Name tidak boleh kosong";
    public const string ErrorQuantityInvalid = "QTY harus berupa angka lebih dari 0";
    public const string ErrorPriceInvalid = "Price harus berupa angka lebih dari 0";

    /// <summary>
    /// Batas panjang kolom mengikuti skema database (VARCHAR).
    /// Divalidasi di service supaya pengguna dapat pesan yang jelas,
    /// bukan error "String or binary data would be truncated" dari SQL.
    /// </summary>
    private const int MaxSoNoLength = 20;
    private const int MaxAddressLength = 500;
    private const int MaxItemNameLength = 100;

    /// <summary>
    /// Validasi keseluruhan order (header + semua item) sebelum disimpan.
    /// Mengembalikan daftar pesan error; daftar kosong berarti valid.
    /// </summary>
    public IReadOnlyList<string> ValidateOrder(SaveOrderRequest request)
    {
        var errors = new List<string>();

        errors.AddRange(ValidateHeader(request));

        // Aturan: minimal 1 baris item sebelum order disimpan.
        if (request.Items is null || request.Items.Count == 0)
        {
            errors.Add(ErrorItemRequired);
            return errors;
        }

        // Nomor baris (mulai dari 1) diikutkan supaya pengguna tahu
        // baris mana yang bermasalah saat menyimpan order.
        for (int index = 0; index < request.Items.Count; index++)
        {
            IReadOnlyList<string> itemErrors = ValidateItem(request.Items[index]);

            foreach (string itemError in itemErrors)
            {
                errors.Add($"Item baris {index + 1}: {itemError}");
            }
        }

        return errors;
    }

    /// <summary>
    /// Validasi bagian header order (FSD bagian 5.1), tanpa cek duplikat.
    /// </summary>
    public IReadOnlyList<string> ValidateHeader(SaveOrderRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.SoNo))
        {
            errors.Add(ErrorSoNoRequired);
        }
        else if (request.SoNo.Trim().Length > MaxSoNoLength)
        {
            errors.Add($"Order Number maksimal {MaxSoNoLength} karakter");
        }

        // default(DateTime) diperlakukan sebagai kosong karena JSON
        // "orderDate": "" / tanggal tidak diisi bisa terbaca sebagai 01/01/0001.
        if (request.OrderDate is null || request.OrderDate == default(DateTime))
        {
            errors.Add(ErrorOrderDateRequired);
        }

        if (request.CustomerId is null || request.CustomerId <= 0)
        {
            errors.Add(ErrorCustomerRequired);
        }

        // Address opsional (boleh kosong), hanya panjangnya dibatasi.
        if (!string.IsNullOrEmpty(request.Address) && request.Address.Length > MaxAddressLength)
        {
            errors.Add($"Address maksimal {MaxAddressLength} karakter");
        }

        return errors;
    }

    /// <summary>
    /// Validasi satu baris item (FSD bagian 5.2).
    /// Dipakai baik saat Save Order maupun saat tombol ✓ per baris.
    /// </summary>
    public IReadOnlyList<string> ValidateItem(SaveOrderItemRequest item)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(item.ItemName))
        {
            errors.Add(ErrorItemNameRequired);
        }
        else if (item.ItemName.Trim().Length > MaxItemNameLength)
        {
            errors.Add($"Item Name maksimal {MaxItemNameLength} karakter");
        }

        if (item.Quantity is null || item.Quantity <= 0)
        {
            errors.Add(ErrorQuantityInvalid);
        }

        if (item.Price is null || item.Price <= 0)
        {
            errors.Add(ErrorPriceInvalid);
        }

        return errors;
    }
}
