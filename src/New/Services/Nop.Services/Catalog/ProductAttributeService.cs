using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class ProductAttributeService : IProductAttributeService
{
    private const string ProductAttrPrefix = "Nop.productattr.";
    private const string ProductAttrMappingByProductKey = "Nop.productattrmapping.byproduct-{0}";
    private const string ProductAttrValuesByMappingKey = "Nop.productattrvalue.bymapping-{0}";
    private const string ProductAttrCombinationsByProductKey = "Nop.productattrcombination.byproduct-{0}";

    private readonly IRepository<ProductAttribute> _productAttributeRepository;
    private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
    private readonly IRepository<ProductAttributeValue> _productAttributeValueRepository;
    private readonly IRepository<PredefinedProductAttributeValue> _predefinedProductAttributeValueRepository;
    private readonly IRepository<ProductAttributeCombination> _productAttributeCombinationRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public ProductAttributeService(
        IRepository<ProductAttribute> productAttributeRepository,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        IRepository<ProductAttributeValue> productAttributeValueRepository,
        IRepository<PredefinedProductAttributeValue> predefinedProductAttributeValueRepository,
        IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _productAttributeRepository = productAttributeRepository;
        _productAttributeMappingRepository = productAttributeMappingRepository;
        _productAttributeValueRepository = productAttributeValueRepository;
        _predefinedProductAttributeValueRepository = predefinedProductAttributeValueRepository;
        _productAttributeCombinationRepository = productAttributeCombinationRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    // Product attributes

    public virtual async Task DeleteProductAttributeAsync(ProductAttribute productAttribute)
    {
        ArgumentNullException.ThrowIfNull(productAttribute);
        _productAttributeRepository.Delete(productAttribute);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(productAttribute);
    }

    public virtual Task<IPagedList<ProductAttribute>> GetAllProductAttributesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _productAttributeRepository.TableNoTracking.OrderBy(pa => pa.Id);
        return Task.FromResult<IPagedList<ProductAttribute>>(new PagedList<ProductAttribute>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<ProductAttribute?> GetProductAttributeByIdAsync(int productAttributeId) =>
        Task.FromResult(productAttributeId == 0 ? null : _productAttributeRepository.GetById(productAttributeId));

    public virtual async Task InsertProductAttributeAsync(ProductAttribute productAttribute)
    {
        ArgumentNullException.ThrowIfNull(productAttribute);
        _productAttributeRepository.Insert(productAttribute);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(productAttribute);
    }

    public virtual async Task UpdateProductAttributeAsync(ProductAttribute productAttribute)
    {
        ArgumentNullException.ThrowIfNull(productAttribute);
        _productAttributeRepository.Update(productAttribute);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(productAttribute);
    }

    public virtual Task<int[]> GetNotExistingAttributesAsync(int[] attributeIds)
    {
        ArgumentNullException.ThrowIfNull(attributeIds);
        var existingIds = _productAttributeRepository.TableNoTracking
            .Where(pa => attributeIds.Contains(pa.Id)).Select(pa => pa.Id).ToHashSet();
        return Task.FromResult(attributeIds.Where(id => !existingIds.Contains(id)).ToArray());
    }

    // Product attribute mappings

    public virtual async Task DeleteProductAttributeMappingAsync(ProductAttributeMapping productAttributeMapping)
    {
        ArgumentNullException.ThrowIfNull(productAttributeMapping);
        _productAttributeMappingRepository.Delete(productAttributeMapping);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(productAttributeMapping);
    }

    public virtual async Task<IList<ProductAttributeMapping>> GetProductAttributeMappingsByProductIdAsync(int productId)
    {
        var key = new CacheKey(string.Format(ProductAttrMappingByProductKey, productId), ProductAttrPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            IList<ProductAttributeMapping> mappings = _productAttributeMappingRepository.TableNoTracking
                .Where(pam => pam.ProductId == productId)
                .OrderBy(pam => pam.DisplayOrder).ThenBy(pam => pam.Id).ToList();
            return Task.FromResult(mappings);
        }) ?? [];
    }

    public virtual Task<ProductAttributeMapping?> GetProductAttributeMappingByIdAsync(int productAttributeMappingId) =>
        Task.FromResult(productAttributeMappingId == 0 ? null : _productAttributeMappingRepository.GetById(productAttributeMappingId));

    public virtual async Task InsertProductAttributeMappingAsync(ProductAttributeMapping productAttributeMapping)
    {
        ArgumentNullException.ThrowIfNull(productAttributeMapping);
        _productAttributeMappingRepository.Insert(productAttributeMapping);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(productAttributeMapping);
    }

    public virtual async Task UpdateProductAttributeMappingAsync(ProductAttributeMapping productAttributeMapping)
    {
        ArgumentNullException.ThrowIfNull(productAttributeMapping);
        _productAttributeMappingRepository.Update(productAttributeMapping);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(productAttributeMapping);
    }

    // Product attribute values

    public virtual async Task DeleteProductAttributeValueAsync(ProductAttributeValue productAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(productAttributeValue);
        _productAttributeValueRepository.Delete(productAttributeValue);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(productAttributeValue);
    }

    public virtual async Task<IList<ProductAttributeValue>> GetProductAttributeValuesAsync(int productAttributeMappingId)
    {
        var key = new CacheKey(string.Format(ProductAttrValuesByMappingKey, productAttributeMappingId), ProductAttrPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            IList<ProductAttributeValue> values = _productAttributeValueRepository.TableNoTracking
                .Where(pav => pav.ProductAttributeMappingId == productAttributeMappingId)
                .OrderBy(pav => pav.DisplayOrder).ThenBy(pav => pav.Id).ToList();
            return Task.FromResult(values);
        }) ?? [];
    }

    public virtual Task<ProductAttributeValue?> GetProductAttributeValueByIdAsync(int productAttributeValueId) =>
        Task.FromResult(productAttributeValueId == 0 ? null : _productAttributeValueRepository.GetById(productAttributeValueId));

    public virtual async Task InsertProductAttributeValueAsync(ProductAttributeValue productAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(productAttributeValue);
        _productAttributeValueRepository.Insert(productAttributeValue);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(productAttributeValue);
    }

    public virtual async Task UpdateProductAttributeValueAsync(ProductAttributeValue productAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(productAttributeValue);
        _productAttributeValueRepository.Update(productAttributeValue);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(productAttributeValue);
    }

    // Predefined product attribute values

    public virtual async Task DeletePredefinedProductAttributeValueAsync(PredefinedProductAttributeValue ppav)
    {
        ArgumentNullException.ThrowIfNull(ppav);
        _predefinedProductAttributeValueRepository.Delete(ppav);
        await _eventPublisher.EntityDeletedAsync(ppav);
    }

    public virtual Task<IList<PredefinedProductAttributeValue>> GetPredefinedProductAttributeValuesAsync(int productAttributeId)
    {
        IList<PredefinedProductAttributeValue> values = _predefinedProductAttributeValueRepository.TableNoTracking
            .Where(ppav => ppav.ProductAttributeId == productAttributeId)
            .OrderBy(ppav => ppav.DisplayOrder).ThenBy(ppav => ppav.Id).ToList();
        return Task.FromResult(values);
    }

    public virtual Task<PredefinedProductAttributeValue?> GetPredefinedProductAttributeValueByIdAsync(int id) =>
        Task.FromResult(id == 0 ? null : _predefinedProductAttributeValueRepository.GetById(id));

    public virtual async Task InsertPredefinedProductAttributeValueAsync(PredefinedProductAttributeValue ppav)
    {
        ArgumentNullException.ThrowIfNull(ppav);
        _predefinedProductAttributeValueRepository.Insert(ppav);
        await _eventPublisher.EntityInsertedAsync(ppav);
    }

    public virtual async Task UpdatePredefinedProductAttributeValueAsync(PredefinedProductAttributeValue ppav)
    {
        ArgumentNullException.ThrowIfNull(ppav);
        _predefinedProductAttributeValueRepository.Update(ppav);
        await _eventPublisher.EntityUpdatedAsync(ppav);
    }

    // Product attribute combinations

    public virtual async Task DeleteProductAttributeCombinationAsync(ProductAttributeCombination combination)
    {
        ArgumentNullException.ThrowIfNull(combination);
        _productAttributeCombinationRepository.Delete(combination);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(combination);
    }

    public virtual async Task<IList<ProductAttributeCombination>> GetAllProductAttributeCombinationsAsync(int productId)
    {
        if (productId == 0) return [];
        var key = new CacheKey(string.Format(ProductAttrCombinationsByProductKey, productId), ProductAttrPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            IList<ProductAttributeCombination> combinations = _productAttributeCombinationRepository.TableNoTracking
                .Where(pac => pac.ProductId == productId).ToList();
            return Task.FromResult(combinations);
        }) ?? [];
    }

    public virtual Task<ProductAttributeCombination?> GetProductAttributeCombinationByIdAsync(int productAttributeCombinationId) =>
        Task.FromResult(productAttributeCombinationId == 0 ? null : _productAttributeCombinationRepository.GetById(productAttributeCombinationId));

    public virtual Task<ProductAttributeCombination?> GetProductAttributeCombinationBySkuAsync(string sku)
    {
        if (string.IsNullOrEmpty(sku)) return Task.FromResult<ProductAttributeCombination?>(null);
        var combination = _productAttributeCombinationRepository.TableNoTracking
            .FirstOrDefault(pac => pac.Sku == sku);
        return Task.FromResult(combination);
    }

    public virtual async Task InsertProductAttributeCombinationAsync(ProductAttributeCombination combination)
    {
        ArgumentNullException.ThrowIfNull(combination);
        _productAttributeCombinationRepository.Insert(combination);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(combination);
    }

    public virtual async Task UpdateProductAttributeCombinationAsync(ProductAttributeCombination combination)
    {
        ArgumentNullException.ThrowIfNull(combination);
        _productAttributeCombinationRepository.Update(combination);
        await _cacheManager.RemoveByPrefixAsync(ProductAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(combination);
    }
}
