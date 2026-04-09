using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Services.Directory;
using Nop.Services.Events;

namespace Nop.Services.Common;

public class AddressService : IAddressService
{
    private const string ByIdKey = "Nop.address.id-{0}";
    private const string AddressesPrefix = "Nop.address.";

    private readonly IRepository<Address> _addressRepository;
    private readonly ICountryService _countryService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly AddressSettings _addressSettings;

    public AddressService(
        IRepository<Address> addressRepository,
        ICountryService countryService,
        IStateProvinceService stateProvinceService,
        IAddressAttributeService addressAttributeService,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        AddressSettings addressSettings)
    {
        _addressRepository = addressRepository;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
        _addressAttributeService = addressAttributeService;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _addressSettings = addressSettings;
    }

    public virtual async Task<Address?> GetAddressByIdAsync(int addressId)
    {
        if (addressId == 0)
            return null;
        var key = new CacheKey(string.Format(ByIdKey, addressId), AddressesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_addressRepository.GetById(addressId)));
    }

    public virtual async Task InsertAddressAsync(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        address.CreatedOnUtc = DateTime.UtcNow;
        if (address.CountryId == 0) address.CountryId = null;
        if (address.StateProvinceId == 0) address.StateProvinceId = null;

        _addressRepository.Insert(address);
        await _cacheManager.RemoveByPrefixAsync(AddressesPrefix);
        await _eventPublisher.EntityInsertedAsync(address);
    }

    public virtual async Task UpdateAddressAsync(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.CountryId == 0) address.CountryId = null;
        if (address.StateProvinceId == 0) address.StateProvinceId = null;

        _addressRepository.Update(address);
        await _cacheManager.RemoveByPrefixAsync(AddressesPrefix);
        await _eventPublisher.EntityUpdatedAsync(address);
    }

    public virtual async Task DeleteAddressAsync(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        _addressRepository.Delete(address);
        await _cacheManager.RemoveByPrefixAsync(AddressesPrefix);
        await _eventPublisher.EntityDeletedAsync(address);
    }

    public virtual Task<int> GetAddressTotalByCountryIdAsync(int countryId)
    {
        if (countryId == 0)
            return Task.FromResult(0);
        return Task.FromResult(_addressRepository.TableNoTracking.Count(a => a.CountryId == countryId));
    }

    public virtual Task<int> GetAddressTotalByStateProvinceIdAsync(int stateProvinceId)
    {
        if (stateProvinceId == 0)
            return Task.FromResult(0);
        return Task.FromResult(_addressRepository.TableNoTracking.Count(a => a.StateProvinceId == stateProvinceId));
    }

    public virtual async Task<bool> IsAddressValidAsync(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrWhiteSpace(address.FirstName))
            return false;
        if (string.IsNullOrWhiteSpace(address.LastName))
            return false;
        if (string.IsNullOrWhiteSpace(address.Email))
            return false;

        if (_addressSettings.CompanyEnabled && _addressSettings.CompanyRequired &&
            string.IsNullOrWhiteSpace(address.Company))
            return false;

        if (_addressSettings.StreetAddressEnabled && _addressSettings.StreetAddressRequired &&
            string.IsNullOrWhiteSpace(address.Address1))
            return false;

        if (_addressSettings.StreetAddress2Enabled && _addressSettings.StreetAddress2Required &&
            string.IsNullOrWhiteSpace(address.Address2))
            return false;

        if (_addressSettings.ZipPostalCodeEnabled && _addressSettings.ZipPostalCodeRequired &&
            string.IsNullOrWhiteSpace(address.ZipPostalCode))
            return false;

        if (_addressSettings.CountryEnabled)
        {
            if (address.CountryId is null or 0)
                return false;

            var country = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
            if (country == null)
                return false;

            if (_addressSettings.StateProvinceEnabled)
            {
                var states = await _stateProvinceService.GetStateProvincesByCountryIdAsync(country.Id);
                if (states.Count > 0)
                {
                    if (address.StateProvinceId is null or 0)
                        return false;
                    if (!states.Any(x => x.Id == address.StateProvinceId.Value))
                        return false;
                }
            }
        }

        if (_addressSettings.CityEnabled && _addressSettings.CityRequired &&
            string.IsNullOrWhiteSpace(address.City))
            return false;

        if (_addressSettings.PhoneEnabled && _addressSettings.PhoneRequired &&
            string.IsNullOrWhiteSpace(address.PhoneNumber))
            return false;

        if (_addressSettings.FaxEnabled && _addressSettings.FaxRequired &&
            string.IsNullOrWhiteSpace(address.FaxNumber))
            return false;

        // if any required custom attributes exist, address is invalid
        // (full custom attribute validation requires the XML — this is a basic check)
        var attributes = await _addressAttributeService.GetAllAddressAttributesAsync();
        if (attributes.Any(x => x.IsRequired))
            return false;

        return true;
    }
}
