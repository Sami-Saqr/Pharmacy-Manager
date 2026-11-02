namespace PharmacyManager.Services
{
    public enum ExcelExportValueType
    {
        Text,
        Integer,
        Decimal,
        Money,
        Date,
        DateTime,
        Percentage
    }

    public enum ExcelExportTotalType
    {
        Count,
        Sum
    }

    public sealed class ExcelExportColumn
    {
        public string Key { get; init; } = string.Empty;
        public string Header { get; init; } = string.Empty;
        public Func<object, object?> Value { get; init; } = _ => null;
        public ExcelExportValueType Type { get; init; } = ExcelExportValueType.Text;
        public double MinimumWidth { get; init; } = 10;
        public double MaximumWidth { get; init; } = 42;
        public bool WrapText { get; init; }
        public Func<object, int, string?>? Formula { get; init; }
    }

    public sealed class ExcelExportHighlight
    {
        public string ColumnKey { get; init; } = string.Empty;
        public string MatchValue { get; init; } = string.Empty;
        public string FillColor { get; init; } = "FFFEE2E2";
        public string FontColor { get; init; } = "FF475569";
        public bool Strikeout { get; init; }
        public bool GreaterThanZero { get; init; }
    }

    public sealed class ExcelExportSummaryRow
    {
        public string Label { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?>();
        public IReadOnlyDictionary<string, string> Formulas { get; init; } = new Dictionary<string, string>();
        public string? FillColor { get; init; }
        public string? FontColor { get; init; }
        public bool Bold { get; init; } = true;
    }

    public sealed class ExcelExportSheet
    {
        public string Name { get; init; } = string.Empty;
        public string Filters { get; init; } = string.Empty;
        public IReadOnlyList<ExcelExportColumn> Columns { get; init; } = Array.Empty<ExcelExportColumn>();
        public IReadOnlyList<object> Rows { get; init; } = Array.Empty<object>();
        public IReadOnlyDictionary<string, ExcelExportTotalType> Totals { get; init; } =
            new Dictionary<string, ExcelExportTotalType>();
        public IReadOnlyList<ExcelExportHighlight> Highlights { get; init; } = Array.Empty<ExcelExportHighlight>();
        public IReadOnlyDictionary<string, Func<int, int, int, string>> TotalFormulas { get; init; } =
            new Dictionary<string, Func<int, int, int, string>>();
        public IReadOnlyList<ExcelExportSummaryRow> SummaryRows { get; init; } = Array.Empty<ExcelExportSummaryRow>();
        public double DataRowHeight { get; init; } = 30;
    }

    public sealed class ExcelExportWorkbook
    {
        public string ShopName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Filters { get; init; } = string.Empty;
        public DateTime ExportedAt { get; init; } = DateTime.Now;
        public bool ConfidentialFooter { get; init; }
        public IReadOnlyList<ExcelExportSheet> Sheets { get; init; } = Array.Empty<ExcelExportSheet>();
    }
}
