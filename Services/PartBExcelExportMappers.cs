using ClosedXML.Excel;
using CarPartManager.Models;

namespace CarPartManager.Services
{
    public static class PartBExcelExportMappers
    {
        public static ExcelExportWorkbook Customers(
            string shopName,
            string filters,
            IEnumerable<CustomerModel> customers)
        {
            var rows = customers.Select(customer => new CustomerRow
            {
                Name = customer.Name,
                Phone = customer.Phone ?? string.Empty,
                Address = customer.Address,
                Balance = customer.CurrentBalance,
                Status = customer.IsActive ? "نشط" : "غير نشط",
                BalanceStatus = customer.CurrentBalance > 0m ? "عليه رصيد" : "مسدد"
            }).Cast<object>().ToList();

            return Workbook(shopName, "تقرير العملاء", filters, new ExcelExportSheet
            {
                Name = "العملاء",
                Columns = new[]
                {
                    Column<CustomerRow>("name", "الاسم", row => row.Name, ExcelExportValueType.Text, 20, 42, true),
                    Column<CustomerRow>("phone", "الهاتف", row => row.Phone, ExcelExportValueType.Text, 16),
                    Column<CustomerRow>("address", "العنوان", row => row.Address, ExcelExportValueType.Text, 18, 42, true),
                    Column<CustomerRow>("balance", "الرصيد الحالي (ج.م)", row => row.Balance, ExcelExportValueType.Money, 18),
                    Column<CustomerRow>("status", "الحالة", row => row.Status, ExcelExportValueType.Text, 12),
                    Column<CustomerRow>("balanceStatus", "حالة الرصيد", row => row.BalanceStatus, ExcelExportValueType.Text, 14)
                },
                Rows = rows,
                Totals = new Dictionary<string, ExcelExportTotalType>
                {
                    ["balance"] = ExcelExportTotalType.Sum
                },
                Highlights = new[]
                {
                    new ExcelExportHighlight
                    {
                        ColumnKey = "balanceStatus",
                        MatchValue = "عليه رصيد",
                        FillColor = "FFFEF2F2",
                        FontColor = "FFB91C1C"
                    }
                }
            });
        }

        public static ExcelExportWorkbook CustomerStatement(
            string shopName,
            CustomerModel customer,
            DateTime from,
            DateTime to,
            IReadOnlyList<CustomerStatementLineModel> lines,
            decimal totalSales,
            decimal totalPaid,
            decimal currentBalance)
        {
            var sheet = new ExcelExportSheet
            {
                Name = "كشف الحساب",
                Filters = $"العميل: {customer.Name} | الهاتف: {customer.Phone} | الفترة: {from:dd/MM/yyyy} إلى {to:dd/MM/yyyy}",
                Columns = new[]
                {
                    Column<CustomerStatementLineModel>("date", "التاريخ", row => row.Date, ExcelExportValueType.Date, 14),
                    Column<CustomerStatementLineModel>("type", "نوع الحركة", row => row.EntryType, ExcelExportValueType.Text, 16),
                    Column<CustomerStatementLineModel>("reference", "المرجع", row => row.Reference, ExcelExportValueType.Text, 18),
                    Column<CustomerStatementLineModel>("debit", "مدين (ج.م)", row => row.DebitAmount, ExcelExportValueType.Money, 16),
                    Column<CustomerStatementLineModel>("credit", "دائن (ج.م)", row => row.CreditAmount, ExcelExportValueType.Money, 16),
                    Column<CustomerStatementLineModel>("balance", "الرصيد التراكمي (ج.م)", row => row.RunningBalance, ExcelExportValueType.Money, 22),
                    Column<CustomerStatementLineModel>("note", "الحالة / ملاحظات", row => row.Description,
                        ExcelExportValueType.Text, 24, 48, true)
                },
                Rows = lines.Cast<object>().ToList(),
                SummaryRows = new[]
                {
                    Summary("إجمالي المبيعات", ("debit", (object?)totalSales)),
                    Summary("إجمالي المدفوع", ("credit", (object?)totalPaid)),
                    Summary("الرصيد الحالي", ("balance", (object?)currentBalance))
                },
                Highlights = new[]
                {
                    new ExcelExportHighlight { ColumnKey = "type", MatchValue = "دفعة ملغاة", FillColor = "FFF1F5F9", FontColor = "FF64748B", Strikeout = true }
                }
            };
            return Workbook(shopName, $"كشف حساب {customer.Name}", sheet.Filters, sheet);
        }

