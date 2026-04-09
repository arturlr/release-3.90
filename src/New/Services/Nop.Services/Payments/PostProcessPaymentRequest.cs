using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

public class PostProcessPaymentRequest
{
    public required Order Order { get; set; }
}
