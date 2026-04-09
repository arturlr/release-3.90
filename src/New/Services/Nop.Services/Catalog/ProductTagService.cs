using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class ProductTagService : IProductTagService
{
    private const string ProductTagCountKey = "Nop.producttag.count-{0}";
    private const string ProductTagPrefix = "Nop.producttag.";

    private readonly IRepository<ProductTag> _productTagRepository;
    private readonly IRepository<ProductProductTagMapping> _productProductTagMappingRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly CatalogSettings _catalogSettings;

    public ProductTagService(
        IRepository<ProductTag> productTagRepository,
        IRepository<ProductProductTagMapping> productProductTagMappingRepository,
        IRepository<Product> productRepository,
        IRepository<StoreMapping> storeMappingRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        CatalogSettings catalogSettings)
    {
        _productTagRepository = productTagRepository;
        _productProductTagMappingRepository = productProductTagMappingRepository;
        _productRepository = productRepository;
        _storeMappingRepository = storeMappingRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _catalogSettings = catalogSettings;
    }

    public virtual async Task DeleteProductTagAsync(ProductTag productTag)
    {
        ArgumentNullException.ThrowIfNull(productTag);
        _productTagRepository.Delete(productTag);
        await _cacheManager.RemoveByPrefixAsync(ProductTagPrefix);
        await _eventPublisher.EntityDeletedAsync(productTag);
    }

    public virtual Task<IList<ProductTag>> GetAllProductTagsAsync()
    {
        IList<ProductTag> tags = _productTagRepository.TableNoTracking.ToList();
        return Task.FromResult(tags);
    }

    public virtual Task<ProductTag?> GetProductTagByIdAsync(int productTagId) =>
        Task.FromResult(productTagId == 0 ? null : _productTagRepository.GetById(productTagId));

    public virtual Task<ProductTag?> GetProductTagByNameAsync(string name)
    {
        var tag = _productTagRepository.TableNoTracking.FirstOrDefault(pt => pt.Name == name);
        return Task.FromResult(tag);
    }

    public virtual async Task InsertProductTagAsync(ProductTag productTag)
    {
        ArgumentNullException.ThrowIfNull(productTag);
        _productTagRepository.Insert(productTag);
        await _cacheManager.RemoveByPrefixAsync(ProductTagPrefix);
        await _eventPublisher.EntityInsertedAsync(productTag);
    }

    public virtual async Task UpdateProductTagAsync(ProductTag productTag)
    {
        ArgumentNullException.ThrowIfNull(productTag);
        _productTagRepository.Update(productTag);
        await _cacheManager.RemoveByPrefixAsync(ProductTagPrefix);
        await _eventPublisher.EntityUpdatedAsync(productTag);
    }

    public virtual async Task<int> GetProductCountAsync(int productTagId, int storeId)
    {
        var key = new CacheKey(string.Format(ProductTagCountKey, storeId), ProductTagPrefix);
        var dictionary = await _cacheManager.GetAsync(key, () =>
        {
            // LINQ-based product count per tag (no stored procedure)
            var query = from ptm in _productProductTagMappingRepository.TableNoTracking
                        join p in _productRepository.TableNoTracking on ptm.ProductId equals p.Id
                        where !p.Deleted && p.Published
                        select new { ptm.ProductTagId, ptm.ProductId, p.LimitedToStores };

            if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
            {
                query = from q in query
                        join sm in _storeMappingRepository.TableNoTracking
                            on new { c1 = q.ProductId, c2 = "Product" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into q_sm
                        from sm in q_sm.DefaultIfEmpty()
                        where !q.LimitedToStores || storeId == sm.StoreId
                        select q;
            }

            var dict = query.GroupBy(x => x.ProductTagId)
                .ToDictionary(g => g.Key, g => g.Count());

            return Task.FromResult(dict);
        });

        return dictionary != null && dictionary.TryGetValue(productTagId, out var count) ? count : 0;
    }

    public virtual async Task UpdateProductTagsAsync(Product product, string[] productTags)
    {
        ArgumentNullException.ThrowIfNull(product);
        productTags ??= [];

        // get existing tag mappings for this product
        var existingMappings = _productProductTagMappingRepository.TableNoTracking
            .Where(m => m.ProductId == product.Id).ToList();

        var existingTagIds = existingMappings.Select(m => m.ProductTagId).ToList();
        var existingTags = _productTagRepository.TableNoTracking
            .Where(t => existingTagIds.Contains(t.Id)).ToList();

        // remove tags not in new list
        foreach (var existingTag in existingTags)
        {
            if (!productTags.Any(t => t.Equals(existingTag.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var mapping = existingMappings.First(m => m.ProductTagId == existingTag.Id);
                _productProductTagMappingRepository.Delete(mapping);
            }
        }

        // add new tags
        foreach (var tagName in productTags)
        {
            var tag = await GetProductTagByNameAsync(tagName);
            if (tag == null)
            {
                tag = new ProductTag { Name = tagName };
                await InsertProductTagAsync(tag);
            }

            if (!existingTags.Any(t => t.Id == tag.Id))
            {
                _productProductTagMappingRepository.Insert(new ProductProductTagMapping
                {
                    ProductId = product.Id,
                    ProductTagId = tag.Id
                });
            }
        }

        await _cacheManager.RemoveByPrefixAsync(ProductTagPrefix);
    }
}
