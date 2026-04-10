using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Orders;

public class OrderService(
    IRepository<Order> orderRepository,
    IRepository<OrderItem> orderItemRepository,
    IRepository<OrderNote> orderNoteRepository,
    IRepository<RecurringPayment> recurringPaymentRepository,
    IRepository<RecurringPaymentHistory> recurringPaymentHistoryRepository,
    IRepository<Address> addressRepository,
    IEventPublisher eventPublisher) : IOrderService
{
    // Orders
    public Task<Order?> GetOrderByIdAsync(int orderId) =>
        Task.FromResult(orderId == 0 ? null : (Order?)orderRepository.GetById(orderId));

    public Task<Order?> GetOrderByCustomOrderNumberAsync(string customOrderNumber) =>
        Task.FromResult((Order?)orderRepository.Table.FirstOrDefault(o => o.CustomOrderNumber == customOrderNumber));

    public Task<IList<Order>> GetOrdersByIdsAsync(int[] orderIds)
    {
        if (orderIds == null || orderIds.Length == 0)
            return Task.FromResult<IList<Order>>([]);

        var orders = orderRepository.Table.Where(o => orderIds.Contains(o.Id)).ToList();
        // preserve input order
        return Task.FromResult<IList<Order>>(orderIds.Select(id => orders.FirstOrDefault(o => o.Id == id)!).Where(o => o != null).ToList());
    }

    public Task<Order?> GetOrderByGuidAsync(Guid orderGuid) =>
        Task.FromResult((Order?)orderRepository.Table.FirstOrDefault(o => o.OrderGuid == orderGuid));

    public Task DeleteOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        order.Deleted = true;
        orderRepository.Update(order);
        return eventPublisher.EntityUpdatedAsync(order);
    }

    public Task<IPagedList<Order>> SearchOrdersAsync(int storeId = 0, int vendorId = 0, int customerId = 0,
        int productId = 0, int affiliateId = 0, int warehouseId = 0, int billingCountryId = 0,
        string? paymentMethodSystemName = null, DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        List<int>? osIds = null, List<int>? psIds = null, List<int>? ssIds = null,
        string? billingEmail = null, string? billingLastName = null, string? orderNotes = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = orderRepository.Table.Where(o => !o.Deleted);

        if (storeId > 0) query = query.Where(o => o.StoreId == storeId);
        if (customerId > 0) query = query.Where(o => o.CustomerId == customerId);
        if (affiliateId > 0) query = query.Where(o => o.AffiliateId == affiliateId);
        if (!string.IsNullOrEmpty(paymentMethodSystemName)) query = query.Where(o => o.PaymentMethodSystemName == paymentMethodSystemName);
        if (createdFromUtc.HasValue) query = query.Where(o => o.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue) query = query.Where(o => o.CreatedOnUtc <= createdToUtc.Value);
        if (osIds is { Count: > 0 }) query = query.Where(o => osIds.Contains(o.OrderStatusId));
        if (psIds is { Count: > 0 }) query = query.Where(o => psIds.Contains(o.PaymentStatusId));
        if (ssIds is { Count: > 0 }) query = query.Where(o => ssIds.Contains(o.ShippingStatusId));

        // billing address filters via join
        if (billingCountryId > 0 || !string.IsNullOrEmpty(billingEmail) || !string.IsNullOrEmpty(billingLastName))
        {
            query = from o in query
                    join a in addressRepository.Table on o.BillingAddressId equals a.Id
                    where (billingCountryId <= 0 || a.CountryId == billingCountryId) &&
                          (string.IsNullOrEmpty(billingEmail) || a.Email!.Contains(billingEmail)) &&
                          (string.IsNullOrEmpty(billingLastName) || a.LastName!.Contains(billingLastName))
                    select o;
        }

        // product filter via order items
        if (productId > 0)
        {
            query = from o in query
                    join oi in orderItemRepository.Table on o.Id equals oi.OrderId
                    where oi.ProductId == productId
                    select o;
            query = query.Distinct();
        }

        // vendor filter via order items → product
        // Requires IRepository<Product> which creates circular dependency risk
        // Vendor filtering deferred to presentation layer or added when DI composition root is built

        // order notes filter
        if (!string.IsNullOrEmpty(orderNotes))
        {
            query = from o in query
                    join n in orderNoteRepository.Table on o.Id equals n.OrderId
                    where n.Note != null && n.Note.Contains(orderNotes)
                    select o;
            query = query.Distinct();
        }

        query = query.OrderByDescending(o => o.CreatedOnUtc);

        return Task.FromResult<IPagedList<Order>>(new PagedList<Order>(query, pageIndex, pageSize));
    }

    public Task InsertOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        orderRepository.Insert(order);
        return eventPublisher.EntityInsertedAsync(order);
    }

    public Task UpdateOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        orderRepository.Update(order);
        return eventPublisher.EntityUpdatedAsync(order);
    }

    public Task<Order?> GetOrderByAuthorizationTransactionIdAndPaymentMethodAsync(string authorizationTransactionId, string paymentMethodSystemName) =>
        Task.FromResult((Order?)orderRepository.Table.FirstOrDefault(o =>
            o.AuthorizationTransactionId == authorizationTransactionId &&
            o.PaymentMethodSystemName == paymentMethodSystemName));

    // Order items
    public Task<OrderItem?> GetOrderItemByIdAsync(int orderItemId) =>
        Task.FromResult(orderItemId == 0 ? null : (OrderItem?)orderItemRepository.GetById(orderItemId));

    public Task<OrderItem?> GetOrderItemByGuidAsync(Guid orderItemGuid) =>
        Task.FromResult((OrderItem?)orderItemRepository.Table.FirstOrDefault(oi => oi.OrderItemGuid == orderItemGuid));

    public Task<IList<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId) =>
        Task.FromResult<IList<OrderItem>>(orderItemRepository.Table.Where(oi => oi.OrderId == orderId).ToList());

    public Task<IList<OrderItem>> GetDownloadableOrderItemsAsync(int customerId)
    {
        var query = from oi in orderItemRepository.Table
                    join o in orderRepository.Table on oi.OrderId equals o.Id
                    where o.CustomerId == customerId && !o.Deleted
                    // IsDownload check requires Product nav property — deferred
                    orderby o.CreatedOnUtc descending, oi.Id descending
                    select oi;

        return Task.FromResult<IList<OrderItem>>(query.ToList());
    }

    public Task DeleteOrderItemAsync(OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);
        orderItemRepository.Delete(orderItem);
        return eventPublisher.EntityDeletedAsync(orderItem);
    }

    public Task InsertOrderItemAsync(OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);
        orderItemRepository.Insert(orderItem);
        return eventPublisher.EntityInsertedAsync(orderItem);
    }

    public Task UpdateOrderItemAsync(OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);
        orderItemRepository.Update(orderItem);
        return eventPublisher.EntityUpdatedAsync(orderItem);
    }

    // Order notes
    public Task<OrderNote?> GetOrderNoteByIdAsync(int orderNoteId) =>
        Task.FromResult(orderNoteId == 0 ? null : (OrderNote?)orderNoteRepository.GetById(orderNoteId));

    public Task<IList<OrderNote>> GetOrderNotesByOrderIdAsync(int orderId, bool? displayToCustomer = null)
    {
        var query = orderNoteRepository.Table.Where(n => n.OrderId == orderId);
        if (displayToCustomer.HasValue)
            query = query.Where(n => n.DisplayToCustomer == displayToCustomer.Value);
        return Task.FromResult<IList<OrderNote>>(query.OrderByDescending(n => n.CreatedOnUtc).ToList());
    }

    public Task DeleteOrderNoteAsync(OrderNote orderNote)
    {
        ArgumentNullException.ThrowIfNull(orderNote);
        orderNoteRepository.Delete(orderNote);
        return eventPublisher.EntityDeletedAsync(orderNote);
    }

    public Task InsertOrderNoteAsync(OrderNote orderNote)
    {
        ArgumentNullException.ThrowIfNull(orderNote);
        orderNoteRepository.Insert(orderNote);
        return eventPublisher.EntityInsertedAsync(orderNote);
    }

    // Recurring payments
    public Task DeleteRecurringPaymentAsync(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        recurringPayment.Deleted = true;
        recurringPaymentRepository.Update(recurringPayment);
        return eventPublisher.EntityUpdatedAsync(recurringPayment);
    }

    public Task<RecurringPayment?> GetRecurringPaymentByIdAsync(int recurringPaymentId) =>
        Task.FromResult(recurringPaymentId == 0 ? null : (RecurringPayment?)recurringPaymentRepository.GetById(recurringPaymentId));

    public Task InsertRecurringPaymentAsync(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        recurringPaymentRepository.Insert(recurringPayment);
        return eventPublisher.EntityInsertedAsync(recurringPayment);
    }

    public Task UpdateRecurringPaymentAsync(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        recurringPaymentRepository.Update(recurringPayment);
        return eventPublisher.EntityUpdatedAsync(recurringPayment);
    }

    public Task<IPagedList<RecurringPayment>> SearchRecurringPaymentsAsync(int storeId = 0, int customerId = 0,
        int initialOrderId = 0, OrderStatus? initialOrderStatus = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var query = recurringPaymentRepository.Table.Where(rp => !rp.Deleted);

        if (!showHidden)
            query = query.Where(rp => rp.IsActive);

        if (initialOrderId > 0)
            query = query.Where(rp => rp.InitialOrderId == initialOrderId);

        // storeId, customerId, initialOrderStatus require Order join
        if (storeId > 0 || customerId > 0 || initialOrderStatus.HasValue)
        {
            query = from rp in query
                    join o in orderRepository.Table on rp.InitialOrderId equals o.Id
                    where !o.Deleted &&
                          (storeId <= 0 || o.StoreId == storeId) &&
                          (customerId <= 0 || o.CustomerId == customerId) &&
                          (!initialOrderStatus.HasValue || o.OrderStatusId == (int)initialOrderStatus.Value)
                    select rp;
        }

        query = query.OrderByDescending(rp => rp.StartDateUtc).ThenByDescending(rp => rp.Id);

        return Task.FromResult<IPagedList<RecurringPayment>>(new PagedList<RecurringPayment>(query, pageIndex, pageSize));
    }

    public Task<IList<RecurringPaymentHistory>> GetRecurringPaymentHistoryAsync(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        return Task.FromResult<IList<RecurringPaymentHistory>>(
            recurringPaymentHistoryRepository.Table
                .Where(h => h.RecurringPaymentId == recurringPayment.Id)
                .OrderBy(h => h.CreatedOnUtc)
                .ToList());
    }

    public Task InsertRecurringPaymentHistoryAsync(RecurringPaymentHistory recurringPaymentHistory)
    {
        ArgumentNullException.ThrowIfNull(recurringPaymentHistory);
        recurringPaymentHistoryRepository.Insert(recurringPaymentHistory);
        return Task.CompletedTask;
    }
}
