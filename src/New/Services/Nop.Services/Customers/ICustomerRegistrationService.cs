using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public interface ICustomerRegistrationService
{
    Task<CustomerLoginResults> ValidateCustomerAsync(string usernameOrEmail, string password);
    Task<CustomerRegistrationResult> RegisterCustomerAsync(CustomerRegistrationRequest request);
    Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task SetEmailAsync(Customer customer, string newEmail, bool requireValidation);
    Task SetUsernameAsync(Customer customer, string newUsername);
}
