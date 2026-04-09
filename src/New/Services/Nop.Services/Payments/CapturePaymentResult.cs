using Nop.Core.Domain.Payments;

namespace Nop.Services.Payments;

public class CapturePaymentResult
{
    public List<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;

    public string? CaptureTransactionId { get; set; }
    public string? CaptureTransactionResult { get; set; }
    public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;

    public void AddError(string error) => Errors.Add(error);
}
