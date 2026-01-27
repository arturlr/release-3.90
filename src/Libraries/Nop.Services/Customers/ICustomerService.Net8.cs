using System;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers
{
    /// <summary>
    /// Customer service interface
    /// </summary>
    public interface ICustomerService
    {
        Task<Customer?> GetCustomerByIdAsync(int customerId);
        Task<Customer?> GetCustomerByGuidAsync(Guid customerGuid);
        Task<Customer?> GetCustomerByEmailAsync(string email);
        Task<Customer?> GetCustomerByUsernameAsync(string username);
        Task InsertCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(Customer customer);
        Task<bool> ValidateCustomerAsync(string email, string password);
    }
}
