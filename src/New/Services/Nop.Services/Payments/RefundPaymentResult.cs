using Nop.Core.Domain.Payments;

namespace Nop.Services.Payments;

public class RefundPaymentResult
{
    public List<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;
    public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;

    public void AddError(string error) => Errors.Add(error);
}
