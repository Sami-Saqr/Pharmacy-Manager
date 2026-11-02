using Microsoft.EntityFrameworkCore;
using PharmacyManager.Data;
using PharmacyManager.Models;

namespace PharmacyManager.Services
{
    /// <summary>
    /// خدمة استخراج مؤشرات وإحصائيات لوحة التحكم من قاعدة بيانات SQLite المحلية
    /// </summary>
    public class SqliteDashboardService : IDashboardService
    {
        private PharmacyDbContext CreateContext() => new PharmacyDbContext();

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            try
            {
                using var context = CreateContext();
                var summary = new DashboardSummaryDto();
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var productStats = await context.Products.AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(products => new
                    {
                        Count = products.Count(),
                        Pieces = products.Sum(p => p.Quantity),
                        InventoryCost = products.Sum(p => p.Quantity * p.PurchasePrice)
                    })
                    .SingleOrDefaultAsync();

                summary.TotalProductsCount = productStats?.Count ?? 0;
                summary.TotalPiecesCount = productStats?.Pieces ?? 0;
                summary.TotalInventoryCost = productStats?.InventoryCost ?? 0m;
                var categories = await context.Products.AsNoTracking()
                    .Select(p => p.Category)
                    .Distinct()
                    .ToListAsync();
                summary.TotalCategoriesCount = categories.Count(category => !string.IsNullOrWhiteSpace(category));

                var lowStockProducts = await context.Products.AsNoTracking()
                    .Where(p => p.Quantity <= p.MinimumStock)
                    .OrderBy(p => p.Quantity)
                    .ThenBy(p => p.MinimumStock - p.Quantity)
                    .Select(p => new
                    {
                        p.PartNumber,
                        p.Name,
                        p.CompatibleVehicles,
                        p.Quantity,
                        p.MinimumStock,
                        p.SellingPrice,
                        p.PurchasePrice
                    })
                    .ToListAsync();

                summary.LowStockProductsCount = lowStockProducts.Count;
                summary.LowStockItems = lowStockProducts.Select(p =>
                {
                    var (urgencyText, urgencyColor) = p.Quantity <= 0
                        ? ("نفد تماماً", "#DC2626")
                        : p.Quantity <= Math.Max(1, p.MinimumStock / 2)
                            ? ("حرج جداً", "#DC2626")
                            : ("منخفض", "#D97706");

                    return new LowStockItem
                    {
                        PartNumber = p.PartNumber,
                        PartName = p.Name,
                        VehicleModel = string.IsNullOrWhiteSpace(p.CompatibleVehicles) ? "عام" : p.CompatibleVehicles,
                        CurrentStock = p.Quantity,
                        MinimumRequired = p.MinimumStock,
                        Unit = "قطعة",
                        UnitPrice = p.SellingPrice > 0 ? p.SellingPrice : p.PurchasePrice,
                        UrgencyText = urgencyText,
                        UrgencyColor = urgencyColor
                    };
                }).ToList();

                var purchaseStats = await context.PurchaseInvoices.AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(invoices => new
                    {
                        Count = invoices.Count(),
                        TodayCount = invoices.Count(p => p.Status == "معتمدة" &&
                            ((p.PostedDate.HasValue && p.PostedDate.Value >= today && p.PostedDate.Value < tomorrow) ||
                             (!p.PostedDate.HasValue && p.InvoiceDate >= today && p.InvoiceDate < tomorrow))),
                        TodayTotal = invoices.Sum(p => p.Status == "معتمدة" &&
                            ((p.PostedDate.HasValue && p.PostedDate.Value >= today && p.PostedDate.Value < tomorrow) ||
                             (!p.PostedDate.HasValue && p.InvoiceDate >= today && p.InvoiceDate < tomorrow))
                                ? p.TotalAmount
                                : 0m)
                    })
                    .SingleOrDefaultAsync();

                summary.TotalPurchasesCount = purchaseStats?.Count ?? 0;
                summary.TodayPurchasesCount = purchaseStats?.TodayCount ?? 0;
                summary.TodayPurchasesTotal = purchaseStats?.TodayTotal ?? 0m;

                var salesStats = await context.SalesInvoices.AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(invoices => new
                    {
                        Count = invoices.Count(),
                        TodayCount = invoices.Count(s => s.Status == "معتمدة" &&
                            ((s.PostedDate.HasValue && s.PostedDate.Value >= today && s.PostedDate.Value < tomorrow) ||
                             (!s.PostedDate.HasValue && s.InvoiceDate >= today && s.InvoiceDate < tomorrow))),
                        TodayTotal = invoices.Sum(s => s.Status == "معتمدة" &&
                            ((s.PostedDate.HasValue && s.PostedDate.Value >= today && s.PostedDate.Value < tomorrow) ||
                             (!s.PostedDate.HasValue && s.InvoiceDate >= today && s.InvoiceDate < tomorrow))
                                ? s.GrandTotal
                                : 0m)
                    })
                    .SingleOrDefaultAsync();

                summary.TotalSalesCount = salesStats?.Count ?? 0;
                summary.TodaySalesCount = salesStats?.TodayCount ?? 0;
                summary.TodaySalesTotal = salesStats?.TodayTotal ?? 0m;

                var salesReturnStats = await context.SalesReturns.AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(returns => new
                    {
                        Count = returns.Count(),
                        TodayCount = returns.Count(r => r.Status == "معتمدة" && r.ReturnDate >= today && r.ReturnDate < tomorrow),
                        TodayTotal = returns.Sum(r => r.Status == "معتمدة" && r.ReturnDate >= today && r.ReturnDate < tomorrow
                            ? r.TotalAmount
                            : 0m)
                    })
                    .SingleOrDefaultAsync();

                summary.TodaySalesReturnsCount = salesReturnStats?.TodayCount ?? 0;
                summary.TodaySalesReturnsTotal = salesReturnStats?.TodayTotal ?? 0m;

                var purchaseReturnStats = await context.PurchaseReturns.AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(returns => new
                    {
                        Count = returns.Count(),
                        TodayCount = returns.Count(r => r.Status == "معتمدة" && r.ReturnDate >= today && r.ReturnDate < tomorrow),
                        TodayTotal = returns.Sum(r => r.Status == "معتمدة" && r.ReturnDate >= today && r.ReturnDate < tomorrow
                            ? r.TotalAmount
                            : 0m)
                    })
                    .SingleOrDefaultAsync();

                summary.TodayPurchaseReturnsCount = purchaseReturnStats?.TodayCount ?? 0;
                summary.TodayPurchaseReturnsTotal = purchaseReturnStats?.TodayTotal ?? 0m;
                summary.TotalReturnsCount = (salesReturnStats?.Count ?? 0) + (purchaseReturnStats?.Count ?? 0);

                var recentInvoices = new List<(DateTime Date, int Id, RecentInvoiceSummary Summary)>();

                void AddRecent(DateTime date, int id, string code, string party, string type, decimal amount, string payment, string color)
                {
                    recentInvoices.Add((date, id, new RecentInvoiceSummary
                    {
                        InvoiceCode = code,
                        PartyName = party,
                        TypeName = type,
                        TotalAmount = amount,
                        PaymentMethod = payment,
                        BadgeColor = color
                    }));
                }

                var recentPurchases = await context.PurchaseInvoices.AsNoTracking()
                    .OrderByDescending(p => p.InvoiceDate)
                    .ThenByDescending(p => p.Id)
                    .Take(7)
                    .Select(p => new { p.InvoiceDate, p.Id, p.InvoiceNumber, p.SupplierName, p.Status, p.TotalAmount })
                    .ToListAsync();
                foreach (var invoice in recentPurchases)
                {
                    AddRecent(
                        invoice.InvoiceDate,
                        invoice.Id,
                        invoice.InvoiceNumber,
                        string.IsNullOrWhiteSpace(invoice.SupplierName) ? "مورد عام" : invoice.SupplierName,
                        invoice.Status == "معتمدة" ? "فاتورة مشتريات (معتمدة)" : invoice.Status == "ملغاة" ? "فاتورة مشتريات (ملغاة)" : "مسودة مشتريات",
                        invoice.TotalAmount,
                        invoice.Status == "ملغاة" ? "ملغاة" : invoice.Status,
                        invoice.Status == "معتمدة" ? "#059669" : invoice.Status == "ملغاة" ? "#DC2626" : "#D97706");
                }

                var recentSales = await context.SalesInvoices.AsNoTracking()
                    .OrderByDescending(s => s.InvoiceDate)
                    .ThenByDescending(s => s.Id)
                    .Take(7)
                    .Select(s => new { s.InvoiceDate, s.Id, s.InvoiceNumber, s.CustomerName, s.Status, s.GrandTotal, s.PaymentMethod })
                    .ToListAsync();
                foreach (var invoice in recentSales)
                {
                    AddRecent(
                        invoice.InvoiceDate,
                        invoice.Id,
                        invoice.InvoiceNumber,
                        string.IsNullOrWhiteSpace(invoice.CustomerName) ? "عميل نقدي" : invoice.CustomerName,
                        invoice.Status == "معتمدة" ? "فاتورة مبيعات (معتمدة)" : invoice.Status == "ملغاة" ? "فاتورة مبيعات (ملغاة)" : "مسودة مبيعات",
                        invoice.GrandTotal,
                        invoice.Status == "ملغاة" ? "ملغاة" : invoice.PaymentMethod,
                        invoice.Status == "معتمدة" ? "#059669" : invoice.Status == "ملغاة" ? "#DC2626" : "#D97706");
                }

                var recentSalesReturns = await context.SalesReturns.AsNoTracking()
                    .OrderByDescending(r => r.ReturnDate)
                    .ThenByDescending(r => r.Id)
                    .Take(7)
                    .Select(r => new { r.ReturnDate, r.Id, r.ReturnNumber, r.CustomerName, r.TotalAmount, r.Reason })
                    .ToListAsync();
                foreach (var invoice in recentSalesReturns)
                {
                    AddRecent(
                        invoice.ReturnDate,
                        invoice.Id,
                        invoice.ReturnNumber,
                        string.IsNullOrWhiteSpace(invoice.CustomerName) ? "عميل نقدي" : invoice.CustomerName,
                        "مرتجع مبيعات (معتمد)",
                        invoice.TotalAmount,
                        string.IsNullOrWhiteSpace(invoice.Reason) ? "مرتجع" : invoice.Reason,
                        "#2563EB");
                }

                var recentPurchaseReturns = await context.PurchaseReturns.AsNoTracking()
                    .OrderByDescending(r => r.ReturnDate)
                    .ThenByDescending(r => r.Id)
                    .Take(7)
                    .Select(r => new { r.ReturnDate, r.Id, r.ReturnNumber, r.SupplierName, r.TotalAmount, r.Reason })
                    .ToListAsync();
                foreach (var invoice in recentPurchaseReturns)
                {
                    AddRecent(
                        invoice.ReturnDate,
                        invoice.Id,
                        invoice.ReturnNumber,
                        string.IsNullOrWhiteSpace(invoice.SupplierName) ? "مورد" : invoice.SupplierName,
                        "مرتجع مشتريات (معتمد)",
                        invoice.TotalAmount,
                        string.IsNullOrWhiteSpace(invoice.Reason) ? "مرتجع" : invoice.Reason,
                        "#7C3AED");
                }

                string FormatTime(DateTime date)
                {
                    if (date.Date == today) return $"اليوم {date:hh:mm tt}";
                    if (date.Date == today.AddDays(-1)) return $"أمس {date:hh:mm tt}";
                    return date.ToString("yyyy/MM/dd");
                }

                summary.RecentInvoices = recentInvoices
                    .OrderByDescending(invoice => invoice.Date)
                    .ThenByDescending(invoice => invoice.Id)
                    .Take(7)
                    .Select(invoice =>
                    {
                        invoice.Summary.TimeFormatted = FormatTime(invoice.Date);
                        return invoice.Summary;
                    })
                    .ToList();

                summary.TotalSuppliersCount = await context.Suppliers.CountAsync();
                summary.TotalInventoryMovementsCount = await context.InventoryMovements.CountAsync();
                summary.LastUpdated = DateTime.Now;

                return summary;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"تعذر تحميل مؤشرات لوحة التحكم من قاعدة البيانات: {ex.Message}", ex);
            }
        }
    }
}
