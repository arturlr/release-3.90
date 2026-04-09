using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class ManufacturerService : IManufacturerService
{
    private const string ManufacturersByIdKey = "Nop.manufacturer.id-{0}";
    private const string ManufacturersAllKey = "Nop.manufacturer.all-{0}-{1}";
    private const string ProductManufacturersByProductKey = "Nop.productmanufacturer.byproduct-{0}-{1}";
    private const string ManufacturersPrefix = "Nop.manufacturer.";
    private const string ProductManufacturersPrefix = "Nop.productmanufacturer.";

    private readonly IRepository<Manufacturer> _manufacturerRepository;
    private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<AclRecord> _aclRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IWorkContext _workContext;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly CatalogSettings _catalogSettings;

    public ManufacturerService(
        IRepository<Manufacturer> manufacturerRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<Product> productRepository,
        IRepository<AclRecord> aclRepository,
        IRepository<StoreMapping> storeMappingRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IWorkContext workContext,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        CatalogSettings catalogSettings)
    {
        _manufacturerRepository = manufacturerRepository;
        _productManufacturerRepository = productManufacturerRepository;
        _productRepository = productRepository;
        _aclRepository = aclRepository;
        _storeMappingRepository = storeMappingRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _workContext = workContext;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _catalogSettings = catalogSettings;
    }

    public virtual async Task DeleteManufacturerAsync(Manufacturer manufacturer)
    {
        ArgumentNullException.ThrowIfNull(manufacturer);
        manufacturer.Deleted = true;
        await UpdateManufacturerAsync(manufacturer);
    }

    public virtual async Task<IPagedList<Manufacturer>> GetAllManufacturersAsync(string manufacturerName = "", int storeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var key = new CacheKey(string.Format(ManufacturersAllKey, storeId, showHidden), ManufacturersPrefix);
        var allManufacturers = await _cacheManager.GetAsync(key, () =>
        {
            var query = _manufacturerRepository.TableNoTracking.Where(m => !m.Deleted);

            if (!showHidden)
                query = query.Where(m => m.Published);

            if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
            {
                query = from m in query
                        join sm in _storeMappingRepository.TableNoTracking
                            on new { c1 = m.Id, c2 = "Manufacturer" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into m_sm
                        from sm in m_sm.DefaultIfEmpty()
                        where !m.LimitedToStores || storeId == sm.StoreId
                        select m;
            }

            if (!showHidden && !_catalogSettings.IgnoreAcl)
            {
                var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
                    .Where(crm => crm.CustomerId == _workContext.CurrentCustomer.Id)
                    .Select(crm => crm.CustomerRoleId).ToList();

                query = from m in query
                        join acl in _aclRepository.TableNoTracking
                            on new { c1 = m.Id, c2 = "Manufacturer" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into m_acl
                        from acl in m_acl.DefaultIfEmpty()
                        where !m.SubjectToAcl || customerRoleIds.Contains(acl.CustomerRoleId)
                        select m;
            }

            query = from m in query
                    group m by m.Id into mGroup
                    orderby mGroup.Key
                    select mGroup.First();

            query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id);
            return Task.FromResult<IList<Manufacturer>>(query.ToList());
        }) ?? [];

        // name filter applied post-cache (matching legacy pattern)
        if (!string.IsNullOrWhiteSpace(manufacturerName))
            allManufacturers = allManufacturers.Where(m => m.Name != null && m.Name.Contains(manufacturerName, StringComparison.OrdinalIgnoreCase)).ToList();

        return new PagedList<Manufacturer>(allManufacturers, pageIndex, pageSize);
    }

    public virtual async Task<Manufacturer?> GetManufacturerByIdAsync(int manufacturerId)
    {
        if (manufacturerId == 0) return null;
        var key = new CacheKey(string.Format(ManufacturersByIdKey, manufacturerId), ManufacturersPrefix);
        return await _cacheManager.GetAsync(key, () => Task.FromResult(_manufacturerRepository.GetById(manufacturerId)));
    }

    public virtual async Task InsertManufacturerAsync(Manufacturer manufacturer)
    {
        ArgumentNullException.ThrowIfNull(manufacturer);
        _manufacturerRepository.Insert(manufacturer);
        await _cacheManager.RemoveByPrefixAsync(ManufacturersPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductManufacturersPrefix);
        await _eventPublisher.EntityInsertedAsync(manufacturer);
    }

    public virtual async Task UpdateManufacturerAsync(Manufacturer manufacturer)
    {
        ArgumentNullException.ThrowIfNull(manufacturer);
        _manufacturerRepository.Update(manufacturer);
        await _cacheManager.RemoveByPrefixAsync(ManufacturersPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductManufacturersPrefix);
        await _eventPublisher.EntityUpdatedAsync(manufacturer);
    }

    public virtual async Task DeleteProductManufacturerAsync(ProductManufacturer productManufacturer)
    {
        ArgumentNullException.ThrowIfNull(productManufacturer);
        _productManufacturerRepository.Delete(productManufacturer);
        await _cacheManager.RemoveByPrefixAsync(ManufacturersPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductManufacturersPrefix);
        await _eventPublisher.EntityDeletedAsync(productManufacturer);
    }

    public virtual Task<IPagedList<ProductManufacturer>> GetProductManufacturersByManufacturerIdAsync(int manufacturerId,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        if (manufacturerId == 0)
            return Task.FromResult<IPagedList<ProductManufacturer>>(new PagedList<ProductManufacturer>([], pageIndex, pageSize));

        var query = from pm in _productManufacturerRepository.TableNoTracking
                    join p in _productRepository.TableNoTracking on pm.ProductId equals p.Id
                    where pm.ManufacturerId == manufacturerId && !p.Deleted && (showHidden || p.Published)
                    orderby pm.DisplayOrder, pm.Id
                    select pm;

        return Task.FromResult<IPagedList<ProductManufacturer>>(new PagedList<ProductManufacturer>(query.ToList(), pageIndex, pageSize));
    }

    public virtual async Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdAsync(int productId, bool showHidden = false)
    {
        if (productId == 0) return [];

        var key = new CacheKey(string.Format(ProductManufacturersByProductKey, productId, showHidden), ProductManufacturersPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = from pm in _productManufacturerRepository.TableNoTracking
                        join m in _manufacturerRepository.TableNoTracking on pm.ManufacturerId equals m.Id
                        where pm.ProductId == productId && !m.Deleted && (showHidden || m.Published)
                        orderby pm.DisplayOrder, pm.Id
                        select pm;

            return Task.FromResult<IList<ProductManufacturer>>(query.ToList());
        }) ?? [];
    }

    public virtual Task<ProductManufacturer?> GetProductManufacturerByIdAsync(int productManufacturerId) =>
        Task.FromResult(productManufacturerId == 0 ? null : _productManufacturerRepository.GetById(productManufacturerId));

    public virtual async Task InsertProductManufacturerAsync(ProductManufacturer productManufacturer)
    {
        ArgumentNullException.ThrowIfNull(productManufacturer);
        _productManufacturerRepository.Insert(productManufacturer);
        await _cacheManager.RemoveByPrefixAsync(ManufacturersPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductManufacturersPrefix);
        await _eventPublisher.EntityInsertedAsync(productManufacturer);
    }

    public virtual async Task UpdateProductManufacturerAsync(ProductManufacturer productManufacturer)
    {
        ArgumentNullException.ThrowIfNull(productManufacturer);
        _productManufacturerRepository.Update(productManufacturer);
        await _cacheManager.RemoveByPrefixAsync(ManufacturersPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductManufacturersPrefix);
        await _eventPublisher.EntityUpdatedAsync(productManufacturer);
    }

    public virtual Task<IDictionary<int, int[]>> GetProductManufacturerIdsAsync(int[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        var query = _productManufacturerRepository.TableNoTracking
            .Where(pm => productIds.Contains(pm.ProductId))
            .Select(pm => new { pm.ProductId, pm.ManufacturerId }).ToList();

        IDictionary<int, int[]> result = query
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ManufacturerId).ToArray());

        return Task.FromResult(result);
    }

    public virtual Task<string[]> GetNotExistingManufacturersAsync(string[] manufacturerNames)
    {
        ArgumentNullException.ThrowIfNull(manufacturerNames);
        var existingNames = _manufacturerRepository.TableNoTracking
            .Where(m => manufacturerNames.Contains(m.Name))
            .Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Task.FromResult(manufacturerNames.Where(n => !existingNames.Contains(n)).ToArray());
    }
}
