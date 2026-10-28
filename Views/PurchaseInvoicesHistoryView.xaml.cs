using System.Windows;
using System.Windows.Controls;
using CarPartManager.ViewModels;

namespace CarPartManager.Views
{
    public partial class PurchaseInvoicesHistoryView : UserControl
    {
        public PurchaseInvoicesHistoryView()
        {
            InitializeComponent();
        }

        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PurchaseInvoicesHistoryViewModel viewModel || Window.GetWindow(this) is not { } owner)
            {
                return;
            }

            var fileName = $"فواتير_المشتريات_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportAsync, () => viewModel.ExportErrorMessage);
        }
    }
}
