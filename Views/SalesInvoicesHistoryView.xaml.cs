using System.Windows;
using System.Windows.Controls;
using CarPartManager.ViewModels;

namespace CarPartManager.Views
{
    public partial class SalesInvoicesHistoryView : UserControl
    {
        public SalesInvoicesHistoryView()
        {
            InitializeComponent();
        }

        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SalesInvoicesHistoryViewModel viewModel || Window.GetWindow(this) is not { } owner)
            {
                return;
            }

            var fileName = $"فواتير_المبيعات_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportAsync, () => viewModel.ExportErrorMessage);
        }
    }
}
