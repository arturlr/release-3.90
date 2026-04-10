using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;

namespace Nop.Services.ExportImport;

public interface IExportManager
{
    Task<string> ExportProductsToXmlAsync(IList<Product> products);
    Task<byte[]> ExportProductsToXlsxAsync(IList<Product> products);
    Task<string> ExportCategoriestoXmlAsync();
    Task<byte[]> ExportCategoriesToXlsxAsync(IList<Category> categories);
    string ExportManufacturersToXml(IList<Manufacturer> manufacturers);
    byte[] ExportManufacturersToXlsx(IList<Manufacturer> manufacturers);
    Task<byte[]> ExportOrdersToXlsxAsync(IList<Order> orders);
    Task<byte[]> ExportCustomersToXlsxAsync(IList<Customer> customers);
    string ExportNewsletterSubscribersToTxt(IList<NewsLetterSubscription> subscriptions);
    string ExportStatesToTxt(IList<StateProvince> states);
}
