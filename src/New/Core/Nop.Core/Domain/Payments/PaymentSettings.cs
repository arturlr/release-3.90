using Nop.Core.Configuration;

namespace Nop.Core.Domain.Payments;

public class PaymentSettings : ISettings
{
    public List<string> ActivePaymentMethodSystemNames { get; set; } = [];
    public bool AllowRePostingPayments { get; set; }
    public bool BypassPaymentMethodSelectionIfOnlyOne { get; set; }
    public bool ShowPaymentMethodDescriptions { get; set; }
    public bool SkipPaymentInfoStepForRedirectionPaymentMethods { get; set; }
    public bool CancelRecurringPaymentsAfterFailedPayment { get; set; }
}
