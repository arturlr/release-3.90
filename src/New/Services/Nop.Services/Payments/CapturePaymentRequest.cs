using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

public class CapturePaymentRequest
{
    public required Order Order { get; set; }
}
