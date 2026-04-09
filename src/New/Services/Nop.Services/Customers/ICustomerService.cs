using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Customers;

public interface ICustomerService
{
    // Customers
    Task<IPagedList<Customer>> GetAllCustomersAsync(
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int affiliateId = 0, int vendorId = 0, int[]? customerRoleIds = null,
        string? email = null, string? username = null,
        string? firstName = null, string? lastName = null,
        int dayOfBirth = 0, int monthOfBirth = 0,
        string? company = null, string? phone = null, string? zipPostalCode = null,
        string? ipAddress = null,
        bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null,
        int pageIndex = 0, int pageSize = int.MaxValue);

    Task<IPagedList<Customer>> GetOnlineCustomersAsync(DateTime lastActivityFromUtc,
        int[]? customerRoleIds = null, int pageIndex = 0, int pageSize = int.MaxValue);

    Task DeleteCustomerAsync(Customer customer);
    Task<Customer?> GetCustomerByIdAsync(int customerId);
    Task<IList<Customer>> GetCustomersByIdsAsync(int[] customerIds);
    Task<Customer?> GetCustomerByGuidAsync(Guid customerGuid);
    Task<Customer?> GetCustomerByEmailAsync(string email);
    Task<Customer?> GetCustomerBySystemNameAsync(string systemName);
    Task<Customer?> GetCustomerByUsernameAsync(string username);
    Task<Customer> InsertGuestCustomerAsync();
    Task InsertCustomerAsync(Customer customer);
    Task UpdateCustomerAsync(Customer customer);
    Task ResetCheckoutDataAsync(Customer customer, int storeId,
        bool clearCouponCodes = false, bool clearCheckoutAttributes = false,
        bool clearRewardPoints = true, bool clearShippingMethod = true,
        bool clearPaymentMethod = true);
    Task<int> DeleteGuestCustomersAsync(DateTime? createdFromUtc, DateTime? createdToUtc, bool onlyWithoutShoppingCart);

    // Customer roles
    Task DeleteCustomerRoleAsync(CustomerRole customerRole);
    Task<CustomerRole?> GetCustomerRoleByIdAsync(int customerRoleId);
    Task<CustomerRole?> GetCustomerRoleBySystemNameAsync(string systemName);
    Task<IList<CustomerRole>> GetAllCustomerRolesAsync(bool showHidden = false);
    Task InsertCustomerRoleAsync(CustomerRole customerRole);
    Task UpdateCustomerRoleAsync(CustomerRole customerRole);

    // Customer role mappings
    Task AddCustomerRoleMappingAsync(CustomerCustomerRoleMapping mapping);
    Task RemoveCustomerRoleMappingAsync(Customer customer, CustomerRole role);
    Task<int[]> GetCustomerRoleIdsAsync(Customer customer, bool showHidden = false);

    // Customer passwords
    Task<IList<CustomerPassword>> GetCustomerPasswordsAsync(int? customerId = null,
        PasswordFormat? passwordFormat = null, int? passwordsToReturn = null);
    Task<CustomerPassword?> GetCurrentPasswordAsync(int customerId);
    Task InsertCustomerPasswordAsync(CustomerPassword customerPassword);
    Task UpdateCustomerPasswordAsync(CustomerPassword customerPassword);
}
