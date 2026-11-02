using System.Collections.ObjectModel;
using System.Windows;
using PharmacyManager.Models;
using PharmacyManager.MVVM;
using PharmacyManager.Services;

namespace PharmacyManager.ViewModels
{
    /// <summary>
    /// نموذج عرض صفحة سجل فواتير المبيعات السابقة المستقلة.
    /// يُحمِّل جميع الفواتير من قاعدة البيانات ويدعم البحث والتصفية وعرض التفاصيل.
    /// </summary>
    public class SalesInvoicesHistoryViewModel : ExcelExportViewModelBase
    {
        private readonly ISalesInvoiceService _invoiceService;
        private readonly List<SalesInvoiceModel> _allInvoices = new();

        // ── قوائم البيانات ──────────────────────────────────────────
        public ObservableCollection<SalesInvoiceModel> InvoicesHistory { get; } = new();

        // ── حالة التحميل ────────────────────────────────────────────
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                    OnPropertyChanged(nameof(ShowEmptyState));
            }
        }

        public bool ShowEmptyState => !IsLoading && InvoicesHistory.Count == 0;

        // ── البحث والتصفية ──────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    FilterInvoicesHistory();
            }
        }

        // ── رسالة الحالة ────────────────────────────────────────────
        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ── الفاتورة المحددة للمعاينة ───────────────────────────────
        private SalesInvoiceModel? _selectedInvoice;
        public SalesInvoiceModel? SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        // ── الأوامر ─────────────────────────────────────────────────
        public RelayCommand RefreshCommand { get; }
        public RelayCommand ViewInvoiceCommand { get; }
        public override bool CanExport => base.CanExport && InvoicesHistory.Count > 0;
        public override string ExportButtonToolTip => CanExport ? "تصدير سجل المبيعات الظاهر" : "لا توجد بيانات للتصدير";

        public SalesInvoicesHistoryViewModel(
            ISalesInvoiceService? invoiceService = null,
            IExcelExportService? excelExportService = null)
            : base(excelExportService)
        {
            _invoiceService = invoiceService ?? new SqliteSalesInvoiceService();

            RefreshCommand  = new RelayCommand(async () => await LoadAllDataAsync());
            ViewInvoiceCommand = new RelayCommand(p =>
            {
                if (p is SalesInvoiceModel inv)
                    SelectedInvoice = inv;
            });
        }

        // ── تحميل البيانات (يُستدعى من MainViewModel عند كل تنقل) ──
        public async Task LoadAllDataAsync()
        {
            IsLoading = true;
            StatusMessage = "جارٍ تحميل سجل فواتير المبيعات...";
            InvoicesHistory.Clear();
            OnPropertyChanged(nameof(ShowEmptyState));

            try
            {
                var invoices = await _invoiceService.GetInvoiceSummariesAsync();
                _allInvoices.Clear();
                foreach (var inv in invoices)
                {
                    // الفواتير القديمة أو فواتير البيع المباشر (Walk-in) قد يكون اسم العميل فيها فارغاً
                    if (string.IsNullOrWhiteSpace(inv.CustomerName))
                        inv.CustomerName = "عميل نقدي";
                    _allInvoices.Add(inv);
                }
                FilterInvoicesHistory();
                StatusMessage = $"تم تحميل {_allInvoices.Count} فاتورة مبيعات.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل سجل فواتير المبيعات: {Describe(ex)}";
                MessageBox.Show(StatusMessage, "خطأ في التحميل", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }

        /// <summary>
        /// تجميع نص الخطأ مع الأخطاء الداخلية (مثل أخطاء SQLite الفعلية) لعرض السبب الحقيقي بوضوح.
        /// </summary>
        private static string Describe(Exception ex)
        {
            var messages = new List<string>();
            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                var message = current.Message?.Trim();
                if (!string.IsNullOrWhiteSpace(message) && !messages.Contains(message))
                {
                    messages.Add(message);
                }
            }
            return string.Join(" | ", messages);
        }

        private void FilterInvoicesHistory()
        {
            InvoicesHistory.Clear();
            var source = string.IsNullOrWhiteSpace(SearchText)
                ? _allInvoices
                : _allInvoices.Where(inv =>
                    inv.InvoiceNumber.ToLower().Contains(SearchText.Trim().ToLower()) ||
                    (inv.CustomerName != null && inv.CustomerName.ToLower().Contains(SearchText.Trim().ToLower())) ||
                    (inv.CustomerPhone != null && inv.CustomerPhone.ToLower().Contains(SearchText.Trim().ToLower())) ||
                    (inv.Notes != null && inv.Notes.ToLower().Contains(SearchText.Trim().ToLower()))
                  ).ToList();

            foreach (var inv in source) InvoicesHistory.Add(inv);
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(CanExport));
            OnPropertyChanged(nameof(ExportButtonToolTip));
        }

        public Task<bool> ExportAsync(string destinationPath, string shopName) =>
            ExportWorkbookAsync(async () =>
            {
                var invoices = await _invoiceService.GetInvoicesForExportAsync(SearchText);
                var filters = string.IsNullOrWhiteSpace(SearchText)
                    ? "المرشح: جميع الفواتير"
                    : $"البحث: {SearchText.Trim()}";
                return ExcelExportMappers.SalesInvoices(shopName, filters, invoices);
            }, destinationPath);
    }
}
