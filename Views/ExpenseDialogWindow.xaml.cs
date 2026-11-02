using System.Windows;
using PharmacyManager.ViewModels;

namespace PharmacyManager.Views
{
    /// <summary>
    /// نافذة إضافة أو تعديل مصروف (Interaction logic for ExpenseDialogWindow.xaml)
    /// </summary>
    public partial class ExpenseDialogWindow : Window
    {
        public ExpenseDialogWindow(ExpenseDialogViewModel viewModel)
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
