using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public class CustomerPasswordChangedEvent
{
    public CustomerPasswordChangedEvent(CustomerPassword password)
    {
        Password = password;
    }

    public CustomerPassword Password { get; }
}
