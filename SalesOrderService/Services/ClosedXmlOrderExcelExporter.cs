using ClosedXML.Excel;
using SalesOrderService.Models;

namespace SalesOrderService.Services;

/// <summary>
/// Implementasi ekspor Excel memakai ClosedXML (lisensi MIT, gratis).
///
/// Kolom mengikuti FSD bagian 5.5: SO Number, Order Date, Customer Name,
/// Address. Grand Total ditambahkan sebagai informasi tambahan
/// (FSD menyebut kolom tersebut sebagai "minimal").
/// </summary>
public class ClosedXmlOrderExcelExporter : IOrderExcelExporter
{
    public byte[] Export(IReadOnlyList<OrderListItemDto> orders)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Sales Order");

        // ---------------- Header tabel ----------------
        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "SO Number";
        worksheet.Cell(1, 3).Value = "Order Date";
        worksheet.Cell(1, 4).Value = "Customer Name";
        worksheet.Cell(1, 5).Value = "Address";
        worksheet.Cell(1, 6).Value = "Grand Total";

        IXLRange headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // ---------------- Isi data ----------------
        // Baris 1 dipakai header, jadi data mulai baris 2.
        int row = 2;

        foreach (OrderListItemDto order in orders)
        {
            worksheet.Cell(row, 1).Value = row - 1;
            worksheet.Cell(row, 2).Value = order.SoNo;

            worksheet.Cell(row, 3).Value = order.OrderDate;
            worksheet.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";

            worksheet.Cell(row, 4).Value = order.CustomerName;
            worksheet.Cell(row, 5).Value = order.Address ?? string.Empty;

            worksheet.Cell(row, 6).Value = order.GrandTotal;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        worksheet.Range(1, 1, Math.Max(row - 1, 1), 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Columns(1, 6).AdjustToContents();
        worksheet.SheetView.FreezeRows(1);

        // Workbook ditulis ke memory stream, lalu dikembalikan sebagai
        // byte array agar Controller cukup mengirimkannya sebagai file.
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}
