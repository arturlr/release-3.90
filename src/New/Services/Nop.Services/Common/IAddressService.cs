using Nop.Core.Domain.Common;

namespace Nop.Services.Common;

public interface IAddressService
{
    Task<Address?> GetAddressByIdAsync(int addressId);
    Task InsertAddressAsync(Address address);
    Task UpdateAddressAsync(Address address);
    Task DeleteAddressAsync(Address address);
    Task<int> GetAddressTotalByCountryIdAsync(int countryId);
    Task<int> GetAddressTotalByStateProvinceIdAsync(int stateProvinceId);
    Task<bool> IsAddressValidAsync(Address address);
}
