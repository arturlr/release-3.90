using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

public class VoidPaymentRequest
{
    public required Order Order { get; set; }
}
