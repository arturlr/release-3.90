using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Shipping;

public interface IShipmentService
{
    Task DeleteShipmentAsync(Shipment shipment);

    Task<IPagedList<Shipment>> GetAllShipmentsAsync(
        int vendorId = 0,
        int warehouseId = 0,
        int shippingCountryId = 0,
        int shippingStateId = 0,
        string? shippingCity = null,
        string? trackingNumber = null,
        bool loadNotShipped = false,
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue);

    Task<IList<Shipment>> GetShipmentsByIdsAsync(int[] shipmentIds);

    Task<Shipment?> GetShipmentByIdAsync(int shipmentId);

    Task InsertShipmentAsync(Shipment shipment);

    Task UpdateShipmentAsync(Shipment shipment);

    Task DeleteShipmentItemAsync(ShipmentItem shipmentItem);

    Task<ShipmentItem?> GetShipmentItemByIdAsync(int shipmentItemId);

    Task InsertShipmentItemAsync(ShipmentItem shipmentItem);

    Task UpdateShipmentItemAsync(ShipmentItem shipmentItem);

    Task<IList<ShipmentItem>> GetShipmentItemsByShipmentIdAsync(int shipmentId);

    Task<IList<Shipment>> GetShipmentsByOrderIdAsync(int orderId);

    Task<int> GetQuantityInShipmentsAsync(Product product, int warehouseId, bool ignoreShipped, bool ignoreDelivered);
}
