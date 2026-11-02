namespace PharmacyManager.Services
{
    public static class ExcelExportMappers
    {
        public static ExcelExportWorkbook InventoryStock(
            string shopName,
            string filters,
            IEnumerable<ProductStockInfo> products)
        {
            var rows = products.Cast<object>().ToList();
            return Workbook(shopName, "تقرير المخزون", filters, new ExcelExportSheet
            {
                Name = "المخزون",
                Columns = new[]
                {
                    Column<ProductStockInfo>("barcode", "الباركود", row => row.Barcode, ExcelExportValueType.Text, 14),
                    Column<ProductStockInfo>("name", "الصنف", row => row.Name, ExcelExportValueType.Text, 18, 42, true),
                    Column<ProductStockInfo>("category", "التصنيف", row => row.Category, ExcelExportValueType.Text, 12, 24, true),
                    Column<ProductStockInfo>("quantity", "الكمية", row => row.Quantity, ExcelExportValueType.Integer, 10),
                    Column<ProductStockInfo>("minimum", "حد الطلب", row => row.MinimumStock, ExcelExportValueType.Integer, 10),
                    Column<ProductStockInfo>("cost", "سعر التكلفة (ج.م)", row => row.CostPrice, ExcelExportValueType.Money, 16),
                    Column<ProductStockInfo>("selling", "سعر البيع (ج.م)", row => row.SellingPrice, ExcelExportValueType.Money, 16),
                    Column<ProductStockInfo>("stockValue", "قيمة المخزون (ج.م)", row => row.StockValue, ExcelExportValueType.Money, 18),
                    Column<ProductStockInfo>("status", "الحالة", row => row.Status, ExcelExportValueType.Text, 16)
                },
                Rows = rows,
                Totals = new Dictionary<string, ExcelExportTotalType>
                {
                    ["barcode"] = ExcelExportTotalType.Count,
                    ["quantity"] = ExcelExportTotalType.Sum,
                    ["stockValue"] = ExcelExportTotalType.Sum
                },
                Highlights = new[]
                {
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "نفد من المخزن", FillColor = "FFFEF2F2", FontColor = "FFB91C1C" },
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "منخفض (حرج)", FillColor = "FFFFF7ED", FontColor = "FFC2410C" }
                }
            });
        }

        public static ExcelExportWorkbook InventoryMovements(
            string shopName,
            string filters,
            IEnumerable<Models.InventoryMovementModel> movements)
        {
            var rows = movements.Cast<object>().ToList();
            return Workbook(shopName, "تقرير حركة المخزون", filters, new ExcelExportSheet
            {
                Name = "حركة المخزون",
                Columns = new[]
                {
                    Column<Models.InventoryMovementModel>("date", "التاريخ والوقت", row => row.MovementDate, ExcelExportValueType.DateTime, 18),
                    Column<Models.InventoryMovementModel>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 18, 42, true),
                    Column<Models.InventoryMovementModel>("type", "نوع الحركة", row => row.MovementType, ExcelExportValueType.Text, 16),
                    Column<Models.InventoryMovementModel>("quantity", "تغيير الكمية", row => row.QuantityChange, ExcelExportValueType.Integer, 14),
                    Column<Models.InventoryMovementModel>("reference", "المرجع", row => row.ReferenceNumber, ExcelExportValueType.Text, 16),
                    Column<Models.InventoryMovementModel>("note", "ملاحظات", row => row.Notes, ExcelExportValueType.Text, 14, 42, true)
                },
                Rows = rows,
                Totals = new Dictionary<string, ExcelExportTotalType>
                {
                    ["product"] = ExcelExportTotalType.Count,
                    ["quantity"] = ExcelExportTotalType.Sum
                }
            });
        }

        public static ExcelExportWorkbook SalesInvoices(string shopName, string filters, IReadOnlyList<InvoiceExportRecord> invoices)
        {
            var invoiceRows = invoices.Cast<object>().ToList();
            var lineRows = invoices.SelectMany(invoice => invoice.Items).Cast<object>().ToList();
            return Workbook(
                shopName,
                "تقرير فواتير المبيعات",
                filters,
                InvoiceSheet("فواتير المبيعات", "العميل", invoiceRows, true),
                LinesSheet(lineRows));
        }

        public static ExcelExportWorkbook PurchaseInvoices(string shopName, string filters, IReadOnlyList<InvoiceExportRecord> invoices)
        {
            var invoiceRows = invoices.Cast<object>().ToList();
            var lineRows = invoices.SelectMany(invoice => invoice.Items).Cast<object>().ToList();
            return Workbook(
                shopName,
                "تقرير فواتير المشتريات",
                filters,
                InvoiceSheet("فواتير المشتريات", "المورد", invoiceRows, false),
                LinesSheet(lineRows));
        }

        private static ExcelExportSheet InvoiceSheet(string sheetName, string partyHeader, IReadOnlyList<object> rows, bool isSales)
        {
            var columns = new List<ExcelExportColumn>
            {
                Column<InvoiceExportRecord>("invoiceNumber", "رقم الفاتورة", row => row.InvoiceNumber, ExcelExportValueType.Text, 18),
                Column<InvoiceExportRecord>("date", "التاريخ والوقت", row => row.InvoiceDate, ExcelExportValueType.DateTime, 19),
                Column<InvoiceExportRecord>("party", partyHeader, row => row.PartyName, ExcelExportValueType.Text, 18, 42, true),
                Column<InvoiceExportRecord>("items", "عدد البنود", row => row.ItemCount, ExcelExportValueType.Integer, 12),
                Column<InvoiceExportRecord>("subtotal", "الإجمالي قبل الخصم (ج.م)", row => row.Subtotal, ExcelExportValueType.Money, 20)
            };

            var totals = new Dictionary<string, ExcelExportTotalType>
            {
                ["invoiceNumber"] = ExcelExportTotalType.Count,
                ["items"] = ExcelExportTotalType.Sum,
                ["subtotal"] = ExcelExportTotalType.Sum
            };
            if (isSales)
            {
                columns.Add(Column<InvoiceExportRecord>("discount", "الخصم (ج.م)", row => row.Discount, ExcelExportValueType.Money, 14));
                columns.Add(Column<InvoiceExportRecord>("total", "الصافي (ج.م)", row => row.Total, ExcelExportValueType.Money, 16));
                columns.Add(Column<InvoiceExportRecord>("paid", "المدفوع (ج.م)", row => row.Paid, ExcelExportValueType.Money, 16));
                totals["discount"] = ExcelExportTotalType.Sum;
                totals["total"] = ExcelExportTotalType.Sum;
                totals["paid"] = ExcelExportTotalType.Sum;
            }
            else
            {
                columns.Add(Column<InvoiceExportRecord>("total", "الإجمالي (ج.م)", row => row.Total, ExcelExportValueType.Money, 16));
                totals["total"] = ExcelExportTotalType.Sum;
            }

            columns.Add(Column<InvoiceExportRecord>("status", "الحالة", row => row.Status, ExcelExportValueType.Text, 14));
            return new ExcelExportSheet
            {
                Name = sheetName,
                Columns = columns,
                Rows = rows,
                Totals = totals,
                Highlights = new[]
                {
                    new ExcelExportHighlight { ColumnKey = "status", MatchValue = "ملغاة", FillColor = "FFF1F5F9", FontColor = "FF64748B", Strikeout = true }
                }
            };
        }

        private static ExcelExportSheet LinesSheet(IReadOnlyList<object> rows) => new()
        {
            Name = "بنود الفواتير",
            Columns = new[]
            {
                Column<InvoiceExportLine>("invoiceNumber", "رقم الفاتورة", row => row.InvoiceNumber, ExcelExportValueType.Text, 18),
                Column<InvoiceExportLine>("date", "التاريخ والوقت", row => row.InvoiceDate, ExcelExportValueType.DateTime, 19),
                Column<InvoiceExportLine>("product", "الصنف", row => row.ProductName, ExcelExportValueType.Text, 18, 42, true),
                Column<InvoiceExportLine>("barcode", "الباركود", row => row.Barcode, ExcelExportValueType.Text, 16),
                Column<InvoiceExportLine>("quantity", "الكمية", row => row.Quantity, ExcelExportValueType.Integer, 10),
                Column<InvoiceExportLine>("unitPrice", "سعر الوحدة (ج.م)", row => row.UnitPrice, ExcelExportValueType.Money, 18),
                Column<InvoiceExportLine>("lineTotal", "إجمالي البند (ج.م)", row => row.LineTotal, ExcelExportValueType.Money, 18)
            },
            Rows = rows,
            Totals = new Dictionary<string, ExcelExportTotalType>
            {
                ["invoiceNumber"] = ExcelExportTotalType.Count,
                ["quantity"] = ExcelExportTotalType.Sum,
                ["lineTotal"] = ExcelExportTotalType.Sum
            }
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
            Sheets = sheets
        };
    }
}
