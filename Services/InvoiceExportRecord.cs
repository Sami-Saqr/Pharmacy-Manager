namespace CarPartManager.Services
{
    public sealed class InvoiceExportRecord
    {
        public string InvoiceNumber { get; init; } = string.Empty;
        public DateTime InvoiceDate { get; init; }
        public string PartyName { get; init; } = string.Empty;
        public int ItemCount { get; init; }
        public decimal Subtotal { get; init; }
        public decimal Discount { get; init; }
        public decimal Total { get; init; }
        public decimal? Paid { get; init; }
        public string Status { get; init; } = string.Empty;
        public IReadOnlyList<InvoiceExportLine> Items { get; init; } = Array.Empty<InvoiceExportLine>();
    }

    public sealed class InvoiceExportLine
    {
        public string InvoiceNumber { get; init; } = string.Empty;
        public DateTime InvoiceDate { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string Barcode { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
    }
}
