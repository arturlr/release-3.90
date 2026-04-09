using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public class PlaceOrderResult
{
    public List<string> Errors { get; } = [];
    public bool Success => Errors.Count == 0;
    public Order? PlacedOrder { get; set; }

    public void AddError(string error) => Errors.Add(error);
}
