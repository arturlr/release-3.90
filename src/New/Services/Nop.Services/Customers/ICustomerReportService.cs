using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Customers;

public interface ICustomerReportService
{
    Task<IPagedList<BestCustomerReportLine>> GetBestCustomersReportAsync(
        DateTime? createdFromUtc, DateTime? createdToUtc,
        OrderStatus? os, PaymentStatus? ps, ShippingStatus? ss,
        int orderBy, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<int> GetRegisteredCustomersReportAsync(int days);
}
