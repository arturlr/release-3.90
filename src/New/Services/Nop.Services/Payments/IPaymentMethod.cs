using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

/// <summary>
/// Payment method plugin interface. Actual implementations live in plugin projects.
/// Does NOT extend IPlugin — plugin infrastructure deferred to [2.10].
/// </summary>
public interface IPaymentMethod
{
    /// <summary>
    /// Gets the payment method system name (unique identifier).
    /// </summary>
    string SystemName { get; }

    /// <summary>
    /// Gets a payment method description displayed on checkout pages.
    /// </summary>
    string PaymentMethodDescription { get; }

    PaymentMethodType PaymentMethodType { get; }
    bool SupportCapture { get; }
    bool SupportPartiallyRefund { get; }
    bool SupportRefund { get; }
    bool SupportVoid { get; }
    RecurringPaymentType RecurringPaymentType { get; }
    bool SkipPaymentInfo { get; }

    Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task PostProcessPaymentAsync(PostProcessPaymentRequest request);
    Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart);
    Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart);
    Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request);
    Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request);
    Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request);
    Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request);
    Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest request);
    Task<bool> CanRePostProcessPaymentAsync(Order order);
}
