using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Common;
using Nop.Data;

namespace Nop.Services.Common
{
    public class AddressService : IAddressService
    {
        private readonly IRepository<Address> _addressRepository;

        public AddressService(IRepository<Address> addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public virtual async Task<Address> GetAddressByIdAsync(int addressId)
        {
            if (addressId == 0)
                return null;

            return await _addressRepository.GetByIdAsync(addressId);
        }

        public virtual async Task<IList<Address>> GetAddressesByCustomerIdAsync(int customerId)
        {
            // Addresses are linked through Customer entity, not directly
            // For now, return all addresses (would need Customer navigation property)
            var query = _addressRepository.Table;
            return await query.ToListAsync();
        }

        public virtual async Task InsertAddressAsync(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            await _addressRepository.InsertAsync(address);
        }

        public virtual async Task UpdateAddressAsync(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            await _addressRepository.UpdateAsync(address);
        }

        public virtual async Task DeleteAddressAsync(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            await _addressRepository.DeleteAsync(address);
        }
    }
}
