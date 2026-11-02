using System.Windows;
using System.Windows.Controls;
using PharmacyManager.ViewModels;

namespace PharmacyManager.Views
{
    /// <summary>
    /// Interaction logic for CustomersView.xaml
    /// </summary>
    public partial class CustomersView : UserControl
    {
        public CustomersView()
        {
            InitializeComponent();
        }

        private async void ExportCustomers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CustomersViewModel viewModel || Window.GetWindow(this) is not { } owner)
                return;

            var fileName = $"العملاء_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportCustomersAsync, () => viewModel.ExportErrorMessage);
        }

        private async void ExportStatement_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CustomersViewModel viewModel || Window.GetWindow(this) is not { } owner)
                return;

            var fileName = $"كشف_حساب_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportStatementAsync, () => viewModel.ExportErrorMessage);
        }
    }
}
