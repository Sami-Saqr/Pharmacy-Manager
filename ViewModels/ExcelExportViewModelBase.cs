using System.IO;
using PharmacyManager.MVVM;
using PharmacyManager.Services;

namespace PharmacyManager.ViewModels
{
    public abstract class ExcelExportViewModelBase : ViewModelBase
    {
        private readonly IExcelExportService _excelExportService;
        private bool _isExporting;
        private string _exportErrorMessage = string.Empty;

        protected ExcelExportViewModelBase(IExcelExportService? excelExportService)
        {
            _excelExportService = excelExportService ?? new ExcelExportService();
        }

        public bool IsExporting
        {
            get => _isExporting;
            private set
            {
                if (SetProperty(ref _isExporting, value))
                {
                    OnExportStateChanged();
                }
            }
        }

        public string ExportErrorMessage
        {
            get => _exportErrorMessage;
            private set => SetProperty(ref _exportErrorMessage, value);
        }

        public virtual bool CanExport => !IsExporting;
        public string ExportButtonText => IsExporting ? "جارٍ التصدير..." : "تصدير Excel";
        public virtual string ExportButtonToolTip => CanExport ? "تصدير البيانات الظاهرة" : "لا توجد بيانات للتصدير";

        protected virtual void OnExportStateChanged()
        {
            OnPropertyChanged(nameof(CanExport));
            OnPropertyChanged(nameof(ExportButtonText));
            OnPropertyChanged(nameof(ExportButtonToolTip));
        }

        protected async Task<bool> ExportWorkbookAsync(
            Func<Task<ExcelExportWorkbook>> createWorkbook,
            string destinationPath)
        {
            if (IsExporting)
            {
                return false;
            }

            IsExporting = true;
            ExportErrorMessage = string.Empty;
            try
            {
                var workbook = await Task.Run(createWorkbook);
                await _excelExportService.ExportAsync(workbook, destinationPath);
                return true;
            }
            catch (IOException ex)
            {
                ExportErrorMessage = $"تعذر حفظ الملف. قد يكون مفتوحاً أو أن المجلد غير قابل للكتابة. اختر اسماً أو مجلداً آخر.\n{ex.Message}";
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                ExportErrorMessage = $"لا توجد صلاحية للكتابة في هذا المسار. اختر مجلداً آخر.\n{ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                ExportErrorMessage = $"تعذر إنشاء ملف Excel:\n{ex.Message}";
                return false;
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
}
