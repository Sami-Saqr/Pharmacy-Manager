using System.IO;
using ClosedXML.Excel;

namespace CarPartManager.Services
{
    public sealed class ExcelExportService : IExcelExportService
    {
        private const int HeaderRow = 6;
        private const int FirstDataRow = HeaderRow + 1;

        public Task ExportAsync(
            ExcelExportWorkbook workbook,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(workbook);
            ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
            if (workbook.Sheets.Count == 0)
            {
                throw new ArgumentException("يجب تعريف ورقة واحدة على الأقل للتصدير.", nameof(workbook));
            }

            return Task.Run(() => WriteAtomically(workbook, destinationPath, cancellationToken), cancellationToken);
        }

        private static void WriteAtomically(ExcelExportWorkbook workbook, string destinationPath, CancellationToken cancellationToken)
        {
            var fullPath = Path.GetFullPath(destinationPath);
            var directory = Path.GetDirectoryName(fullPath)
                ?? throw new IOException("تعذر تحديد مجلد حفظ الملف.");
            Directory.CreateDirectory(directory);

            var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp.xlsx");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var output = new XLWorkbook())
                {
                    output.Properties.Title = workbook.Title;
                    output.Properties.Author = "CarPartManager";
                    foreach (var sheet in workbook.Sheets)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        WriteSheet(output, workbook, sheet, cancellationToken);
                    }

                    output.SaveAs(temporaryPath);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

        }

        private static string GetNumberFormat(ExcelExportValueType type) => type switch
        {
            ExcelExportValueType.Text => "@",
            ExcelExportValueType.Integer => "#,##0",
            ExcelExportValueType.Decimal => "#,##0.##",
            ExcelExportValueType.Money => "#,##0.00",
            ExcelExportValueType.Date => "dd/MM/yyyy",
            ExcelExportValueType.DateTime => "dd/MM/yyyy HH:mm",
            ExcelExportValueType.Percentage => "0.00%",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "نوع خلية Excel غير معروف.")
        };

        private static void WriteSheet(
            XLWorkbook workbook,
            ExcelExportWorkbook definition,
            ExcelExportSheet sheet,
            CancellationToken cancellationToken)
        {
            if (sheet.Columns.Count == 0)
            {
                throw new ArgumentException($"ورقة التصدير '{sheet.Name}' لا تحتوي على أعمدة.");
            }

            var worksheet = workbook.Worksheets.Add(SanitizeSheetName(sheet.Name));
            worksheet.RightToLeft = true;
            worksheet.Style.Font.FontName = "Tahoma";
            worksheet.Style.Font.FontSize = 11;
            worksheet.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
            worksheet.PageSetup.PagesWide = 1;
            worksheet.PageSetup.PagesTall = 0;
            worksheet.PageSetup.CenterHorizontally = true;
            worksheet.PageSetup.Margins.Left = 0.3;
            worksheet.PageSetup.Margins.Right = 0.3;
            worksheet.PageSetup.Margins.Top = 0.5;
            worksheet.PageSetup.Margins.Bottom = 0.5;
            worksheet.PageSetup.Footer.Center.AddText("صفحة &P من &N");
            worksheet.PageSetup.Footer.Right.AddText("&A");
            worksheet.PageSetup.SetRowsToRepeatAtTop(HeaderRow, HeaderRow);

            for (var row = 1; row <= 4; row++)
            {
                worksheet.Range(row, 1, row, sheet.Columns.Count).Merge();
            }
            SetCellValue(worksheet.Cell(1, 1), definition.ShopName, ExcelExportValueType.Text);
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            SetCellValue(worksheet.Cell(2, 1), $"{definition.Title} — {sheet.Name}", ExcelExportValueType.Text);
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 12;
            SetCellValue(
                worksheet.Cell(3, 1),
                string.IsNullOrWhiteSpace(sheet.Filters) ? definition.Filters : sheet.Filters,
                ExcelExportValueType.Text);
            SetCellValue(worksheet.Cell(4, 1), $"تاريخ ووقت التصدير: {definition.ExportedAt:dd/MM/yyyy HH:mm}", ExcelExportValueType.Text);
            worksheet.Row(1).Height = 24;
            worksheet.Row(2).Height = 22;
            worksheet.Row(3).Height = 20;
            worksheet.Row(4).Height = 20;
            worksheet.Row(5).Height = 8;

            for (var index = 0; index < sheet.Columns.Count; index++)
            {
                var column = sheet.Columns[index];
                var header = worksheet.Cell(HeaderRow, index + 1);
                header.SetValue(column.Header);
                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                header.Style.Alignment.WrapText = true;
                header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                header.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            }
            worksheet.Row(HeaderRow).Height = 32;

            var lastDataRow = Math.Max(FirstDataRow, FirstDataRow + sheet.Rows.Count - 1);
            var dataRange = worksheet.Range(FirstDataRow, 1, lastDataRow, sheet.Columns.Count);
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            dataRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
            worksheet.Rows(FirstDataRow, lastDataRow).Height = 30;

            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var columnRange = worksheet.Range(FirstDataRow, columnIndex + 1, lastDataRow, columnIndex + 1);
                var column = sheet.Columns[columnIndex];
                columnRange.Style.Alignment.WrapText = column.WrapText;
                if (column.Type is ExcelExportValueType.Integer or ExcelExportValueType.Decimal or ExcelExportValueType.Money or ExcelExportValueType.Percentage)
                {
                    columnRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                columnRange.Style.NumberFormat.Format = GetNumberFormat(column.Type);
            }

            for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = sheet.Rows[rowIndex];
                var excelRow = FirstDataRow + rowIndex;

                for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
                {
                    var column = sheet.Columns[columnIndex];
                    var cell = worksheet.Cell(excelRow, columnIndex + 1);
                    SetCellValue(cell, column.Value(row), column.Type);
                }
            }

