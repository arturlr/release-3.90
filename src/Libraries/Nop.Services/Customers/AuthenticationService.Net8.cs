using System.Threading.Tasks;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ICustomerService _customerService;

        public AuthenticationService(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public virtual async Task SignInAsync(Customer customer, bool isPersistent)
        {
            // TODO: Implement cookie authentication
            await Task.CompletedTask;
        }

        public virtual async Task SignOutAsync()
        {
            // TODO: Implement sign out
            await Task.CompletedTask;
        }

        public virtual async Task<Customer> GetAuthenticatedCustomerAsync()
        {
            // TODO: Get customer from authentication cookie
            return await Task.FromResult<Customer>(null);
        }
    }
}
