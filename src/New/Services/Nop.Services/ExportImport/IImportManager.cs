namespace Nop.Services.ExportImport;

public interface IImportManager
{
    Task ImportProductsFromXlsxAsync(Stream stream);
    Task<int> ImportNewsletterSubscribersFromTxtAsync(Stream stream);
    Task<int> ImportStatesFromTxtAsync(Stream stream);
    Task ImportManufacturersFromXlsxAsync(Stream stream);
    Task ImportCategoriesFromXlsxAsync(Stream stream);
}
