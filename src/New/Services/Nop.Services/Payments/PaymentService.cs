using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;

namespace Nop.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly ISettingService _settingService;
    private readonly IEnumerable<IPaymentMethod> _paymentMethods;
    private readonly PaymentSettings _paymentSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    public PaymentService(
        ISettingService settingService,
        IEnumerable<IPaymentMethod> paymentMethods,
        PaymentSettings paymentSettings,
        ShoppingCartSettings shoppingCartSettings)
    {
        _settingService = settingService;
        _paymentMethods = paymentMethods;
        _paymentSettings = paymentSettings;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #region Helpers

    protected virtual IPaymentMethod? LoadPaymentMethodBySystemName(string systemName)
    {
        if (string.IsNullOrEmpty(systemName))
            return null;

        return _paymentMethods.FirstOrDefault(pm =>
            pm.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Restrictions

    public virtual async Task<IList<int>> GetRestrictedCountryIdsAsync(string paymentMethodSystemName)
    {
        ArgumentException.ThrowIfNullOrEmpty(paymentMethodSystemName);

        var key = $"PaymentMethodRestictions.{paymentMethodSystemName}";
        var ids = await _settingService.GetSettingByKeyAsync<List<int>>(key);
        return ids ?? [];
    }

    public virtual async Task SaveRestrictedCountryIdsAsync(string paymentMethodSystemName, List<int> countryIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(paymentMethodSystemName);

        var key = $"PaymentMethodRestictions.{paymentMethodSystemName}";
        await _settingService.SetSettingAsync(key, countryIds);
    }

    #endregion

    #region Processing

    public virtual async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OrderTotal == decimal.Zero)
            return new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Paid };

        // Strip whitespace/dashes from credit card number
        if (!string.IsNullOrWhiteSpace(request.CreditCardNumber))
            request.CreditCardNumber = request.CreditCardNumber.Replace(" ", "").Replace("-", "");

        var paymentMethod = LoadPaymentMethodBySystemName(request.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        return await paymentMethod.ProcessPaymentAsync(request);
    }

    public virtual async Task PostProcessPaymentAsync(PostProcessPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Order.PaymentStatus == PaymentStatus.Paid)
            return;

        var paymentMethod = LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        await paymentMethod.PostProcessPaymentAsync(request);
    }

    public virtual async Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (!_paymentSettings.AllowRePostingPayments)
            return false;

        var paymentMethod = LoadPaymentMethodBySystemName(order.PaymentMethodSystemName!);
        if (paymentMethod == null)
            return false;

        if (paymentMethod.PaymentMethodType != PaymentMethodType.Redirection)
            return false;

        if (order.Deleted || order.OrderStatus == OrderStatus.Cancelled)
            return false;

        if (order.PaymentStatus != PaymentStatus.Pending)
            return false;

        return await paymentMethod.CanRePostProcessPaymentAsync(order);
    }

    public virtual async Task<decimal> GetAdditionalHandlingFeeAsync(
        IList<ShoppingCartItem> cart, string paymentMethodSystemName)
    {
        if (string.IsNullOrEmpty(paymentMethodSystemName))
            return decimal.Zero;

        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        if (paymentMethod == null)
            return decimal.Zero;

        var result = await paymentMethod.GetAdditionalHandlingFeeAsync(cart);
        if (result < decimal.Zero)
            result = decimal.Zero;

        if (_shoppingCartSettings.RoundPricesDuringCalculation)
            result = Math.Round(result, 2);

        return result;
    }

    public virtual Task<bool> SupportCaptureAsync(string paymentMethodSystemName)
    {
        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        return Task.FromResult(paymentMethod?.SupportCapture ?? false);
    }

    public virtual async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentMethod = LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        return await paymentMethod.CaptureAsync(request);
    }

    public virtual Task<bool> SupportPartiallyRefundAsync(string paymentMethodSystemName)
    {
        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        return Task.FromResult(paymentMethod?.SupportPartiallyRefund ?? false);
    }

    public virtual Task<bool> SupportRefundAsync(string paymentMethodSystemName)
    {
        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        return Task.FromResult(paymentMethod?.SupportRefund ?? false);
    }

    public virtual async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentMethod = LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        return await paymentMethod.RefundAsync(request);
    }

    public virtual Task<bool> SupportVoidAsync(string paymentMethodSystemName)
    {
        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        return Task.FromResult(paymentMethod?.SupportVoid ?? false);
    }

    public virtual async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentMethod = LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        return await paymentMethod.VoidAsync(request);
    }

    public virtual Task<RecurringPaymentType> GetRecurringPaymentTypeAsync(string paymentMethodSystemName)
    {
        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        return Task.FromResult(paymentMethod?.RecurringPaymentType ?? RecurringPaymentType.NotSupported);
    }

    public virtual async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OrderTotal == decimal.Zero)
            return new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Paid };

        var paymentMethod = LoadPaymentMethodBySystemName(request.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        return await paymentMethod.ProcessRecurringPaymentAsync(request);
    }

    public virtual async Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(
        CancelRecurringPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Order.OrderTotal == decimal.Zero)
            return new CancelRecurringPaymentResult();

        var paymentMethod = LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName!)
            ?? throw new NopException("Payment method couldn't be loaded");

        return await paymentMethod.CancelRecurringPaymentAsync(request);
    }

    public virtual string GetMaskedCreditCardNumber(string creditCardNumber)
    {
        if (string.IsNullOrEmpty(creditCardNumber))
            return string.Empty;

        if (creditCardNumber.Length <= 4)
            return creditCardNumber;

        var last4 = creditCardNumber[^4..];
        return new string('*', creditCardNumber.Length - 4) + last4;
    }

    #endregion
}
