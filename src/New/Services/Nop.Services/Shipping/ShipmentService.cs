using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Events;

namespace Nop.Services.Shipping;

public class ShipmentService : IShipmentService
{
    private readonly IRepository<Shipment> _shipmentRepository;
    private readonly IRepository<ShipmentItem> _shipmentItemRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<Address> _addressRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IEventPublisher _eventPublisher;

    public ShipmentService(
        IRepository<Shipment> shipmentRepository,
        IRepository<ShipmentItem> shipmentItemRepository,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Address> addressRepository,
        IRepository<Product> productRepository,
        IEventPublisher eventPublisher)
    {
        _shipmentRepository = shipmentRepository;
        _shipmentItemRepository = shipmentItemRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _addressRepository = addressRepository;
        _productRepository = productRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task DeleteShipmentAsync(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        _shipmentRepository.Delete(shipment);
        await _eventPublisher.EntityDeletedAsync(shipment);
    }

    public Task<IPagedList<Shipment>> GetAllShipmentsAsync(
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
        int pageSize = int.MaxValue)
    {
        // Start with shipments joined to non-deleted orders
        var query = from s in _shipmentRepository.Table
                    join o in _orderRepository.TableNoTracking on s.OrderId equals o.Id
                    where !o.Deleted
                    select new { Shipment = s, Order = o };

        if (!string.IsNullOrEmpty(trackingNumber))
            query = query.Where(x => x.Shipment.TrackingNumber != null && x.Shipment.TrackingNumber.Contains(trackingNumber));

        if (loadNotShipped)
            query = query.Where(x => !x.Shipment.ShippedDateUtc.HasValue);

        if (createdFromUtc.HasValue)
            query = query.Where(x => createdFromUtc.Value <= x.Shipment.CreatedOnUtc);

        if (createdToUtc.HasValue)
            query = query.Where(x => createdToUtc.Value >= x.Shipment.CreatedOnUtc);

        // Address-based filtering requires joining Order → Address
        if (shippingCountryId > 0 || shippingStateId > 0 || !string.IsNullOrWhiteSpace(shippingCity))
        {
            var addresses = _addressRepository.TableNoTracking;
            var queryWithAddr = from x in query
                                join addr in addresses on x.Order.ShippingAddressId equals addr.Id
                                select new { x.Shipment, x.Order, Address = addr };

            if (shippingCountryId > 0)
                queryWithAddr = queryWithAddr.Where(x => x.Address.CountryId == shippingCountryId);

            if (shippingStateId > 0)
                queryWithAddr = queryWithAddr.Where(x => x.Address.StateProvinceId == shippingStateId);

            if (!string.IsNullOrWhiteSpace(shippingCity))
                queryWithAddr = queryWithAddr.Where(x => x.Address.City != null && x.Address.City.Contains(shippingCity));

            // Vendor filtering: shipment items → order items → products with VendorId
            if (vendorId > 0)
            {
                var vendorOrderItemIds = from oi in _orderItemRepository.TableNoTracking
                                         join p in _productRepository.TableNoTracking on oi.ProductId equals p.Id
                                         where p.VendorId == vendorId
                                         select oi.Id;

                queryWithAddr = from x in queryWithAddr
                                where (from si in _shipmentItemRepository.TableNoTracking
                                       where si.ShipmentId == x.Shipment.Id
                                       select si.OrderItemId).Any(oiId => vendorOrderItemIds.Contains(oiId))
                                select x;
            }

            if (warehouseId > 0)
            {
                queryWithAddr = from x in queryWithAddr
                                where (from si in _shipmentItemRepository.TableNoTracking
                                       where si.ShipmentId == x.Shipment.Id && si.WarehouseId == warehouseId
                                       select si).Any()
                                select x;
            }

            var finalQuery = queryWithAddr.Select(x => x.Shipment).OrderByDescending(s => s.CreatedOnUtc);
            return Task.FromResult<IPagedList<Shipment>>(new PagedList<Shipment>(finalQuery, pageIndex, pageSize));
        }

        // No address filtering needed — simpler path
        if (vendorId > 0)
        {
            var vendorOrderItemIds = from oi in _orderItemRepository.TableNoTracking
                                     join p in _productRepository.TableNoTracking on oi.ProductId equals p.Id
                                     where p.VendorId == vendorId
                                     select oi.Id;

            query = from x in query
                    where (from si in _shipmentItemRepository.TableNoTracking
                           where si.ShipmentId == x.Shipment.Id
                           select si.OrderItemId).Any(oiId => vendorOrderItemIds.Contains(oiId))
                    select x;
        }

        if (warehouseId > 0)
        {
            query = from x in query
                    where (from si in _shipmentItemRepository.TableNoTracking
                           where si.ShipmentId == x.Shipment.Id && si.WarehouseId == warehouseId
                           select si).Any()
                    select x;
        }

        var shipmentQuery = query.Select(x => x.Shipment).OrderByDescending(s => s.CreatedOnUtc);
        return Task.FromResult<IPagedList<Shipment>>(new PagedList<Shipment>(shipmentQuery, pageIndex, pageSize));
    }

    public Task<IList<Shipment>> GetShipmentsByIdsAsync(int[] shipmentIds)
    {
        if (shipmentIds == null || shipmentIds.Length == 0)
            return Task.FromResult<IList<Shipment>>([]);

        var shipments = _shipmentRepository.Table
            .Where(s => shipmentIds.Contains(s.Id))
            .ToList();

        // Sort by passed identifiers
        var sorted = new List<Shipment>();
        foreach (var id in shipmentIds)
        {
            var shipment = shipments.Find(x => x.Id == id);
            if (shipment != null)
                sorted.Add(shipment);
        }
        return Task.FromResult<IList<Shipment>>(sorted);
    }

    public Task<Shipment?> GetShipmentByIdAsync(int shipmentId)
    {
        return Task.FromResult(shipmentId == 0 ? null : _shipmentRepository.GetById(shipmentId));
    }

    public async Task InsertShipmentAsync(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        _shipmentRepository.Insert(shipment);
        await _eventPublisher.EntityInsertedAsync(shipment);
    }

    public async Task UpdateShipmentAsync(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        _shipmentRepository.Update(shipment);
        await _eventPublisher.EntityUpdatedAsync(shipment);
    }

    public async Task DeleteShipmentItemAsync(ShipmentItem shipmentItem)
    {
        ArgumentNullException.ThrowIfNull(shipmentItem);
        _shipmentItemRepository.Delete(shipmentItem);
        await _eventPublisher.EntityDeletedAsync(shipmentItem);
    }

    public Task<ShipmentItem?> GetShipmentItemByIdAsync(int shipmentItemId)
    {
        return Task.FromResult(shipmentItemId == 0 ? null : _shipmentItemRepository.GetById(shipmentItemId));
    }

    public async Task InsertShipmentItemAsync(ShipmentItem shipmentItem)
    {
        ArgumentNullException.ThrowIfNull(shipmentItem);
        _shipmentItemRepository.Insert(shipmentItem);
        await _eventPublisher.EntityInsertedAsync(shipmentItem);
    }

    public async Task UpdateShipmentItemAsync(ShipmentItem shipmentItem)
    {
        ArgumentNullException.ThrowIfNull(shipmentItem);
        _shipmentItemRepository.Update(shipmentItem);
        await _eventPublisher.EntityUpdatedAsync(shipmentItem);
    }

    public Task<IList<ShipmentItem>> GetShipmentItemsByShipmentIdAsync(int shipmentId)
    {
        var items = _shipmentItemRepository.TableNoTracking
            .Where(si => si.ShipmentId == shipmentId)
            .ToList();
        return Task.FromResult<IList<ShipmentItem>>(items);
    }

    public Task<int> GetQuantityInShipmentsAsync(Product product, int warehouseId, bool ignoreShipped, bool ignoreDelivered)
    {
        ArgumentNullException.ThrowIfNull(product);

        // Only products with "manage stock + multiple warehouses" are tracked this way
        if (product.ManageInventoryMethod != ManageInventoryMethod.ManageStock || !product.UseMultipleWarehouses)
            return Task.FromResult(0);

        const int cancelledOrderStatusId = (int)OrderStatus.Cancelled;

        // Join: ShipmentItem → Shipment → Order (non-deleted, non-cancelled)
        // Filter: OrderItem.ProductId matches, warehouse filter, shipped/delivered filters
        var query = from si in _shipmentItemRepository.TableNoTracking
                    join s in _shipmentRepository.TableNoTracking on si.ShipmentId equals s.Id
                    join o in _orderRepository.TableNoTracking on s.OrderId equals o.Id
                    where !o.Deleted && o.OrderStatusId != cancelledOrderStatusId
                    select new { si, s };

        if (warehouseId > 0)
            query = query.Where(x => x.si.WarehouseId == warehouseId);

        if (ignoreShipped)
            query = query.Where(x => !x.s.ShippedDateUtc.HasValue);

        if (ignoreDelivered)
            query = query.Where(x => !x.s.DeliveryDateUtc.HasValue);

        // Filter to shipment items whose OrderItemId belongs to this product
        var productOrderItemIds = _orderItemRepository.TableNoTracking
            .Where(oi => oi.ProductId == product.Id)
            .Select(oi => oi.Id);

        var result = query
            .Where(x => productOrderItemIds.Contains(x.si.OrderItemId))
            .Sum(x => (int?)x.si.Quantity) ?? 0;

        return Task.FromResult(result);
    }
}
