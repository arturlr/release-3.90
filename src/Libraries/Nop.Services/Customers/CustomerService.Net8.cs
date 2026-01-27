using System;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Microsoft.EntityFrameworkCore;

namespace Nop.Services.Customers
{
    /// <summary>
    /// Customer service implementation
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly IRepository<Customer> _customerRepository;

        public CustomerService(IRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
        }

        /// <summary>
        /// Gets a customer by ID
        /// </summary>
        public virtual async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            if (customerId == 0)
                return null;

            return await _customerRepository.GetByIdAsync(customerId);
        }

        /// <summary>
        /// Gets a customer by GUID
        /// </summary>
        public virtual async Task<Customer?> GetCustomerByGuidAsync(Guid customerGuid)
        {
            if (customerGuid == Guid.Empty)
                return null;

            return await _customerRepository.Table
                .FirstOrDefaultAsync(c => c.CustomerGuid == customerGuid);
        }

        /// <summary>
        /// Gets a customer by email
        /// </summary>
        public virtual async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _customerRepository.Table
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        /// <summary>
        /// Gets a customer by username
        /// </summary>
        public virtual async Task<Customer?> GetCustomerByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await _customerRepository.Table
                .FirstOrDefaultAsync(c => c.Username == username);
        }

        /// <summary>
        /// Inserts a customer
        /// </summary>
        public virtual async Task InsertCustomerAsync(Customer customer)
        {
            await _customerRepository.InsertAsync(customer);
        }

        /// <summary>
        /// Updates a customer
        /// </summary>
        public virtual async Task UpdateCustomerAsync(Customer customer)
        {
            await _customerRepository.UpdateAsync(customer);
        }

        /// <summary>
        /// Deletes a customer
        /// </summary>
        public virtual async Task DeleteCustomerAsync(Customer customer)
        {
            await _customerRepository.DeleteAsync(customer);
        }

        /// <summary>
        /// Validates customer credentials
        /// </summary>
        public virtual async Task<bool> ValidateCustomerAsync(string email, string password)
        {
            var customer = await GetCustomerByEmailAsync(email);
            if (customer == null || !customer.Active)
                return false;

            // TODO: Implement password hashing validation
            // For now, just return true if customer exists
            return true;
        }
    }
}
