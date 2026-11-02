using System.Windows;
using PharmacyManager.ViewModels;

namespace PharmacyManager.Views
{
    /// <summary>
    /// نافذة تأكيد إلغاء مصروف مع سبب إجباري (Interaction logic for CancelExpenseDialogWindow.xaml)
    /// </summary>
    public partial class CancelExpenseDialogWindow : Window
    {
        public CancelExpenseDialogWindow(CancelExpenseDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose = (result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
