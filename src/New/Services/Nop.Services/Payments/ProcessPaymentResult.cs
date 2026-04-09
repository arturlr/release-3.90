using Nop.Core.Domain.Payments;

namespace Nop.Services.Payments;

public class ProcessPaymentResult
{
    public List<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;

    public string? AvsResult { get; set; }
    public string? AuthorizationTransactionId { get; set; }
    public string? AuthorizationTransactionCode { get; set; }
    public string? AuthorizationTransactionResult { get; set; }
    public string? CaptureTransactionId { get; set; }
    public string? CaptureTransactionResult { get; set; }
    public string? SubscriptionTransactionId { get; set; }
    public bool AllowStoringCreditCardNumber { get; set; }
    public bool RecurringPaymentFailed { get; set; }
    public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;

    public void AddError(string error) => Errors.Add(error);
}