        public static ExcelExportWorkbook Expenses(
            string shopName,
            string filters,
            IReadOnlyList<ExpenseModel> expenses)
        {
            var rows = expenses.Select(expense => new ExpenseRow
            {
                Date = expense.ExpenseDate,
                Category = expense.CategoryName ?? "—",
                Description = expense.Description,
                Amount = expense.Amount,
                PaymentMethod = expense.PaymentMethod,
                Reference = expense.ReferenceNumber ?? string.Empty,
                User = expense.UserDisplayName,
                Status = expense.IsCancelled ? "ملغي" : "فعّال",
                Note = expense.IsCancelled
                    ? expense.CancellationReason ?? expense.Notes ?? string.Empty
                    : expense.Notes ?? string.Empty
            }).Cast<object>().ToList();

            var detailSheet = new ExcelExportSheet
            {
                Name = "المصروفات",
                Filters = filters,
                Columns = new[]
                {
                    Column<ExpenseRow>("date", "التاريخ", row => row.Date, ExcelExportValueType.Date, 14),
                    Column<ExpenseRow>("category", "التصنيف", row => row.Category, ExcelExportValueType.Text, 18, 32, true),
                    Column<ExpenseRow>("description", "الوصف", row => row.Description, ExcelExportValueType.Text, 22, 42, true),
                    Column<ExpenseRow>("amount", "المبلغ (ج.م)", row => row.Amount, ExcelExportValueType.Money, 16),
                    Column<ExpenseRow>("payment", "طريقة الدفع", row => row.PaymentMethod, ExcelExportValueType.Text, 16),
                    Column<ExpenseRow>("reference", "المرجع", row => row.Reference, ExcelExportValueType.Text, 16),
                    Column<ExpenseRow>("user", "المستخدم", row => row.User, ExcelExportValueType.Text, 16),
                    Column<ExpenseRow>("status", "الحالة", row => row.Status, ExcelExportValueType.Text, 12),
                    Column<ExpenseRow>("note", "سبب الإلغاء / ملاحظات", row => row.Note, ExcelExportValueType.Text, 22, 42, true)
                },
                Rows = rows,
                Totals = new Dictionary<string, ExcelExportTotalType>
                {
                    ["amount"] = ExcelExportTotalType.Sum
                },
                TotalFormulas = new Dictionary<string, Func<int, int, int, string>>
                {
                    ["amount"] = (_, last, _) => $"SUMIFS(D7:D{last},H7:H{last},\"فعّال\")"
                },
                SummaryRows = new[]
                {
                    new ExcelExportSummaryRow
                    {
                        Label = "عدد المصروفات الفعّالة",
                        Formulas = new Dictionary<string, string> { ["amount"] = "COUNTIF(H{firstDataRow}:H{lastDataRow},\"فعّال\")" }
                    },
                    new ExcelExportSummaryRow
                    {
                        Label = "عدد المصروفات الملغاة",
                        Formulas = new Dictionary<string, string> { ["amount"] = "COUNTIF(H{firstDataRow}:H{lastDataRow},\"ملغي\")" }
                    }
                },
                Highlights = new[]
                {
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "ملغي", FillColor = "FFF1F5F9", FontColor = "FF64748B", Strikeout = true }
                }
            };

            var activeGroups = expenses.Where(expense => !expense.IsCancelled)
                .GroupBy(expense => expense.CategoryName ?? "—")
                .Select(group => new ExpenseCategoryExportRow
                {
                    Category = group.Key,
                    Count = group.Count(),
                    Total = group.Sum(expense => expense.Amount)
                })
                .OrderByDescending(group => group.Total)
                .ToList();
            var activeTotal = activeGroups.Sum(group => group.Total);
            foreach (var group in activeGroups)
            {
                group.Share = activeTotal == 0m ? 0m : group.Total / activeTotal;
            }

