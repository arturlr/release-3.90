using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Tax;
using Nop.Services.Events;

namespace Nop.Services.Tax;

public class TaxCategoryService : ITaxCategoryService
{
    private const string AllKey = "Nop.taxcategory.all";
    private const string ByIdKey = "Nop.taxcategory.id-{0}";
    private const string Prefix = "Nop.taxcategory.";

    private readonly IRepository<TaxCategory> _taxCategoryRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public TaxCategoryService(
        IRepository<TaxCategory> taxCategoryRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _taxCategoryRepository = taxCategoryRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task<IList<TaxCategory>> GetAllTaxCategoriesAsync()
    {
        var key = new CacheKey(AllKey, Prefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            IList<TaxCategory> result = _taxCategoryRepository.TableNoTracking
                .OrderBy(tc => tc.DisplayOrder).ThenBy(tc => tc.Id)
                .ToList();
            return Task.FromResult(result);
        }) ?? [];
    }

    public virtual async Task<TaxCategory?> GetTaxCategoryByIdAsync(int taxCategoryId)
    {
        if (taxCategoryId == 0)
            return null;

        var key = new CacheKey(string.Format(ByIdKey, taxCategoryId), Prefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_taxCategoryRepository.GetById(taxCategoryId)));
    }

    public virtual async Task InsertTaxCategoryAsync(TaxCategory taxCategory)
    {
        ArgumentNullException.ThrowIfNull(taxCategory);
        _taxCategoryRepository.Insert(taxCategory);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
        await _eventPublisher.EntityInsertedAsync(taxCategory);
    }

    public virtual async Task UpdateTaxCategoryAsync(TaxCategory taxCategory)
    {
        ArgumentNullException.ThrowIfNull(taxCategory);
        _taxCategoryRepository.Update(taxCategory);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
        await _eventPublisher.EntityUpdatedAsync(taxCategory);
    }

    public virtual async Task DeleteTaxCategoryAsync(TaxCategory taxCategory)
    {
        ArgumentNullException.ThrowIfNull(taxCategory);
        _taxCategoryRepository.Delete(taxCategory);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
        await _eventPublisher.EntityDeletedAsync(taxCategory);
    }
}
