using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CarPartManager.ViewModels;

namespace CarPartManager.Views
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
        }

        private async void ExportStock_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not InventoryViewModel viewModel || Window.GetWindow(this) is not { } owner)
            {
                return;
            }

            var fileName = $"مخزون_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportStockAsync, () => viewModel.ExportErrorMessage);
        }

        private async void ExportMovements_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not InventoryViewModel viewModel || Window.GetWindow(this) is not { } owner)
            {
                return;
            }

            var fileName = $"حركة_المخزون_{DateTime.Now:yyyy-MM-dd}.xlsx";
            await ExcelExportDialog.ExportAsync(owner, fileName, viewModel.ExportMovementsAsync, () => viewModel.ExportErrorMessage);
        }

        /// <summary>
        /// دعم قارئ الباركود: الضغط على Enter في حقل الجرد يحدّث الربط ثم يسجّل العد،
        /// ويُبقي التركيز في الحقل لمواصلة المسح تباعاً. وفي حال ظهور رسالة تحذيرية يعود التركيز للحقل بعد إغلاقها.
        /// </summary>
        private void StocktakeBarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is not InventoryViewModel vm) return;

            e.Handled = true;

            // تحديث فوري لمصدر الربط قبل تنفيذ الأمر لالتقاط كامل نص الباركود المكتوب
            var binding = StocktakeBarcodeTextBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();

            if (vm.CountByBarcodeCommand.CanExecute(null))
            {
                vm.CountByBarcodeCommand.Execute(null);
            }

            StocktakeBarcodeTextBox.Focus();
            StocktakeBarcodeTextBox.SelectAll();
        }
    }
}
