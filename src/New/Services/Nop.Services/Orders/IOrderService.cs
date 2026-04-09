using Nop.Core;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface IOrderService
{
    // Orders
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<Order?> GetOrderByCustomOrderNumberAsync(string customOrderNumber);
    Task<IList<Order>> GetOrdersByIdsAsync(int[] orderIds);
    Task<Order?> GetOrderByGuidAsync(Guid orderGuid);
    Task DeleteOrderAsync(Order order);
    Task<IPagedList<Order>> SearchOrdersAsync(int storeId = 0, int vendorId = 0, int customerId = 0,
        int productId = 0, int affiliateId = 0, int warehouseId = 0, int billingCountryId = 0,
        string? paymentMethodSystemName = null, DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        List<int>? osIds = null, List<int>? psIds = null, List<int>? ssIds = null,
        string? billingEmail = null, string? billingLastName = null, string? orderNotes = null,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertOrderAsync(Order order);
    Task UpdateOrderAsync(Order order);
    Task<Order?> GetOrderByAuthorizationTransactionIdAndPaymentMethodAsync(string authorizationTransactionId, string paymentMethodSystemName);

    // Order items
    Task<OrderItem?> GetOrderItemByIdAsync(int orderItemId);
    Task<OrderItem?> GetOrderItemByGuidAsync(Guid orderItemGuid);
    Task<IList<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<IList<OrderItem>> GetDownloadableOrderItemsAsync(int customerId);
    Task DeleteOrderItemAsync(OrderItem orderItem);
    Task InsertOrderItemAsync(OrderItem orderItem);

    // Order notes
    Task<OrderNote?> GetOrderNoteByIdAsync(int orderNoteId);
    Task<IList<OrderNote>> GetOrderNotesByOrderIdAsync(int orderId, bool? displayToCustomer = null);
    Task DeleteOrderNoteAsync(OrderNote orderNote);
    Task InsertOrderNoteAsync(OrderNote orderNote);

    // Recurring payments
    Task DeleteRecurringPaymentAsync(RecurringPayment recurringPayment);
    Task<RecurringPayment?> GetRecurringPaymentByIdAsync(int recurringPaymentId);
    Task InsertRecurringPaymentAsync(RecurringPayment recurringPayment);
    Task UpdateRecurringPaymentAsync(RecurringPayment recurringPayment);
    Task<IPagedList<RecurringPayment>> SearchRecurringPaymentsAsync(int storeId = 0, int customerId = 0,
        int initialOrderId = 0, OrderStatus? initialOrderStatus = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    Task<IList<RecurringPaymentHistory>> GetRecurringPaymentHistoryAsync(RecurringPayment recurringPayment);
    Task InsertRecurringPaymentHistoryAsync(RecurringPaymentHistory recurringPaymentHistory);
}
