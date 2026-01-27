using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;

namespace Nop.Services.Common
{
    public interface IAddressService
    {
        Task<Address> GetAddressByIdAsync(int addressId);
        Task<IList<Address>> GetAddressesByCustomerIdAsync(int customerId);
        Task InsertAddressAsync(Address address);
        Task UpdateAddressAsync(Address address);
        Task DeleteAddressAsync(Address address);
    }
}
