using System.Threading.Tasks;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers
{
    public interface IAuthenticationService
    {
        Task SignInAsync(Customer customer, bool isPersistent);
        Task SignOutAsync();
        Task<Customer> GetAuthenticatedCustomerAsync();
    }
}
