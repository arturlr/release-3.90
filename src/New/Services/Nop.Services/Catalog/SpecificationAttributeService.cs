using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class SpecificationAttributeService : ISpecificationAttributeService
{
    private const string SpecAttrPrefix = "Nop.specattr.";

    private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
    private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionRepository;
    private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public SpecificationAttributeService(
        IRepository<SpecificationAttribute> specificationAttributeRepository,
        IRepository<SpecificationAttributeOption> specificationAttributeOptionRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _specificationAttributeRepository = specificationAttributeRepository;
        _specificationAttributeOptionRepository = specificationAttributeOptionRepository;
        _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    // Specification attributes

    public virtual Task<SpecificationAttribute?> GetSpecificationAttributeByIdAsync(int specificationAttributeId) =>
        Task.FromResult(specificationAttributeId == 0 ? null : _specificationAttributeRepository.GetById(specificationAttributeId));

    public virtual Task<IPagedList<SpecificationAttribute>> GetSpecificationAttributesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _specificationAttributeRepository.TableNoTracking
            .OrderBy(sa => sa.DisplayOrder).ThenBy(sa => sa.Id);
        return Task.FromResult<IPagedList<SpecificationAttribute>>(new PagedList<SpecificationAttribute>(query.ToList(), pageIndex, pageSize));
    }

    public virtual async Task DeleteSpecificationAttributeAsync(SpecificationAttribute specificationAttribute)
    {
        ArgumentNullException.ThrowIfNull(specificationAttribute);
        _specificationAttributeRepository.Delete(specificationAttribute);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(specificationAttribute);
    }

    public virtual async Task InsertSpecificationAttributeAsync(SpecificationAttribute specificationAttribute)
    {
        ArgumentNullException.ThrowIfNull(specificationAttribute);
        _specificationAttributeRepository.Insert(specificationAttribute);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(specificationAttribute);
    }

    public virtual async Task UpdateSpecificationAttributeAsync(SpecificationAttribute specificationAttribute)
    {
        ArgumentNullException.ThrowIfNull(specificationAttribute);
        _specificationAttributeRepository.Update(specificationAttribute);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(specificationAttribute);
    }

    // Specification attribute options

    public virtual Task<SpecificationAttributeOption?> GetSpecificationAttributeOptionByIdAsync(int specificationAttributeOptionId) =>
        Task.FromResult(specificationAttributeOptionId == 0 ? null : _specificationAttributeOptionRepository.GetById(specificationAttributeOptionId));

    public virtual Task<IList<SpecificationAttributeOption>> GetSpecificationAttributeOptionsByIdsAsync(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        IList<SpecificationAttributeOption> options = _specificationAttributeOptionRepository.TableNoTracking
            .Where(sao => ids.Contains(sao.Id)).ToList();
        return Task.FromResult(options);
    }

    public virtual Task<IList<SpecificationAttributeOption>> GetSpecificationAttributeOptionsBySpecificationAttributeAsync(int specificationAttributeId)
    {
        IList<SpecificationAttributeOption> options = _specificationAttributeOptionRepository.TableNoTracking
            .Where(sao => sao.SpecificationAttributeId == specificationAttributeId)
            .OrderBy(sao => sao.DisplayOrder).ThenBy(sao => sao.Id).ToList();
        return Task.FromResult(options);
    }

    public virtual async Task DeleteSpecificationAttributeOptionAsync(SpecificationAttributeOption specificationAttributeOption)
    {
        ArgumentNullException.ThrowIfNull(specificationAttributeOption);
        _specificationAttributeOptionRepository.Delete(specificationAttributeOption);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(specificationAttributeOption);
    }

    public virtual async Task InsertSpecificationAttributeOptionAsync(SpecificationAttributeOption specificationAttributeOption)
    {
        ArgumentNullException.ThrowIfNull(specificationAttributeOption);
        _specificationAttributeOptionRepository.Insert(specificationAttributeOption);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(specificationAttributeOption);
    }

    public virtual async Task UpdateSpecificationAttributeOptionAsync(SpecificationAttributeOption specificationAttributeOption)
    {
        ArgumentNullException.ThrowIfNull(specificationAttributeOption);
        _specificationAttributeOptionRepository.Update(specificationAttributeOption);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(specificationAttributeOption);
    }

    // Product specification attributes

    public virtual Task<IList<ProductSpecificationAttribute>> GetProductSpecificationAttributesAsync(int productId = 0,
        int specificationAttributeOptionId = 0, bool? allowFiltering = null, bool? showOnProductPage = null)
    {
        var query = _productSpecificationAttributeRepository.TableNoTracking.AsQueryable();

        if (productId > 0)
            query = query.Where(psa => psa.ProductId == productId);
        if (specificationAttributeOptionId > 0)
            query = query.Where(psa => psa.SpecificationAttributeOptionId == specificationAttributeOptionId);
        if (allowFiltering.HasValue)
            query = query.Where(psa => psa.AllowFiltering == allowFiltering.Value);
        if (showOnProductPage.HasValue)
            query = query.Where(psa => psa.ShowOnProductPage == showOnProductPage.Value);

        query = query.OrderBy(psa => psa.DisplayOrder).ThenBy(psa => psa.Id);
        IList<ProductSpecificationAttribute> result = query.ToList();
        return Task.FromResult(result);
    }

    public virtual Task<ProductSpecificationAttribute?> GetProductSpecificationAttributeByIdAsync(int productSpecificationAttributeId) =>
        Task.FromResult(productSpecificationAttributeId == 0 ? null : _productSpecificationAttributeRepository.GetById(productSpecificationAttributeId));

    public virtual async Task DeleteProductSpecificationAttributeAsync(ProductSpecificationAttribute productSpecificationAttribute)
    {
        ArgumentNullException.ThrowIfNull(productSpecificationAttribute);
        _productSpecificationAttributeRepository.Delete(productSpecificationAttribute);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityDeletedAsync(productSpecificationAttribute);
    }

    public virtual async Task InsertProductSpecificationAttributeAsync(ProductSpecificationAttribute productSpecificationAttribute)
    {
        ArgumentNullException.ThrowIfNull(productSpecificationAttribute);
        _productSpecificationAttributeRepository.Insert(productSpecificationAttribute);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityInsertedAsync(productSpecificationAttribute);
    }

    public virtual async Task UpdateProductSpecificationAttributeAsync(ProductSpecificationAttribute productSpecificationAttribute)
    {
        ArgumentNullException.ThrowIfNull(productSpecificationAttribute);
        _productSpecificationAttributeRepository.Update(productSpecificationAttribute);
        await _cacheManager.RemoveByPrefixAsync(SpecAttrPrefix);
        await _eventPublisher.EntityUpdatedAsync(productSpecificationAttribute);
    }

    public virtual Task<int> GetProductSpecificationAttributeCountAsync(int productId = 0, int specificationAttributeOptionId = 0)
    {
        var query = _productSpecificationAttributeRepository.TableNoTracking.AsQueryable();
        if (productId > 0)
            query = query.Where(psa => psa.ProductId == productId);
        if (specificationAttributeOptionId > 0)
            query = query.Where(psa => psa.SpecificationAttributeOptionId == specificationAttributeOptionId);
        return Task.FromResult(query.Count());
    }
}
