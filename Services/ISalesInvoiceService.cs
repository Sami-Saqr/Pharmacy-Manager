using PharmacyManager.Models;

namespace PharmacyManager.Services
{
    /// <summary>
    /// واجهة خدمة فواتير المبيعات ونقاط البيع وخصم المخزون
    /// </summary>
    public interface ISalesInvoiceService
    {
        Task<string> GenerateNextInvoiceNumberAsync();
        Task<string> GenerateInvoiceNumberAsync();
        Task<IEnumerable<SalesInvoiceModel>> GetAllInvoicesAsync();
        Task<IEnumerable<SalesInvoiceModel>> GetInvoiceSummariesAsync();
        Task<IReadOnlyList<InvoiceExportRecord>> GetInvoicesForExportAsync(string? searchQuery);
        Task<SalesInvoiceModel?> GetInvoiceByIdAsync(int id);
        Task<SalesInvoiceModel> SaveDraftInvoiceAsync(SalesInvoiceModel invoice);
        Task<SalesInvoiceModel> PostInvoiceAsync(SalesInvoiceModel invoice);
        Task<SalesInvoiceModel> PostInvoiceAsync(int invoiceId);
        Task<IEnumerable<SalesInvoiceModel>> SearchInvoicesAsync(string query);
        Task<bool> CancelInvoiceAsync(int invoiceId, string reason);
    }
}
