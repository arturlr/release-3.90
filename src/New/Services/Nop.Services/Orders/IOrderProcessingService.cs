using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Payments;

namespace Nop.Services.Orders;

public interface IOrderProcessingService
{
    // Order placement
    Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest);

    // Order status
    Task CheckOrderStatusAsync(Order order);

    // Order deletion
    Task DeleteOrderAsync(Order order);

    // Recurring payments
    Task<IList<string>> ProcessNextRecurringPaymentAsync(RecurringPayment recurringPayment, ProcessPaymentResult? paymentResult = null);
    Task<IList<string>> CancelRecurringPaymentAsync(RecurringPayment recurringPayment);
    bool CanCancelRecurringPayment(Customer customerToValidate, RecurringPayment recurringPayment, Order? initialOrder);
    bool CanRetryLastRecurringPayment(Customer customer, RecurringPayment recurringPayment, Order? initialOrder);

    // Shipping
    Task ShipAsync(Shipment shipment, bool notifyCustomer);
    Task DeliverAsync(Shipment shipment, bool notifyCustomer);

    // Cancel
    bool CanCancelOrder(Order order);
    Task CancelOrderAsync(Order order, bool notifyCustomer);

    // Authorize
    bool CanMarkOrderAsAuthorized(Order order);
    Task MarkAsAuthorizedAsync(Order order);

    // Capture
    bool CanCapture(Order order);
    Task<IList<string>> CaptureAsync(Order order);

    // Mark as paid
    bool CanMarkOrderAsPaid(Order order);
    Task MarkOrderAsPaidAsync(Order order);

    // Refund (online)
    bool CanRefund(Order order);
    Task<IList<string>> RefundAsync(Order order);

    // Refund (offline)
    bool CanRefundOffline(Order order);
    Task RefundOfflineAsync(Order order);

    // Partial refund (online)
    bool CanPartiallyRefund(Order order, decimal amountToRefund);
    Task<IList<string>> PartiallyRefundAsync(Order order, decimal amountToRefund);

    // Partial refund (offline)
    bool CanPartiallyRefundOffline(Order order, decimal amountToRefund);
    Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund);

    // Void (online)
    bool CanVoid(Order order);
    Task<IList<string>> VoidAsync(Order order);

    // Void (offline)
    bool CanVoidOffline(Order order);
    Task VoidOfflineAsync(Order order);

    // Reorder
    Task ReOrderAsync(Order order);

    // Return request
    Task<bool> IsReturnRequestAllowedAsync(Order order);

    // Validation
    Task<bool> ValidateMinOrderSubtotalAmountAsync(IList<ShoppingCartItem> cart);
    Task<bool> ValidateMinOrderTotalAmountAsync(IList<ShoppingCartItem> cart);
    Task<bool> IsPaymentWorkflowRequiredAsync(IList<ShoppingCartItem> cart, bool? useRewardPoints = null);
}
