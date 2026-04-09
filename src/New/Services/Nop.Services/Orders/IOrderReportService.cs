using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Orders;

public interface IOrderReportService
{
    Task<IList<OrderByCountryReportLine>> GetCountryReportAsync(int storeId = 0, OrderStatus? os = null,
        PaymentStatus? ps = null, ShippingStatus? ss = null,
        DateTime? startTimeUtc = null, DateTime? endTimeUtc = null);

    Task<OrderAverageReportLine> GetOrderAverageReportLineAsync(int storeId = 0, int vendorId = 0,
        int billingCountryId = 0, int orderId = 0, string? paymentMethodSystemName = null,
        List<int>? osIds = null, List<int>? psIds = null, List<int>? ssIds = null,
        DateTime? startTimeUtc = null, DateTime? endTimeUtc = null,
        string? billingEmail = null, string? billingLastName = null, string? orderNotes = null);

    Task<OrderAverageReportLineSummary> OrderAverageReportAsync(int storeId, OrderStatus os);

    Task<IPagedList<BestsellersReportLine>> BestSellersReportAsync(int categoryId = 0, int manufacturerId = 0,
        int storeId = 0, int vendorId = 0,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        OrderStatus? os = null, PaymentStatus? ps = null, ShippingStatus? ss = null,
        int billingCountryId = 0, int orderBy = 1,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

    Task<int[]> GetAlsoPurchasedProductsIdsAsync(int storeId, int productId,
        int recordsToReturn = 5, bool showHidden = false);

    Task<IPagedList<Product>> ProductsNeverSoldAsync(int vendorId = 0, int storeId = 0,
        int categoryId = 0, int manufacturerId = 0,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

    Task<decimal> ProfitReportAsync(int storeId = 0, int vendorId = 0,
        int billingCountryId = 0, int orderId = 0, string? paymentMethodSystemName = null,
        List<int>? osIds = null, List<int>? psIds = null, List<int>? ssIds = null,
        DateTime? startTimeUtc = null, DateTime? endTimeUtc = null,
        string? billingEmail = null, string? billingLastName = null, string? orderNotes = null);
}
