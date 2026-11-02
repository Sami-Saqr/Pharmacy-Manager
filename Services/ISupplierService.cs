using PharmacyManager.Models;

namespace PharmacyManager.Services
{
    /// <summary>
    /// واجهة خدمة إدارة الموردين (ISupplierService)
    /// </summary>
    public interface ISupplierService
    {
        Task<IEnumerable<SupplierModel>> GetAllSuppliersAsync();
        Task<SupplierModel?> GetByIdAsync(int id);
        Task<SupplierModel> AddSupplierAsync(SupplierModel supplier);
        Task<bool> UpdateSupplierAsync(SupplierModel supplier);
        Task<bool> DeleteSupplierAsync(int id);
        Task<IEnumerable<SupplierModel>> SearchSuppliersAsync(string query);
    }
}
