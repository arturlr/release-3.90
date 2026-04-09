using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Customers;

public class CustomerReportService : ICustomerReportService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<CustomerRole> _customerRoleRepository;
    private readonly IRepository<Order> _orderRepository;

    public CustomerReportService(
        IRepository<Customer> customerRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<CustomerRole> customerRoleRepository,
        IRepository<Order> orderRepository)
    {
        _customerRepository = customerRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _customerRoleRepository = customerRoleRepository;
        _orderRepository = orderRepository;
    }

    public virtual Task<IPagedList<BestCustomerReportLine>> GetBestCustomersReportAsync(
        DateTime? createdFromUtc, DateTime? createdToUtc,
        OrderStatus? os, PaymentStatus? ps, ShippingStatus? ss,
        int orderBy, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        int? orderStatusId = os.HasValue ? (int)os.Value : null;
        int? paymentStatusId = ps.HasValue ? (int)ps.Value : null;
        int? shippingStatusId = ss.HasValue ? (int)ss.Value : null;

        var query = from c in _customerRepository.TableNoTracking
                    join o in _orderRepository.TableNoTracking on c.Id equals o.CustomerId
                    where (!createdFromUtc.HasValue || createdFromUtc.Value <= o.CreatedOnUtc) &&
                          (!createdToUtc.HasValue || createdToUtc.Value >= o.CreatedOnUtc) &&
                          (!orderStatusId.HasValue || orderStatusId == o.OrderStatusId) &&
                          (!paymentStatusId.HasValue || paymentStatusId == o.PaymentStatusId) &&
                          (!shippingStatusId.HasValue || shippingStatusId == o.ShippingStatusId) &&
                          !o.Deleted && !c.Deleted
                    select new { c, o };

        var grouped = from co in query
                      group co by co.c.Id into g
                      select new BestCustomerReportLine
                      {
                          CustomerId = g.Key,
                          OrderTotal = g.Sum(x => x.o.OrderTotal),
                          OrderCount = g.Count()
                      };

        grouped = orderBy switch
        {
            1 => grouped.OrderByDescending(x => x.OrderTotal),
            2 => grouped.OrderByDescending(x => x.OrderCount),
            _ => throw new ArgumentException("Wrong orderBy parameter", nameof(orderBy))
        };

        return Task.FromResult<IPagedList<BestCustomerReportLine>>(
            new PagedList<BestCustomerReportLine>(grouped, pageIndex, pageSize));
    }

    public virtual Task<int> GetRegisteredCustomersReportAsync(int days)
    {
        var date = DateTime.UtcNow.AddDays(-days);

        var registeredRole = _customerRoleRepository.TableNoTracking
            .FirstOrDefault(cr => cr.SystemName == SystemCustomerRoleNames.Registered);
        if (registeredRole == null)
            return Task.FromResult(0);

        var count = (from c in _customerRepository.TableNoTracking
                     join crm in _customerRoleMappingRepository.TableNoTracking
                         on c.Id equals crm.CustomerId
                     where !c.Deleted &&
                           crm.CustomerRoleId == registeredRole.Id &&
                           c.CreatedOnUtc >= date
                     select c.Id).Distinct().Count();

        return Task.FromResult(count);
    }
}
