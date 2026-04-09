using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

/// <summary>
/// Order events published during order lifecycle.
/// </summary>
public sealed class OrderPlacedEvent(Order order)
{
    public Order Order { get; } = order;
}

public sealed class OrderPaidEvent(Order order)
{
    public Order Order { get; } = order;
}

public sealed class OrderCancelledEvent(Order order)
{
    public Order Order { get; } = order;
}

public sealed class OrderRefundedEvent(Order order, decimal amount)
{
    public Order Order { get; } = order;
    public decimal Amount { get; } = amount;
}
