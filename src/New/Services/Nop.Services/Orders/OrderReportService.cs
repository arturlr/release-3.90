using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Orders;

public class OrderReportService(
    IRepository<Order> orderRepository,
    IRepository<OrderItem> orderItemRepository,
    IRepository<OrderNote> orderNoteRepository,
    IRepository<Product> productRepository,
    IRepository<Address> addressRepository) : IOrderReportService
{
    private IQueryable<Order> GetFilteredOrders(int storeId, int vendorId, int billingCountryId,
        int orderId, string? paymentMethodSystemName,
        List<int>? osIds, List<int>? psIds, List<int>? ssIds,
        DateTime? startTimeUtc, DateTime? endTimeUtc,
        string? billingEmail, string? billingLastName, string? orderNotes)
    {
        var query = orderRepository.Table.Where(o => !o.Deleted);

        if (storeId > 0) query = query.Where(o => o.StoreId == storeId);
        if (orderId > 0) query = query.Where(o => o.Id == orderId);
        if (!string.IsNullOrEmpty(paymentMethodSystemName)) query = query.Where(o => o.PaymentMethodSystemName == paymentMethodSystemName);
        if (startTimeUtc.HasValue) query = query.Where(o => o.CreatedOnUtc >= startTimeUtc.Value);
        if (endTimeUtc.HasValue) query = query.Where(o => o.CreatedOnUtc <= endTimeUtc.Value);
        if (osIds is { Count: > 0 }) query = query.Where(o => osIds.Contains(o.OrderStatusId));
        if (psIds is { Count: > 0 }) query = query.Where(o => psIds.Contains(o.PaymentStatusId));
        if (ssIds is { Count: > 0 }) query = query.Where(o => ssIds.Contains(o.ShippingStatusId));

        if (billingCountryId > 0 || !string.IsNullOrEmpty(billingEmail) || !string.IsNullOrEmpty(billingLastName))
        {
            query = from o in query
                    join a in addressRepository.Table on o.BillingAddressId equals a.Id
                    where (billingCountryId <= 0 || a.CountryId == billingCountryId) &&
                          (string.IsNullOrEmpty(billingEmail) || a.Email!.Contains(billingEmail)) &&
                          (string.IsNullOrEmpty(billingLastName) || a.LastName!.Contains(billingLastName))
                    select o;
        }

        if (!string.IsNullOrEmpty(orderNotes))
        {
            query = from o in query
                    join n in orderNoteRepository.Table on o.Id equals n.OrderId
                    where n.Note != null && n.Note.Contains(orderNotes)
                    select o;
            query = query.Distinct();
        }

        return query;
    }

    public Task<IList<OrderByCountryReportLine>> GetCountryReportAsync(int storeId = 0, OrderStatus? os = null,
        PaymentStatus? ps = null, ShippingStatus? ss = null,
        DateTime? startTimeUtc = null, DateTime? endTimeUtc = null)
    {
        var query = orderRepository.Table.Where(o => !o.Deleted);
        if (storeId > 0) query = query.Where(o => o.StoreId == storeId);
        if (os.HasValue) query = query.Where(o => o.OrderStatusId == (int)os.Value);
        if (ps.HasValue) query = query.Where(o => o.PaymentStatusId == (int)ps.Value);
        if (ss.HasValue) query = query.Where(o => o.ShippingStatusId == (int)ss.Value);
        if (startTimeUtc.HasValue) query = query.Where(o => o.CreatedOnUtc >= startTimeUtc.Value);
        if (endTimeUtc.HasValue) query = query.Where(o => o.CreatedOnUtc <= endTimeUtc.Value);

        var report = from o in query
                     join a in addressRepository.Table on o.BillingAddressId equals a.Id
                     group o by a.CountryId into g
                     select new OrderByCountryReportLine
                     {
                         CountryId = g.Key ?? 0,
                         TotalOrders = g.Count(),
                         SumOrders = g.Sum(x => x.OrderTotal)
                     };

        return Task.FromResult<IList<OrderByCountryReportLine>>(report.OrderByDescending(x => x.SumOrders).ToList());
    }

    public Task<OrderAverageReportLine> GetOrderAverageReportLineAsync(int storeId = 0, int vendorId = 0,
        int billingCountryId = 0, int orderId = 0, string? paymentMethodSystemName = null,
        List<int>? osIds = null, List<int>? psIds = null, List<int>? ssIds = null,
        DateTime? startTimeUtc = null, DateTime? endTimeUtc = null,
        string? billingEmail = null, string? billingLastName = null, string? orderNotes = null)
    {
        var query = GetFilteredOrders(storeId, vendorId, billingCountryId, orderId, paymentMethodSystemName,
            osIds, psIds, ssIds, startTimeUtc, endTimeUtc, billingEmail, billingLastName, orderNotes);

        var orders = query.ToList();
        return Task.FromResult(new OrderAverageReportLine
        {
            CountOrders = orders.Count,
            SumShippingExclTax = orders.Sum(o => o.OrderShippingExclTax),
            SumTax = orders.Sum(o => o.OrderTax),
            SumOrders = orders.Sum(o => o.OrderTotal)
        });
    }

    public Task<OrderAverageReportLineSummary> OrderAverageReportAsync(int storeId, OrderStatus os)
    {
        var result = new OrderAverageReportLineSummary { OrderStatus = os };
        var now = DateTime.UtcNow;

        // today
        var todayQuery = orderRepository.Table.Where(o => !o.Deleted && o.OrderStatusId == (int)os && o.CreatedOnUtc >= now.Date);
        if (storeId > 0) todayQuery = todayQuery.Where(o => o.StoreId == storeId);
        var todayOrders = todayQuery.ToList();
        result.SumTodayOrders = todayOrders.Sum(o => o.OrderTotal);
        result.CountTodayOrders = todayOrders.Count;

        // this week (last 7 days)
        var weekStart = now.AddDays(-7);
        var weekQuery = orderRepository.Table.Where(o => !o.Deleted && o.OrderStatusId == (int)os && o.CreatedOnUtc >= weekStart);
        if (storeId > 0) weekQuery = weekQuery.Where(o => o.StoreId == storeId);
        var weekOrders = weekQuery.ToList();
        result.SumThisWeekOrders = weekOrders.Sum(o => o.OrderTotal);
        result.CountThisWeekOrders = weekOrders.Count;

        // this month
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthQuery = orderRepository.Table.Where(o => !o.Deleted && o.OrderStatusId == (int)os && o.CreatedOnUtc >= monthStart);
        if (storeId > 0) monthQuery = monthQuery.Where(o => o.StoreId == storeId);
        var monthOrders = monthQuery.ToList();
        result.SumThisMonthOrders = monthOrders.Sum(o => o.OrderTotal);
        result.CountThisMonthOrders = monthOrders.Count;

        // this year
        var yearStart = new DateTime(now.Year, 1, 1);
        var yearQuery = orderRepository.Table.Where(o => !o.Deleted && o.OrderStatusId == (int)os && o.CreatedOnUtc >= yearStart);
        if (storeId > 0) yearQuery = yearQuery.Where(o => o.StoreId == storeId);
        var yearOrders = yearQuery.ToList();
        result.SumThisYearOrders = yearOrders.Sum(o => o.OrderTotal);
        result.CountThisYearOrders = yearOrders.Count;

        // all time
        var allQuery = orderRepository.Table.Where(o => !o.Deleted && o.OrderStatusId == (int)os);
        if (storeId > 0) allQuery = allQuery.Where(o => o.StoreId == storeId);
        var allOrders = allQuery.ToList();
        result.SumAllTimeOrders = allOrders.Sum(o => o.OrderTotal);
        result.CountAllTimeOrders = allOrders.Count;

        return Task.FromResult(result);
    }

    public Task<IPagedList<BestsellersReportLine>> BestSellersReportAsync(int categoryId = 0, int manufacturerId = 0,
        int storeId = 0, int vendorId = 0,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        OrderStatus? os = null, PaymentStatus? ps = null, ShippingStatus? ss = null,
        int billingCountryId = 0, int orderBy = 1,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var orderQuery = orderRepository.Table.Where(o => !o.Deleted);
        if (storeId > 0) orderQuery = orderQuery.Where(o => o.StoreId == storeId);
        if (os.HasValue) orderQuery = orderQuery.Where(o => o.OrderStatusId == (int)os.Value);
        if (ps.HasValue) orderQuery = orderQuery.Where(o => o.PaymentStatusId == (int)ps.Value);
        if (ss.HasValue) orderQuery = orderQuery.Where(o => o.ShippingStatusId == (int)ss.Value);
        if (createdFromUtc.HasValue) orderQuery = orderQuery.Where(o => o.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue) orderQuery = orderQuery.Where(o => o.CreatedOnUtc <= createdToUtc.Value);

        if (billingCountryId > 0)
        {
            orderQuery = from o in orderQuery
                         join a in addressRepository.Table on o.BillingAddressId equals a.Id
                         where a.CountryId == billingCountryId
                         select o;
        }

        var query = from oi in orderItemRepository.Table
                    join o in orderQuery on oi.OrderId equals o.Id
                    group oi by oi.ProductId into g
                    select new BestsellersReportLine
                    {
                        ProductId = g.Key,
                        TotalAmount = g.Sum(x => x.PriceExclTax),
                        TotalQuantity = g.Sum(x => x.Quantity)
                    };

        query = orderBy == 1
            ? query.OrderByDescending(x => x.TotalQuantity)
            : query.OrderByDescending(x => x.TotalAmount);

        return Task.FromResult<IPagedList<BestsellersReportLine>>(new PagedList<BestsellersReportLine>(query, pageIndex, pageSize));
    }

    public Task<int[]> GetAlsoPurchasedProductsIdsAsync(int storeId, int productId,
        int recordsToReturn = 5, bool showHidden = false)
    {
        var orderQuery = orderRepository.Table.Where(o => !o.Deleted);
        if (storeId > 0) orderQuery = orderQuery.Where(o => o.StoreId == storeId);

        // find orders containing the product
        var ordersWithProduct = from oi in orderItemRepository.Table
                                join o in orderQuery on oi.OrderId equals o.Id
                                where oi.ProductId == productId
                                select o.Id;

        // find other products in those orders
        var query = from oi in orderItemRepository.Table
                    where ordersWithProduct.Contains(oi.OrderId) && oi.ProductId != productId
                    group oi by oi.ProductId into g
                    orderby g.Count() descending
                    select g.Key;

        return Task.FromResult(query.Take(recordsToReturn).ToArray());
    }

    public Task<IPagedList<Product>> ProductsNeverSoldAsync(int vendorId = 0, int storeId = 0,
        int categoryId = 0, int manufacturerId = 0,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var orderQuery = orderRepository.Table.Where(o => !o.Deleted);
        if (storeId > 0) orderQuery = orderQuery.Where(o => o.StoreId == storeId);
        if (createdFromUtc.HasValue) orderQuery = orderQuery.Where(o => o.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue) orderQuery = orderQuery.Where(o => o.CreatedOnUtc <= createdToUtc.Value);

        var soldProductIds = (from oi in orderItemRepository.Table
                              join o in orderQuery on oi.OrderId equals o.Id
                              select oi.ProductId).Distinct();

        var productQuery = productRepository.Table.Where(p => !p.Deleted && !soldProductIds.Contains(p.Id));
        if (!showHidden) productQuery = productQuery.Where(p => p.Published);
        if (vendorId > 0) productQuery = productQuery.Where(p => p.VendorId == vendorId);

        productQuery = productQuery.OrderBy(p => p.Name);

        return Task.FromResult<IPagedList<Product>>(new PagedList<Product>(productQuery, pageIndex, pageSize));
    }

    public Task<decimal> ProfitReportAsync(int storeId = 0, int vendorId = 0,
        int billingCountryId = 0, int orderId = 0, string? paymentMethodSystemName = null,
        List<int>? osIds = null, List<int>? psIds = null, List<int>? ssIds = null,
        DateTime? startTimeUtc = null, DateTime? endTimeUtc = null,
        string? billingEmail = null, string? billingLastName = null, string? orderNotes = null)
    {
        var query = GetFilteredOrders(storeId, vendorId, billingCountryId, orderId, paymentMethodSystemName,
            osIds, psIds, ssIds, startTimeUtc, endTimeUtc, billingEmail, billingLastName, orderNotes);

        var orderIds = query.Select(o => o.Id).ToList();

        var profit = query.Sum(o => o.OrderTotal - o.OrderShippingExclTax - o.OrderTax - o.RefundedAmount);

        // subtract product costs
        var productCosts = orderItemRepository.Table
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Sum(oi => (decimal?)oi.OriginalProductCost * oi.Quantity) ?? 0m;

        return Task.FromResult(profit - productCosts);
    }
}
