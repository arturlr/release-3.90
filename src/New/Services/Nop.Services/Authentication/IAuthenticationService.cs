using Nop.Core.Domain.Customers;

namespace Nop.Services.Authentication;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Sign in a customer by creating an authentication cookie.
    /// </summary>
    /// <param name="customer">Customer to sign in</param>
    /// <param name="isPersistent">Whether the cookie should persist across browser sessions</param>
    Task SignInAsync(Customer customer, bool isPersistent);

    /// <summary>
    /// Sign out the current customer by removing the authentication cookie.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Get the currently authenticated customer from the request cookie.
    /// Returns null if not authenticated or customer is invalid.
    /// </summary>
    Task<Customer?> GetAuthenticatedCustomerAsync();
}