            var categorySheet = new ExcelExportSheet
            {
                Name = "حسب التصنيف",
                Filters = filters + " | الإجماليات تشمل المصروفات الفعّالة فقط",
                Columns = new[]
                {
                    Column<ExpenseCategoryExportRow>("category", "التصنيف", row => row.Category, ExcelExportValueType.Text, 22, 42, true),
                    Column<ExpenseCategoryExportRow>("count", "عدد المصروفات", row => row.Count, ExcelExportValueType.Integer, 18),
                    Column<ExpenseCategoryExportRow>("total", "الإجمالي (ج.م)", row => row.Total, ExcelExportValueType.Money, 18),
                    Column<ExpenseCategoryExportRow>("share", "النسبة من الإجمالي", row => row.Share, ExcelExportValueType.Percentage, 20)
                },
                Rows = activeGroups.Cast<object>().ToList(),
                Totals = new Dictionary<string, ExcelExportTotalType>
                {
                    ["count"] = ExcelExportTotalType.Sum,
                    ["total"] = ExcelExportTotalType.Sum,
                    ["share"] = ExcelExportTotalType.Sum
                }
            };

            return Workbook(shopName, "تقرير المصروفات", filters, detailSheet, categorySheet);
        }

        public static ExcelExportWorkbook StocktakeSession(
            string shopName,
            StocktakeSessionModel session,
            IReadOnlyList<StocktakeLineModel> lines)
        {
            var sortedLines = lines.OrderBy(line => line.Category).ThenBy(line => line.ProductName).ToList();
            var columns = new[]
            {
                Column<StocktakeLineModel>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 20, 42, true),
                Column<StocktakeLineModel>("barcode", "الباركود", row => row.Barcode, ExcelExportValueType.Text, 16),
                Column<StocktakeLineModel>("category", "التصنيف", row => row.Category, ExcelExportValueType.Text, 16),
                Column<StocktakeLineModel>("book", "الرصيد وقت الجرد", row => row.BookQuantity, ExcelExportValueType.Integer, 16),
                Column<StocktakeLineModel>("counted", "الكمية المعدودة", row => row.CountedQuantity, ExcelExportValueType.Integer, 16),
                new ExcelExportColumn
                {
                    Key = "difference",
                    Header = "الفرق",
                    Type = ExcelExportValueType.Integer,
                    MinimumWidth = 12,
                    Value = _ => null,
                    Formula = (_, excelRow) => $"IF(E{excelRow}=\"\",\"\",E{excelRow}-D{excelRow})"
                },
                Column<StocktakeLineModel>("cost", "تكلفة الوحدة (ج.م)", row => row.CostPrice, ExcelExportValueType.Money, 18),
                new ExcelExportColumn
                {
                    Key = "differenceValue",
                    Header = "قيمة الفرق (ج.م)",
                    Type = ExcelExportValueType.Money,
                    MinimumWidth = 20,
                    Value = _ => null,
                    Formula = (_, excelRow) => $"IF(E{excelRow}=\"\",\"\",F{excelRow}*G{excelRow})"
                },
                Column<StocktakeLineModel>("note", "ملاحظات", row => row.Notes, ExcelExportValueType.Text, 18, 42, true),
                Column<StocktakeLineModel>("status", "الحالة", row => row.DifferenceType == "غير معدود" ? "لم يُعد" : row.DifferenceType,
                    ExcelExportValueType.Text, 14)
            };

