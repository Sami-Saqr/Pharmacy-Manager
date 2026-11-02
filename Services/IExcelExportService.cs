namespace PharmacyManager.Services
{
    public interface IExcelExportService
    {
        Task ExportAsync(ExcelExportWorkbook workbook, string destinationPath, CancellationToken cancellationToken = default);
    }
}
