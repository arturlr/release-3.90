using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Shipping;

public interface IShippingService
{
    // Shipping methods
    Task DeleteShippingMethodAsync(ShippingMethod shippingMethod);
    Task<ShippingMethod?> GetShippingMethodByIdAsync(int shippingMethodId);
    Task<IList<ShippingMethod>> GetAllShippingMethodsAsync(int? filterByCountryId = null);
    Task InsertShippingMethodAsync(ShippingMethod shippingMethod);
    Task UpdateShippingMethodAsync(ShippingMethod shippingMethod);

    // Warehouses
    Task DeleteWarehouseAsync(Warehouse warehouse);
    Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId);
    Task<IList<Warehouse>> GetAllWarehousesAsync();
    Task InsertWarehouseAsync(Warehouse warehouse);
    Task UpdateWarehouseAsync(Warehouse warehouse);

    // Nearest warehouse
    Task<Warehouse?> GetNearestWarehouseAsync(Address? address, IList<Warehouse>? warehouses = null);

    // Country restrictions
    Task<IList<ShippingMethodCountryMapping>> GetAllShippingMethodCountryMappingsAsync();
    Task InsertShippingMethodCountryMappingAsync(ShippingMethodCountryMapping mapping);
    Task DeleteShippingMethodCountryMappingAsync(ShippingMethodCountryMapping mapping);
}
