using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

public interface IPaymentService
{
    // Restriction methods (use ISettingService, no plugin dependency)
    Task<IList<int>> GetRestrictedCountryIdsAsync(string paymentMethodSystemName);
    Task SaveRestrictedCountryIdsAsync(string paymentMethodSystemName, List<int> countryIds);

    // Processing methods (delegate to IPaymentMethod resolved by system name)
    Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task PostProcessPaymentAsync(PostProcessPaymentRequest request);
    Task<bool> CanRePostProcessPaymentAsync(Order order);
    Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart, string paymentMethodSystemName);
    Task<bool> SupportCaptureAsync(string paymentMethodSystemName);
    Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request);
    Task<bool> SupportPartiallyRefundAsync(string paymentMethodSystemName);
    Task<bool> SupportRefundAsync(string paymentMethodSystemName);
    Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request);
    Task<bool> SupportVoidAsync(string paymentMethodSystemName);
    Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request);
    Task<RecurringPaymentType> GetRecurringPaymentTypeAsync(string paymentMethodSystemName);
    Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request);
    Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest request);

    string GetMaskedCreditCardNumber(string creditCardNumber);
}
