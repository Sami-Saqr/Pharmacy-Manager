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
