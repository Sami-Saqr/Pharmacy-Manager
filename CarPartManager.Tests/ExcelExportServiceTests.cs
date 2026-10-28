using System.IO;
using CarPartManager.Services;
using ClosedXML.Excel;
using Xunit;

namespace CarPartManager.Tests;

public sealed class ExcelExportServiceTests
{
    [Fact]
    public async Task Export_writes_rtl_typed_values_safe_text_and_subtotal_formulas()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var destination = Path.Combine(directory, "report.xlsx");
            var workbook = CreateWorkbook(
                new object[]
                {
                    new TestRow("000123", "001234567890", "01001234567", 12.5m, new DateTime(2026, 10, 28), "=1+1"),
                    new TestRow("000124", "009876543210", "01007654321", 7.5m, new DateTime(2026, 10, 29), "+cmd"),
                    new TestRow("000125", "000000000001", "01000000001", 2m, new DateTime(2026, 10, 30), "-1"),
                    new TestRow("000126", "000000000002", "01000000002", 1m, new DateTime(2026, 10, 31), "@SUM(A1)"),
                    new TestRow("000127", "000000000003", "01000000003", 3m, new DateTime(2026, 11, 1), "\tvalue"),
                    new TestRow("000128", "000000000004", "01000000004", 4m, new DateTime(2026, 11, 2), "\rvalue")
                });

            await new ExcelExportService().ExportAsync(workbook, destination);

            using var reopened = new XLWorkbook(destination);
            var sheet = reopened.Worksheet("تقرير");
            Assert.True(sheet.RightToLeft);
            Assert.Equal("رقم الفاتورة", sheet.Cell(6, 1).GetString());
            Assert.Equal("000123", sheet.Cell(7, 1).GetString());
            Assert.Equal("001234567890", sheet.Cell(7, 2).GetString());
            Assert.Equal("01001234567", sheet.Cell(7, 3).GetString());
            Assert.Equal(XLDataType.Text, sheet.Cell(7, 1).DataType);
            Assert.Equal(XLDataType.Text, sheet.Cell(7, 2).DataType);
            Assert.Equal(XLDataType.Text, sheet.Cell(7, 3).DataType);
            Assert.Equal(XLDataType.Number, sheet.Cell(7, 4).DataType);
            Assert.Equal(12.5, sheet.Cell(7, 4).GetDouble());
            Assert.Equal(XLDataType.DateTime, sheet.Cell(7, 5).DataType);
            Assert.Equal(new DateTime(2026, 10, 28), sheet.Cell(7, 5).GetDateTime());
            Assert.Equal("=1+1", sheet.Cell(7, 6).GetString());
            Assert.Equal("+cmd", sheet.Cell(8, 6).GetString());
            Assert.Equal("-1", sheet.Cell(9, 6).GetString());
            Assert.Equal("@SUM(A1)", sheet.Cell(10, 6).GetString());
            Assert.Equal("\tvalue", sheet.Cell(11, 6).GetString());
            Assert.Equal("\nvalue", sheet.Cell(12, 6).GetString());
            Assert.Equal(XLDataType.Text, sheet.Cell(7, 6).DataType);
            Assert.Equal("SUBTOTAL(103,A7:A12)", sheet.Cell(13, 1).FormulaA1);
            Assert.Equal("SUBTOTAL(109,D7:D12)", sheet.Cell(13, 4).FormulaA1);
            Assert.Equal(30m, workbook.Sheets[0].Rows
                .Cast<TestRow>()
                .Sum(row => row.Amount));
            Assert.Equal(6, sheet.AutoFilter.Range.RowCount() - 1);
            Assert.Equal(6, sheet.SheetView.SplitRow);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Export_to_locked_target_does_not_replace_existing_file()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var destination = Path.Combine(directory, "locked.xlsx");
            await File.WriteAllTextAsync(destination, "existing file");
            Exception? exception;
            using (new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                exception = await Record.ExceptionAsync(
                    () => new ExcelExportService().ExportAsync(CreateWorkbook(Array.Empty<object>()), destination));
            }

            Assert.NotNull(exception);
            Assert.True(exception is IOException or UnauthorizedAccessException);
            Assert.Equal("existing file", await File.ReadAllTextAsync(destination));
            Assert.Empty(Directory.GetFiles(directory, ".*.tmp.xlsx"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ExcelExportWorkbook CreateWorkbook(IEnumerable<object> rows) =>
        new()
        {
            ShopName = "متجر الاختبار",
            Title = "تقرير الاختبار",
            Filters = "الفترة: الكل",
            Sheets = new[]
            {
                new ExcelExportSheet
                {
                    Name = "تقرير",
                    Columns = new[]
                    {
                        Column("invoice", "رقم الفاتورة", row => ((TestRow)row).InvoiceNumber, ExcelExportValueType.Text),
                        Column("barcode", "الباركود", row => ((TestRow)row).Barcode, ExcelExportValueType.Text),
                        Column("phone", "الهاتف", row => ((TestRow)row).Phone, ExcelExportValueType.Text),
                        Column("amount", "الإجمالي (ج.م)", row => ((TestRow)row).Amount, ExcelExportValueType.Money),
                        Column("date", "التاريخ", row => ((TestRow)row).Date, ExcelExportValueType.Date),
                        Column("text", "النص", row => ((TestRow)row).UserText, ExcelExportValueType.Text)
                    },
                    Rows = rows.ToList(),
                    Totals = new Dictionary<string, ExcelExportTotalType>
                    {
                        ["invoice"] = ExcelExportTotalType.Count,
                        ["amount"] = ExcelExportTotalType.Sum
                    }
                }
            }
        };

    private static ExcelExportColumn Column(
        string key,
        string header,
        Func<object, object?> value,
        ExcelExportValueType type) =>
        new()
        {
            Key = key,
            Header = header,
            Value = value,
            Type = type
        };

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CarPartManager.ExcelTests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed record TestRow(
        string InvoiceNumber,
        string Barcode,
        string Phone,
        decimal Amount,
        DateTime Date,
        string UserText);
}
