using System.IO;
using PharmacyManager.Models;
using PharmacyManager.Services;
using ClosedXML.Excel;
using Xunit;

namespace PharmacyManager.Tests;

public sealed class PartBExcelExportMapperTests
{
    [Fact]
    public async Task Customer_statement_exports_running_balance_and_summary_values()
    {
        var customer = new CustomerModel { Name = "=customer", Phone = "00123456789" };
        var line = new CustomerStatementLineModel
        {
            Date = new DateTime(2026, 10, 28),
            EntryType = "فاتورة بيع",
            Reference = "000012",
            DebitAmount = 30m,
            RunningBalance = 25m,
            InvoiceTotal = 30m,
            InvoicePaid = 5m
        };
        var workbook = PartBExcelExportMappers.CustomerStatement(
            "متجر الاختبار",
            customer,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31),
            new[] { line },
            30m,
            5m,
            25m);

        using var output = await ExportAndOpenAsync(workbook);
        var sheet = output.Worksheet("كشف الحساب");
        Assert.Equal(25d, sheet.Cell(7, 6).GetDouble());
        Assert.Equal(30d, sheet.Cell(8, 4).GetDouble());
        Assert.Equal(5d, sheet.Cell(9, 5).GetDouble());
        Assert.Equal(25d, sheet.Cell(10, 6).GetDouble());
        Assert.Equal("000012", sheet.Cell(7, 3).GetString());
    }

    [Fact]
    public async Task Expense_export_uses_active_only_total_and_category_percentages()
    {
        var expenses = new[]
        {
            new ExpenseModel
            {
                ExpenseDate = new DateTime(2026, 10, 1),
                CategoryName = "إيجار",
                Amount = 100m,
                IsCancelled = false
            },
            new ExpenseModel
            {
                ExpenseDate = new DateTime(2026, 10, 2),
                CategoryName = "إيجار",
                Amount = 50m,
                IsCancelled = true
            }
        };
        var workbook = PartBExcelExportMappers.Expenses("متجر الاختبار", "كل الفترة", expenses);

        using var output = await ExportAndOpenAsync(workbook);
        var detail = output.Worksheet("المصروفات");
        Assert.Equal("SUMIFS(D7:D8,H7:H8,\"فعّال\")", detail.Cell(9, 4).FormulaA1);
        Assert.Contains("SUBTOTAL(103,B7:B8)", detail.Cell(9, 2).FormulaA1);
        Assert.Equal("ملغي", detail.Cell(8, 8).GetString());

        var categories = output.Worksheet("حسب التصنيف");
        Assert.Equal(1d, categories.Cell(7, 4).GetDouble());
        Assert.Equal("SUBTOTAL(109,D7:D7)", categories.Cell(8, 4).FormulaA1);
    }

    [Fact]
    public async Task Customer_names_and_phones_are_exported_as_safe_text()
    {
        var workbook = PartBExcelExportMappers.Customers(
            "متجر الاختبار",
            "كل العملاء",
            new[] { new CustomerModel { Name = "=1+1", Phone = "00123456789", CurrentBalance = 12m } });

        using var output = await ExportAndOpenAsync(workbook);
        var sheet = output.Worksheet("العملاء");
        Assert.Equal("=1+1", sheet.Cell(7, 1).GetString());
        Assert.Equal(XLDataType.Text, sheet.Cell(7, 1).DataType);
        Assert.Equal("00123456789", sheet.Cell(7, 2).GetString());
        Assert.Equal(XLDataType.Text, sheet.Cell(7, 2).DataType);
    }

    [Fact]
    public async Task Reports_include_auditable_profit_formulas_and_only_relevant_warnings()
    {
        var profit = new ProfitSummaryModel
        {
            GrossSales = 100m,
            CogsBeforeReturns = 30m,
            Expenses = 10m,
            IsCogsEstimated = true,
            EstimatedLineCount = 2,
            EstimatedInvoiceCount = 1,
            Previous = new ProfitSummaryModel
            {
                GrossSales = 80m,
                CogsBeforeReturns = 20m,
                Expenses = 8m
            }
        };
        var profitability = new ItemProfitabilityModel
        {
            IsCogsEstimated = true,
            EstimatedLineCount = 2,
            EstimatedInvoiceCount = 1,
            Rows = { new ItemProfitabilityRow { ProductName = "صنف", QuantitySold = 2, SalesValue = 40m, Cost = 20m } }
        };
        var workbook = CreateReportsWorkbook(profit, profitability);

        using var output = await ExportAndOpenAsync(workbook);
        var profitSheet = output.Worksheet("ملخص الأرباح");
        Assert.Equal("B7-B8", profitSheet.Cell(9, 2).FormulaA1);
        Assert.Equal("B9-B10", profitSheet.Cell(11, 2).FormulaA1);
        Assert.Equal("تحذير: التكلفة تقديرية", profitSheet.Cell(12, 1).GetString());
        Assert.Contains(output.Worksheets.Select(sheet => sheet.Name), name => name == "ربحية الأصناف");

        var withoutWarning = CreateReportsWorkbook(
            new ProfitSummaryModel { GrossSales = 100m, CogsBeforeReturns = 30m, Expenses = 10m },
            new ItemProfitabilityModel());
        using var cleanOutput = await ExportAndOpenAsync(withoutWarning);
        Assert.DoesNotContain("تحذير: التكلفة تقديرية",
            Enumerable.Range(7, 5).Select(row => cleanOutput.Worksheet("ملخص الأرباح").Cell(row, 1).GetString()));
    }

    [Fact]
    public async Task Stocktake_exports_difference_formulas_and_blank_count_column()
    {
        var session = new StocktakeSessionModel
        {
            SessionNumber = "0005",
            Name = "جرد المخزن",
            ScopeType = "كامل",
            Status = "مراجعة"
        };
        var lines = new[]
        {
            new StocktakeLineModel
            {
                ProductName = "فلتر",
                Barcode = "00001234",
                Category = "قطع",
                BookQuantity = 4,
                CountedQuantity = 6,
                CostPrice = 2.5m
            },
            new StocktakeLineModel
            {
                ProductName = "زيت",
                Barcode = "00005678",
                BookQuantity = 3,
                CountedQuantity = null
            }
        };

        using var sessionOutput = await ExportAndOpenAsync(
            PartBExcelExportMappers.StocktakeSession("متجر الاختبار", session, lines));
        var sessionSheet = sessionOutput.Worksheet("تقرير الجلسة");
        Assert.Equal("IF(E7=\"\",\"\",E7-D7)", sessionSheet.Cell(7, 6).FormulaA1);
        Assert.Equal("IF(E7=\"\",\"\",F7*G7)", sessionSheet.Cell(7, 8).FormulaA1);
        Assert.Equal("00005678", sessionSheet.Cell(7, 2).GetString());

        using var blindOutput = await ExportAndOpenAsync(
            PartBExcelExportMappers.BlankStocktakeSheet("متجر الاختبار", session, lines, false));
        var blindSheet = blindOutput.Worksheet("ورقة جرد للطباعة");
        Assert.DoesNotContain("الكمية الدفترية", blindSheet.Row(6).Cells().Select(cell => cell.GetString()));
        Assert.True(blindSheet.Cell(7, 5).IsEmpty());

        using var visibleOutput = await ExportAndOpenAsync(
            PartBExcelExportMappers.BlankStocktakeSheet("متجر الاختبار", session, lines, true));
        var visibleSheet = visibleOutput.Worksheet("ورقة جرد للطباعة");
        Assert.Equal(3d, visibleSheet.Cell(7, 5).GetDouble());
        Assert.True(visibleSheet.Cell(7, 6).IsEmpty());
    }

    [Fact]
    public async Task Generated_report_sheet_names_are_valid_and_unique()
    {
        var workbook = CreateReportsWorkbook(new ProfitSummaryModel(), new ItemProfitabilityModel());
        using var output = await ExportAndOpenAsync(workbook);
        var names = output.Worksheets.Select(sheet => sheet.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(names, name =>
        {
            Assert.InRange(name.Length, 1, 31);
            Assert.DoesNotContain(name, character => ":\\/?*[]".Contains(character));
        });
    }

    private static ExcelExportWorkbook CreateReportsWorkbook(
        ProfitSummaryModel profit,
        ItemProfitabilityModel profitability) =>
        PartBExcelExportMappers.Reports(
            "متجر الاختبار",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 11, 1),
            profit,
            new[] { new SalesDailyReportRow { Day = new DateTime(2026, 10, 2), Total = 100m } },
            new TopProductsModel(),
            new[] { new CustomerSalesReportRow { CustomerName = "عميل نقدي" } },
            new ExpenseReportModel(),
            new PurchaseReportModel(),
            new InventoryValuationModel(),
            new CustomerDebtReportModel(),
            profitability);

    private static async Task<XLWorkbook> ExportAndOpenAsync(ExcelExportWorkbook definition)
    {
        var path = Path.Combine(Path.GetTempPath(), $"PharmacyManager.PartB.{Guid.NewGuid():N}.xlsx");
        try
        {
            await new ExcelExportService().ExportAsync(definition, path);
            return new XLWorkbook(path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
