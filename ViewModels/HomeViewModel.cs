using System.Collections.ObjectModel;
using System.Windows.Media;
using CarPartManager.Models;
using CarPartManager.MVVM;

namespace CarPartManager.ViewModels
{
    public sealed class HomeViewModel : ViewModelBase
    {
        public ObservableCollection<HomeTileSection> Sections { get; }

        public HomeViewModel(IEnumerable<NavItem> navigationItems)
        {
            var itemsByTag = navigationItems.ToDictionary(item => item.Tag);
            var salesAccent = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            var salesLight = new SolidColorBrush(Color.FromRgb(236, 253, 245));
            var operationsAccent = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            var operationsLight = new SolidColorBrush(Color.FromRgb(239, 246, 255));
            var financeAccent = new SolidColorBrush(Color.FromRgb(79, 70, 229));
            var financeLight = new SolidColorBrush(Color.FromRgb(238, 242, 255));

            Sections = new ObservableCollection<HomeTileSection>
            {
                new(
                    "المبيعات",
                    salesAccent,
                    salesLight,
                    new[]
                    {
                        CreateTile(itemsByTag, "POS", "إتمام عمليات البيع وإدارة الفاتورة.", true, salesAccent, salesLight),
                        CreateTile(itemsByTag, "SalesInvoicesHistory", "مراجعة فواتير المبيعات المسجلة.", false, salesAccent, salesLight),
                        CreateTile(itemsByTag, "Returns", "إدارة ومراجعة المرتجعات.", false, salesAccent, salesLight),
                        CreateTile(itemsByTag, "Customers", "إدارة بيانات العملاء.", false, salesAccent, salesLight)
                    }),
                new(
                    "المشتريات والمخزون",
                    operationsAccent,
                    operationsLight,
                    new[]
                    {
                        CreateTile(itemsByTag, "Products", "إدارة المنتجات والخدمات.", false, operationsAccent, operationsLight),
                        CreateTile(itemsByTag, "Inventory", "متابعة الكميات وعمليات الجرد.", false, operationsAccent, operationsLight),
                        CreateTile(itemsByTag, "PurchaseInvoices", "تسجيل المشتريات والتوريد.", false, operationsAccent, operationsLight),
                        CreateTile(itemsByTag, "PurchaseInvoicesHistory", "مراجعة فواتير المشتريات المسجلة.", false, operationsAccent, operationsLight),
                        CreateTile(itemsByTag, "Suppliers", "إدارة بيانات الموردين.", false, operationsAccent, operationsLight)
                    }),
                new(
                    "المالية والتحليل",
                    financeAccent,
                    financeLight,
                    new[]
                    {
                        CreateTile(itemsByTag, "Expenses", "تسجيل ومتابعة المصروفات.", false, financeAccent, financeLight),
                        CreateTile(itemsByTag, "Reports", "استعراض التقارير والتحليلات.", false, financeAccent, financeLight),
                        CreateTile(itemsByTag, "Dashboard", "نظرة عامة على حالة العمل.", false, financeAccent, financeLight)
                    })
            };
        }

        private static HomeTile CreateTile(
            IReadOnlyDictionary<string, NavItem> itemsByTag,
            string tag,
            string description,
            bool isProminent,
            Brush accentBrush,
            Brush accentLightBrush)
        {
            if (!itemsByTag.TryGetValue(tag, out var item))
            {
                throw new InvalidOperationException($"تعذر العثور على عنصر التنقل المطلوب: {tag}");
            }

            return new HomeTile(item, description, isProminent, accentBrush, accentLightBrush);
        }
    }
}
