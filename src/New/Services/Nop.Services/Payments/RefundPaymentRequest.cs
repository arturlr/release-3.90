using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

public class RefundPaymentRequest
{
    public required Order Order { get; set; }
    public decimal AmountToRefund { get; set; }
    public bool IsPartialRefund { get; set; }
}
