using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public class CustomerRegistrationRequest
{
    public CustomerRegistrationRequest(Customer customer, string email, string username,
        string password, PasswordFormat passwordFormat, int storeId, bool isApproved = true)
    {
        Customer = customer;
        Email = email;
        Username = username;
        Password = password;
        PasswordFormat = passwordFormat;
        StoreId = storeId;
        IsApproved = isApproved;
    }

    public Customer Customer { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public PasswordFormat PasswordFormat { get; set; }
    public int StoreId { get; set; }
    public bool IsApproved { get; set; }
}
