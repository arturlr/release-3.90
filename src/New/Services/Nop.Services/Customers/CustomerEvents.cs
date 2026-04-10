using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public class CustomerLoggedinEvent(Customer customer)
{
    public Customer Customer { get; } = customer;
}

public class CustomerLoggedOutEvent(Customer customer)
{
    public Customer Customer { get; } = customer;
}

public class CustomerRegisteredEvent(Customer customer)
{
    public Customer Customer { get; } = customer;
}
