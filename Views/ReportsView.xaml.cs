using System.Windows;
using System.Windows.Controls;
using CarPartManager.ViewModels;

namespace CarPartManager.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private async void ExportReports_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ReportsViewModel viewModel || Window.GetWindow(this) is not { } owner)
                return;

            var fileName = $"التقارير_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportReportsAsync, () => viewModel.ExportErrorMessage);
        }
    }
}
