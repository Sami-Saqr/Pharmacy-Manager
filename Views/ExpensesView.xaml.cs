using System.Windows;
using System.Windows.Controls;
using PharmacyManager.ViewModels;

namespace PharmacyManager.Views
{
    /// <summary>
    /// Interaction logic for ExpensesView.xaml
    /// </summary>
    public partial class ExpensesView : UserControl
    {
        public ExpensesView()
        {
            InitializeComponent();
        }

        private async void ExportExpenses_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ExpensesViewModel viewModel || Window.GetWindow(this) is not { } owner)
                return;

            var fileName = $"المصروفات_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportExpensesAsync, () => viewModel.ExportErrorMessage);
        }
    }
}
