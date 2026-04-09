using Nop.Core.Domain.Orders;

namespace Nop.Services.Payments;

public class CancelRecurringPaymentRequest
{
    public required Order Order { get; set; }
}