            worksheet.Range(HeaderRow, 1, lastDataRow, sheet.Columns.Count).SetAutoFilter();
            worksheet.SheetView.FreezeRows(HeaderRow);

            if (sheet.Rows.Count > 0 && sheet.Totals.Count > 0)
            {
                WriteTotals(worksheet, sheet, lastDataRow);
            }

            ApplyHighlights(worksheet, sheet, lastDataRow);
            SetColumnWidths(worksheet, sheet);
        }

        private static void SetCellValue(IXLCell cell, object? value, ExcelExportValueType type)
        {
            if (value is null)
            {
                cell.Clear(XLClearOptions.Contents);
                return;
            }

            switch (type)
            {
                case ExcelExportValueType.Text:
                    cell.SetValue(SanitizeText(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty));
                    break;
                case ExcelExportValueType.Integer:
                    cell.SetValue(Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case ExcelExportValueType.Decimal:
                    cell.SetValue(Convert.ToDouble(Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture)));
                    break;
                case ExcelExportValueType.Money:
                    cell.SetValue(Convert.ToDouble(Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture)));
                    break;
                case ExcelExportValueType.Date:
                    cell.SetValue(Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case ExcelExportValueType.DateTime:
                    cell.SetValue(Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case ExcelExportValueType.Percentage:
                    cell.SetValue(Convert.ToDouble(Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "نوع خلية Excel غير معروف.");
            }
        }

        private static void WriteTotals(IXLWorksheet worksheet, ExcelExportSheet sheet, int lastDataRow)
        {
            var totalsRow = lastDataRow + 1;
            var countColumn = sheet.Columns.FirstOrDefault(column => column.Type == ExcelExportValueType.Text);
            if (countColumn != null)
            {
                var countIndex = FindColumnIndex(sheet, countColumn.Key);
                var countLetter = XLHelper.GetColumnLetterFromNumber(countIndex + 1);
                worksheet.Cell(totalsRow, countIndex + 1).FormulaA1 =
                    $"SUBTOTAL(103,{countLetter}{FirstDataRow}:{countLetter}{lastDataRow})";
            }
            else
            {
                worksheet.Cell(totalsRow, 1).SetValue("الإجمالي");
            }

            foreach (var total in sheet.Totals)
            {
                var columnIndex = FindColumnIndex(sheet, total.Key);
                if (columnIndex < 0)
                {
                    throw new ArgumentException($"عمود الإجمالي غير موجود: {total.Key}");
                }

                var letter = XLHelper.GetColumnLetterFromNumber(columnIndex + 1);
                var formula = total.Value == ExcelExportTotalType.Count
                    ? $"SUBTOTAL(103,{letter}{FirstDataRow}:{letter}{lastDataRow})"
                    : $"SUBTOTAL(109,{letter}{FirstDataRow}:{letter}{lastDataRow})";
                worksheet.Cell(totalsRow, columnIndex + 1).FormulaA1 = formula;
                worksheet.Cell(totalsRow, columnIndex + 1).Style.NumberFormat.Format =
                    total.Value == ExcelExportTotalType.Count ? "#,##0" : "#,##0.00";
            }

            var totalRange = worksheet.Range(totalsRow, 1, totalsRow, sheet.Columns.Count);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            totalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#64748B");
        }

        private static void ApplyHighlights(IXLWorksheet worksheet, ExcelExportSheet sheet, int lastDataRow)
        {
            foreach (var highlight in sheet.Highlights)
            {
                var columnIndex = FindColumnIndex(sheet, highlight.ColumnKey);
                if (columnIndex < 0 || lastDataRow < FirstDataRow)
                {
                    continue;
                }

                var range = worksheet.Range(FirstDataRow, 1, lastDataRow, sheet.Columns.Count);
                var formula = $"${XLHelper.GetColumnLetterFromNumber(columnIndex + 1)}{FirstDataRow}=\"{highlight.MatchValue.Replace("\"", "\"\"")}\"";
                var conditional = range.AddConditionalFormat().WhenIsTrue(formula);
                conditional.Fill.BackgroundColor = XLColor.FromHtml("#" + highlight.FillColor.TrimStart('#'));
                conditional.Font.FontColor = XLColor.FromHtml("#" + highlight.FontColor.TrimStart('#'));
                conditional.Font.Strikethrough = highlight.Strikeout;
            }
        }

        private static void SetColumnWidths(IXLWorksheet worksheet, ExcelExportSheet sheet)
        {
            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var column = sheet.Columns[columnIndex];
                var longest = column.Header.Length;
                foreach (var row in sheet.Rows)
                {
                    var text = Convert.ToString(column.Value(row), System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
                    longest = Math.Max(longest, text.Length);
                }

                var width = Math.Clamp(longest * 1.15 + 2, column.MinimumWidth, column.MaximumWidth);
                worksheet.Column(columnIndex + 1).Width = width;
                if (column.WrapText)
                {
                    worksheet.Column(columnIndex + 1).Style.Alignment.WrapText = true;
                }
            }
        }

        private static string SanitizeText(string value)
        {
            if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            {
                return "'" + value;
            }
            return value;
        }

        private static int FindColumnIndex(ExcelExportSheet sheet, string key)
        {
            for (var index = 0; index < sheet.Columns.Count; index++)
            {
                if (string.Equals(sheet.Columns[index].Key, key, StringComparison.Ordinal))
                {
                    return index;
                }
            }
            return -1;
        }

        private static string SanitizeSheetName(string name)
        {
            var invalid = new[] { '[', ']', ':', '*', '?', '/', '\\' };
            var safeName = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return safeName.Length > 31 ? safeName[..31] : safeName;
        }
    }
}