            var firstData = 7;
            var lastData = firstData + sortedLines.Count - 1;
            var summaryRows = new List<ExcelExportSummaryRow>
            {
                new()
                {
                    Label = "عدد البنود المعدودة",
                    Formulas = new Dictionary<string, string> { ["counted"] = "COUNT(E{firstDataRow}:E{lastDataRow})" }
                },
                new()
                {
                    Label = "عدد البنود غير المعدودة",
                    Formulas = new Dictionary<string, string> { ["counted"] = "COUNTBLANK(E{firstDataRow}:E{lastDataRow})" }
                },
                new()
                {
                    Label = "إجمالي قيمة العجز",
                    Formulas = new Dictionary<string, string> { ["differenceValue"] = "-SUMIFS(H{firstDataRow}:H{lastDataRow},F{firstDataRow}:F{lastDataRow},\"<0\")" },
                    FontColor = "#B91C1C"
                },
                new()
                {
                    Label = "إجمالي قيمة الفائض",
                    Formulas = new Dictionary<string, string> { ["differenceValue"] = "SUMIFS(H{firstDataRow}:H{lastDataRow},F{firstDataRow}:F{lastDataRow},\">0\")" },
                    FontColor = "#047857"
                },
                new()
                {
                    Label = "صافي قيمة الفرق",
                    Formulas = new Dictionary<string, string> { ["differenceValue"] = "SUM(H{firstDataRow}:H{lastDataRow})" }
                }
            };
            var sheet = new ExcelExportSheet
            {
                Name = "تقرير الجلسة",
                Filters = $"الجلسة: {session.SessionNumber} — {session.Name} | النطاق: {session.ScopeType} {session.ScopeFilter} | الحالة: {session.Status} | الإنشاء: {session.SessionDate:dd/MM/yyyy HH:mm} | الاعتماد: {session.ApprovedDate?.ToString("dd/MM/yyyy HH:mm") ?? "—"}",
                Columns = columns,
                Rows = sortedLines.Cast<object>().ToList(),
                SummaryRows = summaryRows,
                Highlights = new[]
                {
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "لم يُعد", FillColor = "FFF1F5F9", FontColor = "FF64748B" },
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "نقص", FillColor = "FFFEF2F2", FontColor = "FFB91C1C" },
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "فائض", FillColor = "FFECFDF5", FontColor = "FF047857" }
                }
            };
            return Workbook(shopName, "تقرير جلسة الجرد", sheet.Filters, sheet);
        }

        public static ExcelExportWorkbook BlankStocktakeSheet(
            string shopName,
            StocktakeSessionModel session,
            IReadOnlyList<StocktakeLineModel> lines,
            bool showBookQuantity)
        {
            var ordered = lines.OrderBy(line => line.Category).ThenBy(line => line.ProductName)
                .Select((line, index) => new BlankStocktakeRow
                {
                    Serial = index + 1,
                    Barcode = line.Barcode,
                    Product = line.ProductName,
                    Category = line.Category,
                    BookQuantity = line.BookQuantity
                }).ToList();
            var columns = new List<ExcelExportColumn>
            {
                Column<BlankStocktakeRow>("serial", "م", row => row.Serial, ExcelExportValueType.Integer, 8),
                Column<BlankStocktakeRow>("barcode", "الباركود", row => row.Barcode, ExcelExportValueType.Text, 18),
                Column<BlankStocktakeRow>("product", "اسم الصنف", row => row.Product, ExcelExportValueType.Text, 24, 42, true),
                Column<BlankStocktakeRow>("category", "التصنيف", row => row.Category, ExcelExportValueType.Text, 18)
            };
            if (showBookQuantity)
            {
                columns.Add(Column<BlankStocktakeRow>("book", "الكمية الدفترية", row => row.BookQuantity, ExcelExportValueType.Integer, 18));
            }
            columns.Add(Column<BlankStocktakeRow>("counted", "الكمية المعدودة", row => (object?)null, ExcelExportValueType.Text, 26, 34));
            columns.Add(Column<BlankStocktakeRow>("notes", "ملاحظات", row => (object?)null, ExcelExportValueType.Text, 28, 42));

            var sheet = new ExcelExportSheet
            {
                Name = "ورقة جرد للطباعة",
                Filters = $"الجلسة: {session.SessionNumber} — {session.Name} | التاريخ: {session.SessionDate:dd/MM/yyyy}",
                Columns = columns,
                Rows = ordered.Cast<object>().ToList(),
                DataRowHeight = 48,
                SummaryRows = new[]
                {
                    new ExcelExportSummaryRow { Label = "اسم القائم بالجرد: ____________________________    التوقيع: ____________________________" }
                }
            };
            return Workbook(shopName, "ورقة جرد فارغة", sheet.Filters, sheet);
        }

        public static ExcelExportWorkbook Reports(
            string shopName,
            DateTime from,
            DateTime to,
            ProfitSummaryModel profit,
            IReadOnlyList<SalesDailyReportRow> salesDaily,
            TopProductsModel topProducts,
            IReadOnlyList<CustomerSalesReportRow> salesByCustomer,
            ExpenseReportModel expenses,
            PurchaseReportModel purchases,
            InventoryValuationModel inventory,
            CustomerDebtReportModel debts,
            ItemProfitabilityModel profitability)
        {
            var sheets = new List<ExcelExportSheet>
            {
                ProfitSheet(profit),
                SalesDailySheet(salesDaily),
                TopProductsSheet(topProducts.ByQuantity, "أفضل الأصناف بالكمية"),
                TopProductsSheet(topProducts.ByValue, "أفضل الأصناف بالقيمة"),
                CustomerSalesSheet(salesByCustomer),
                ExpenseCategoriesSheet(expenses),
                ExpenseDetailsSheet(expenses),
                PurchaseDailySheet(purchases),
                SupplierPurchaseSheet(purchases),
                PurchaseTopProductsSheet(purchases),
                InventoryValuationSheet(inventory),
                CustomerDebtSheet(debts),
                ItemProfitabilitySheet(profitability)
            };

            return new ExcelExportWorkbook
            {
                ShopName = shopName,
                Title = "التقارير المحاسبية",
                Filters = $"الفترة: {from:dd/MM/yyyy} إلى {to.AddDays(-1):dd/MM/yyyy}",
                ExportedAt = DateTime.Now,
                ConfidentialFooter = true,
                Sheets = sheets
            };
        }

        private static ExcelExportSheet ProfitSheet(ProfitSummaryModel report)
        {
            var rows = new List<ProfitRow>
            {
                new() { Label = "صافي المبيعات", Current = report.NetSales, Previous = report.Previous?.NetSales, Change = report.NetSalesChange, Percent = report.NetSalesChangePercent / 100m },
                new()
                {
                    Label = "تكلفة البضاعة المبيعة",
                    Current = report.Cogs,
                    Previous = report.Previous?.Cogs,
                    Change = report.Previous is null ? null : report.Cogs - report.Previous.Cogs,
                    Percent = ProfitSummaryModel.PercentChange(report.Cogs, report.Previous?.Cogs) / 100m
                },
                new() { Label = "الربح الإجمالي", Current = report.GrossProfit, Previous = report.Previous?.GrossProfit, Change = report.GrossProfitChange, Percent = report.GrossProfitChangePercent / 100m },
                new() { Label = "المصروفات", Current = report.Expenses, Previous = report.Previous?.Expenses, Change = report.ExpensesChange, Percent = report.ExpensesChangePercent / 100m },
                new() { Label = "صافي الربح", Current = report.NetProfit, Previous = report.Previous?.NetProfit, Change = report.NetProfitChange, Percent = report.NetProfitChangePercent / 100m }
            };
            if (report.IsCogsEstimated)
            {
                rows.Add(new ProfitRow
                {
                    Label = "تحذير: التكلفة تقديرية",
                    Notes = $"التكلفة تقديرية جزئياً: {report.EstimatedLineCount} بند في {report.EstimatedInvoiceCount} فاتورة قديمة."
                });
            }

            return new ExcelExportSheet
            {
                Name = "ملخص الأرباح",
                Columns = new[]
                {
                    Column<ProfitRow>("label", "البند", row => row.Label, ExcelExportValueType.Text, 26, 42, true),
                    new ExcelExportColumn { Key = "current", Header = "الفترة الحالية (ج.م)", Type = ExcelExportValueType.Money, Value = row => ((ProfitRow)row).Current, MinimumWidth = 20, Formula = (row, excelRow) => ((ProfitRow)row).Label == "الربح الإجمالي" ? $"B{excelRow - 2}-B{excelRow - 1}" : ((ProfitRow)row).Label == "صافي الربح" ? $"B{excelRow - 2}-B{excelRow - 1}" : null },
                    Column<ProfitRow>("previous", "الفترة السابقة (ج.م)", row => row.Previous, ExcelExportValueType.Money, 20),
                    Column<ProfitRow>("change", "التغير (ج.م)", row => row.Change, ExcelExportValueType.Money, 18),
                    Column<ProfitRow>("percent", "التغير (%)", row => row.Percent, ExcelExportValueType.Percentage, 14),
                    Column<ProfitRow>("notes", "ملاحظات", row => row.Notes, ExcelExportValueType.Text, 24, 48, true)
                },
                Rows = rows.Cast<object>().ToList(),
                Highlights = new[]
                {
                    new ExcelExportHighlight { ColumnKey = "label", MatchValue = "تحذير: التكلفة تقديرية", FillColor = "FFFFF7ED", FontColor = "FF9A3412" }
                },
            };
        }

        private static ExcelExportSheet SalesDailySheet(IReadOnlyList<SalesDailyReportRow> daily) => new()
        {
            Name = "المبيعات",
            Columns = new[]
            {
                Column<SalesDailyReportRow>("day", "التاريخ", row => row.Day, ExcelExportValueType.Date, 14),
                Column<SalesDailyReportRow>("count", "عدد الفواتير", row => row.InvoiceCount, ExcelExportValueType.Integer, 16),
                Column<SalesDailyReportRow>("total", "الإجمالي (ج.م)", row => row.Total, ExcelExportValueType.Money, 18),
                Column<SalesDailyReportRow>("discount", "الخصم (ج.م)", row => row.Discount, ExcelExportValueType.Money, 18),
                Column<SalesDailyReportRow>("returns", "المرتجعات (ج.م)", row => row.ReturnsTotal, ExcelExportValueType.Money, 18),
                Column<SalesDailyReportRow>("net", "الصافي (ج.م)", row => row.Net, ExcelExportValueType.Money, 18)
            },
            Rows = daily.Cast<object>().ToList(),
            Totals = SumTotals("count", "total", "discount", "returns", "net")
        };

        private static ExcelExportSheet TopProductsSheet(IReadOnlyList<TopProductReportRow> rows, string name) => new()
        {
            Name = name,
            Columns = new[]
            {
                Column<TopProductReportRow>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 22, 42, true),
                Column<TopProductReportRow>("quantity", "الكمية", row => row.QuantitySold, ExcelExportValueType.Integer, 14),
                Column<TopProductReportRow>("sales", "قيمة المبيعات (ج.م)", row => row.SalesValue, ExcelExportValueType.Money, 22)
            },
            Rows = rows.Cast<object>().ToList(),
            Totals = SumTotals("quantity", "sales")
        };

        private static ExcelExportSheet CustomerSalesSheet(IReadOnlyList<CustomerSalesReportRow> rows) => new()
        {
            Name = "المبيعات حسب العميل",
            Columns = new[]
            {
                Column<CustomerSalesReportRow>("customer", "العميل", row => row.CustomerName, ExcelExportValueType.Text, 24, 42, true),
                Column<CustomerSalesReportRow>("count", "عدد الفواتير", row => row.InvoiceCount, ExcelExportValueType.Integer, 16),
                Column<CustomerSalesReportRow>("net", "صافي المبيعات (ج.م)", row => row.NetSales, ExcelExportValueType.Money, 22),
                Column<CustomerSalesReportRow>("share", "النسبة", row => row.SharePercent / 100m, ExcelExportValueType.Percentage, 14)
            },
            Rows = rows.Cast<object>().ToList(),
            Totals = SumTotals("count", "net", "share")
        };

        private static ExcelExportSheet ExpenseCategoriesSheet(ExpenseReportModel report) => new()
        {
            Name = "مصروفات حسب التصنيف",
            Columns = new[]
            {
                Column<ExpenseCategoryReportRow>("category", "التصنيف", row => row.CategoryName, ExcelExportValueType.Text, 22, 42, true),
                Column<ExpenseCategoryReportRow>("count", "عدد المصروفات", row => row.Count, ExcelExportValueType.Integer, 18),
                Column<ExpenseCategoryReportRow>("total", "الإجمالي (ج.م)", row => row.Total, ExcelExportValueType.Money, 18),
                Column<ExpenseCategoryReportRow>("share", "النسبة", row => row.SharePercent / 100m, ExcelExportValueType.Percentage, 14)
            },
            Rows = report.Categories.Cast<object>().ToList(),
            Totals = SumTotals("count", "total", "share")
        };

        private static ExcelExportSheet ExpenseDetailsSheet(ExpenseReportModel report) => new()
        {
            Name = "تفاصيل المصروفات",
            Columns = new[]
            {
                Column<ExpenseReportDetailRow>("date", "التاريخ", row => row.ExpenseDate, ExcelExportValueType.Date, 14),
                Column<ExpenseReportDetailRow>("category", "التصنيف", row => row.CategoryName, ExcelExportValueType.Text, 18, 32),
                Column<ExpenseReportDetailRow>("description", "الوصف", row => row.Description, ExcelExportValueType.Text, 22, 42, true),
                Column<ExpenseReportDetailRow>("amount", "المبلغ (ج.م)", row => row.Amount, ExcelExportValueType.Money, 18),
                Column<ExpenseReportDetailRow>("payment", "طريقة الدفع", row => row.PaymentMethod, ExcelExportValueType.Text, 16),
                Column<ExpenseReportDetailRow>("reference", "المرجع", row => row.ReferenceNumber, ExcelExportValueType.Text, 16),
                Column<ExpenseReportDetailRow>("user", "المستخدم", row => row.CreatedBy, ExcelExportValueType.Text, 16)
            },
            Rows = report.Details.Cast<object>().ToList(),
            Totals = SumTotals("amount")
        };

        private static ExcelExportSheet PurchaseDailySheet(PurchaseReportModel report) => new()
        {
            Name = "المشتريات",
            Columns = new[]
            {
                Column<PurchaseDailyReportRow>("day", "التاريخ", row => row.Day, ExcelExportValueType.Date, 14),
                Column<PurchaseDailyReportRow>("count", "عدد الفواتير", row => row.InvoiceCount, ExcelExportValueType.Integer, 16),
                Column<PurchaseDailyReportRow>("total", "الإجمالي (ج.م)", row => row.Total, ExcelExportValueType.Money, 18)
            },
            Rows = report.Daily.Cast<object>().ToList(),
            Totals = SumTotals("count", "total")
        };

        private static ExcelExportSheet SupplierPurchaseSheet(PurchaseReportModel report) => new()
        {
            Name = "المشتريات حسب المورد",
            Columns = new[]
            {
                Column<SupplierPurchaseReportRow>("supplier", "المورد", row => row.SupplierName, ExcelExportValueType.Text, 24, 42, true),
                Column<SupplierPurchaseReportRow>("count", "عدد الفواتير", row => row.InvoiceCount, ExcelExportValueType.Integer, 16),
                Column<SupplierPurchaseReportRow>("total", "الإجمالي (ج.م)", row => row.Total, ExcelExportValueType.Money, 18),
                Column<SupplierPurchaseReportRow>("share", "النسبة", row => row.SharePercent / 100m, ExcelExportValueType.Percentage, 14)
            },
            Rows = report.BySupplier.Cast<object>().ToList(),
            Totals = SumTotals("count", "total", "share")
        };

        private static ExcelExportSheet PurchaseTopProductsSheet(PurchaseReportModel report) => new()
        {
            Name = "أفضل أصناف المشتريات",
            Columns = new[]
            {
                Column<TopProductReportRow>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 22, 42, true),
                Column<TopProductReportRow>("quantity", "الكمية", row => row.QuantitySold, ExcelExportValueType.Integer, 14),
                Column<TopProductReportRow>("total", "قيمة المشتريات (ج.م)", row => row.SalesValue, ExcelExportValueType.Money, 22)
            },
            Rows = report.TopProducts.Cast<object>().ToList(),
            Totals = SumTotals("quantity", "total")
        };

        private static ExcelExportSheet InventoryValuationSheet(InventoryValuationModel report) => new()
        {
            Name = "قيمة المخزون",
            Filters = $"الحالة الحالية وقت التصدير: {report.GeneratedAt:dd/MM/yyyy HH:mm} (ليست مرتبطة بالفترة التاريخية)",
            Columns = new[]
            {
                Column<InventoryValuationRow>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 22, 42, true),
                Column<InventoryValuationRow>("barcode", "الباركود", row => row.Barcode, ExcelExportValueType.Text, 18),
                Column<InventoryValuationRow>("category", "التصنيف", row => row.Category, ExcelExportValueType.Text, 18),
                Column<InventoryValuationRow>("quantity", "الكمية", row => row.Quantity, ExcelExportValueType.Integer, 12),
                Column<InventoryValuationRow>("cost", "التكلفة الحالية (ج.م)", row => row.CostPrice, ExcelExportValueType.Money, 20),
                Column<InventoryValuationRow>("value", "قيمة المخزون (ج.م)", row => row.StockValue, ExcelExportValueType.Money, 20),
                Column<InventoryValuationRow>("lastSale", "آخر بيع", row => row.LastSaleDate, ExcelExportValueType.Date, 14)
            },
            Rows = report.Rows.Cast<object>().ToList(),
            Totals = SumTotals("quantity", "value")
        };

        private static ExcelExportSheet CustomerDebtSheet(CustomerDebtReportModel report) => new()
        {
            Name = "ديون العملاء",
            Columns = new[]
            {
                Column<CustomerDebtRow>("customer", "العميل", row => row.CustomerName, ExcelExportValueType.Text, 22, 42, true),
                Column<CustomerDebtRow>("phone", "الهاتف", row => row.Phone, ExcelExportValueType.Text, 18),
                Column<CustomerDebtRow>("balance", "الرصيد المستحق (ج.م)", row => row.Balance, ExcelExportValueType.Money, 22),
                Column<CustomerDebtRow>("lastPayment", "آخر دفعة", row => row.LastPaymentDate, ExcelExportValueType.Date, 14),
                Column<CustomerDebtRow>("oldestInvoice", "أقدم فاتورة غير مسددة", row => row.OldestUnpaidInvoiceDate, ExcelExportValueType.Date, 22)
            },
            Rows = report.Rows.Cast<object>().ToList(),
            Totals = SumTotals("balance")
        };

        private static ExcelExportSheet ItemProfitabilitySheet(ItemProfitabilityModel report)
        {
            var sheet = new ExcelExportSheet
            {
                Name = "ربحية الأصناف",
                Columns = new[]
                {
                    Column<ItemProfitabilityRow>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 22, 42, true),
                    Column<ItemProfitabilityRow>("quantity", "الكمية المباعة", row => row.QuantitySold, ExcelExportValueType.Integer, 16),
                    Column<ItemProfitabilityRow>("sales", "قيمة البيع (ج.م)", row => row.SalesValue, ExcelExportValueType.Money, 20),
                    Column<ItemProfitabilityRow>("cost", "التكلفة (ج.م)", row => row.Cost, ExcelExportValueType.Money, 18),
                    Column<ItemProfitabilityRow>("profit", "الربح (ج.م)", row => row.Profit, ExcelExportValueType.Money, 18),
                    Column<ItemProfitabilityRow>("margin", "هامش الربح", row => row.MarginPercent / 100m, ExcelExportValueType.Percentage, 16)
                },
                Rows = report.Rows.Cast<object>().ToList(),
                Totals = SumTotals("quantity", "sales", "cost", "profit"),
                SummaryRows = report.IsCogsEstimated
                    ? new[]
                    {
                        new ExcelExportSummaryRow
                        {
                            Label = $"التكلفة تقديرية: {report.EstimatedLineCount} بند في {report.EstimatedInvoiceCount} فاتورة قديمة",
                            FillColor = "#FFF7ED",
                            FontColor = "#9A3412"
                        }
                    }
                    : Array.Empty<ExcelExportSummaryRow>()
            };
            return sheet;
        }

        private static IReadOnlyDictionary<string, ExcelExportTotalType> SumTotals(params string[] keys) =>
            keys.ToDictionary(key => key, _ => ExcelExportTotalType.Sum);

        private static ExcelExportSummaryRow Summary(string label, params (string Key, object? Value)[] values) =>
            new()
            {
                Label = label,
                Values = values.ToDictionary(value => value.Key, value => value.Value)
            };

        private static ExcelExportColumn Column<T>(
            string key,
            string header,
            Func<T, object?> value,
            ExcelExportValueType type,
            double minimumWidth,
            double maximumWidth = 28,
            bool wrap = false) where T : class => new()
        {
            Key = key,
            Header = header,
            Value = row => value((T)row),
            Type = type,
            MinimumWidth = minimumWidth,
            MaximumWidth = maximumWidth,
            WrapText = wrap
        };

        private static ExcelExportWorkbook Workbook(string shopName, string title, string filters, params ExcelExportSheet[] sheets) => new()
        {
            ShopName = shopName,
            Title = title,
            Filters = filters,
            ExportedAt = DateTime.Now,
            Sheets = sheets
        };

        private sealed class CustomerRow
        {
            public string Name { get; init; } = string.Empty;
            public string Phone { get; init; } = string.Empty;
            public string Address { get; init; } = string.Empty;
            public decimal Balance { get; init; }
            public string Status { get; init; } = string.Empty;
            public string BalanceStatus { get; init; } = string.Empty;
        }

        private sealed class ExpenseRow
        {
            public DateTime Date { get; init; }
            public string Category { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public decimal Amount { get; init; }
            public string PaymentMethod { get; init; } = string.Empty;
            public string Reference { get; init; } = string.Empty;
            public string User { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
            public string Note { get; init; } = string.Empty;
        }

        private sealed class ExpenseCategoryExportRow
        {
            public string Category { get; init; } = string.Empty;
            public int Count { get; init; }
            public decimal Total { get; init; }
            public decimal Share { get; set; }
        }

        private sealed class BlankStocktakeRow
        {
            public int Serial { get; init; }
            public string Barcode { get; init; } = string.Empty;
            public string Product { get; init; } = string.Empty;
            public string Category { get; init; } = string.Empty;
            public int BookQuantity { get; init; }
        }

        private sealed class ProfitRow
        {
            public string Label { get; init; } = string.Empty;
            public decimal? Current { get; init; }
            public decimal? Previous { get; init; }
            public decimal? Change { get; init; }
            public decimal? Percent { get; init; }
            public string Notes { get; init; } = string.Empty;
        }
    }
}
