using System.Windows;

namespace PharmacyManager.Views
{
    public partial class UnsavedChangesDialog : Window
    {
        public UnsavedChangesDialog()
        {
            InitializeComponent();
        }

        private void ReturnToScreen_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void DiscardChanges_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
