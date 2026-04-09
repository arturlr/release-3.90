using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Events;

namespace Nop.Services.Shipping;

public class ShippingService : IShippingService
{
    private const string WarehousesByIdKey = "Nop.warehouse.id-{0}";
    private const string WarehousesPrefix = "Nop.warehouse.";

    private readonly IRepository<ShippingMethod> _shippingMethodRepository;
    private readonly IRepository<ShippingMethodCountryMapping> _shippingMethodCountryMappingRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IAddressService _addressService;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public ShippingService(
        IRepository<ShippingMethod> shippingMethodRepository,
        IRepository<ShippingMethodCountryMapping> shippingMethodCountryMappingRepository,
        IRepository<Warehouse> warehouseRepository,
        IAddressService addressService,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _shippingMethodCountryMappingRepository = shippingMethodCountryMappingRepository;
        _warehouseRepository = warehouseRepository;
        _addressService = addressService;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    #region Shipping methods

    public async Task DeleteShippingMethodAsync(ShippingMethod shippingMethod)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);
        _shippingMethodRepository.Delete(shippingMethod);
        await _eventPublisher.EntityDeletedAsync(shippingMethod);
    }

    public Task<ShippingMethod?> GetShippingMethodByIdAsync(int shippingMethodId)
    {
        return Task.FromResult(shippingMethodId == 0 ? null : _shippingMethodRepository.GetById(shippingMethodId));
    }

    public Task<IList<ShippingMethod>> GetAllShippingMethodsAsync(int? filterByCountryId = null)
    {
        if (filterByCountryId.HasValue && filterByCountryId.Value > 0)
        {
            // Exclude shipping methods that are restricted to the specified country
            var restrictedMethodIds = _shippingMethodCountryMappingRepository.TableNoTracking
                .Where(m => m.CountryId == filterByCountryId.Value)
                .Select(m => m.ShippingMethodId);

            var query = from sm in _shippingMethodRepository.Table
                        where !restrictedMethodIds.Contains(sm.Id)
                        orderby sm.DisplayOrder, sm.Id
                        select sm;

            return Task.FromResult<IList<ShippingMethod>>(query.ToList());
        }

        var allQuery = from sm in _shippingMethodRepository.Table
                       orderby sm.DisplayOrder, sm.Id
                       select sm;
        return Task.FromResult<IList<ShippingMethod>>(allQuery.ToList());
    }

    public async Task InsertShippingMethodAsync(ShippingMethod shippingMethod)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);
        _shippingMethodRepository.Insert(shippingMethod);
        await _eventPublisher.EntityInsertedAsync(shippingMethod);
    }

    public async Task UpdateShippingMethodAsync(ShippingMethod shippingMethod)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);
        _shippingMethodRepository.Update(shippingMethod);
        await _eventPublisher.EntityUpdatedAsync(shippingMethod);
    }

    #endregion

    #region Warehouses

    public async Task DeleteWarehouseAsync(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        _warehouseRepository.Delete(warehouse);
        await _cacheManager.RemoveByPrefixAsync(WarehousesPrefix);
        await _eventPublisher.EntityDeletedAsync(warehouse);
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId)
    {
        if (warehouseId == 0)
            return null;

        var key = new CacheKey(string.Format(WarehousesByIdKey, warehouseId), WarehousesPrefix);
        return await _cacheManager.GetAsync(key, () => Task.FromResult(_warehouseRepository.GetById(warehouseId)));
    }

    public Task<IList<Warehouse>> GetAllWarehousesAsync()
    {
        var query = from wh in _warehouseRepository.Table
                    orderby wh.Name
                    select wh;
        return Task.FromResult<IList<Warehouse>>(query.ToList());
    }

    public async Task InsertWarehouseAsync(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        _warehouseRepository.Insert(warehouse);
        await _cacheManager.RemoveByPrefixAsync(WarehousesPrefix);
        await _eventPublisher.EntityInsertedAsync(warehouse);
    }

    public async Task UpdateWarehouseAsync(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        _warehouseRepository.Update(warehouse);
        await _cacheManager.RemoveByPrefixAsync(WarehousesPrefix);
        await _eventPublisher.EntityUpdatedAsync(warehouse);
    }

    #endregion

    #region Nearest warehouse

    public async Task<Warehouse?> GetNearestWarehouseAsync(Address? address, IList<Warehouse>? warehouses = null)
    {
        warehouses ??= await GetAllWarehousesAsync();

        if (address == null)
            return warehouses.FirstOrDefault();

        // Match by country
        var matchedByCountry = new List<Warehouse>();
        foreach (var warehouse in warehouses)
        {
            var warehouseAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
            if (warehouseAddress?.CountryId == address.CountryId)
                matchedByCountry.Add(warehouse);
        }

        if (matchedByCountry.Count == 0)
            return warehouses.FirstOrDefault();

        // Match by state
        foreach (var warehouse in matchedByCountry)
        {
            var warehouseAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
            if (warehouseAddress?.StateProvinceId == address.StateProvinceId)
                return warehouse;
        }

        return matchedByCountry.First();
    }

    #endregion
}
