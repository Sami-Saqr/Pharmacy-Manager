using System.Diagnostics;
using System.IO;
using System.Windows;
using CarPartManager.ViewModels;
using Microsoft.Win32;

namespace CarPartManager.Views
{
    internal static class ExcelExportDialog
    {
        public static async Task ExportAsync(
            Window owner,
            string defaultFileName,
            Func<string, string, Task<bool>> export,
            Func<string> getErrorMessage)
        {
            var shopName = (Application.Current?.MainWindow?.DataContext as MainViewModel)?.StoreName;
            if (string.IsNullOrWhiteSpace(shopName))
            {
                MessageBox.Show(owner, "تعذر قراءة اسم المتجر لإضافته إلى التقرير.", "تصدير Excel", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            while (true)
            {
                var dialog = new SaveFileDialog
                {
                    AddExtension = true,
                    DefaultExt = ".xlsx",
                    Filter = "ملف Excel (*.xlsx)|*.xlsx",
                    FileName = SanitizeFileName(defaultFileName),
                    OverwritePrompt = true,
                    Title = "تصدير التقرير إلى Excel"
                };
                if (dialog.ShowDialog(owner) != true)
                {
                    return;
                }

                if (!await export(dialog.FileName, shopName))
                {
                    MessageBox.Show(owner, getErrorMessage(), "تعذر التصدير", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                if (MessageBox.Show(owner, "تم تصدير الملف بنجاح. هل تريد فتحه الآن؟", "اكتمل التصدير", MessageBoxButton.YesNo, MessageBoxImage.Information)
                    == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(owner, $"تم حفظ الملف، لكن تعذر فتحه:\n{ex.Message}", "تعذر فتح الملف", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                return;
            }
        }

        public static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
            return safe.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? safe : safe + ".xlsx";
        }
    }
}
