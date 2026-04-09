using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface ICustomNumberFormatter
{
    string GenerateOrderCustomNumber(Order order);
    string GenerateReturnRequestCustomNumber(ReturnRequest returnRequest);
}
