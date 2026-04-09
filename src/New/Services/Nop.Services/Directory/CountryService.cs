using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;
using Nop.Services.Stores;

namespace Nop.Services.Directory;

public class CountryService : ICountryService
{
    private const string CountriesAllKey = "Nop.country.all-{0}-{1}";
    private const string CountriesPrefix = "Nop.country.";

    private readonly IRepository<Country> _countryRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IStoreContext _storeContext;
    private readonly CatalogSettings _catalogSettings;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public CountryService(
        IRepository<Country> countryRepository,
        IRepository<StoreMapping> storeMappingRepository,
        IStoreContext storeContext,
        CatalogSettings catalogSettings,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _countryRepository = countryRepository;
        _storeMappingRepository = storeMappingRepository;
        _storeContext = storeContext;
        _catalogSettings = catalogSettings;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task DeleteCountryAsync(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);
        _countryRepository.Delete(country);
        await _cacheManager.RemoveByPrefixAsync(CountriesPrefix);
        await _eventPublisher.EntityDeletedAsync(country);
    }

    public virtual async Task<IList<Country>> GetAllCountriesAsync(int languageId = 0, bool showHidden = false)
    {
        var key = new CacheKey(string.Format(CountriesAllKey, languageId, showHidden), CountriesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _countryRepository.TableNoTracking;
            if (!showHidden)
                query = query.Where(c => c.Published);

            query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);

            if (!showHidden && !_catalogSettings.IgnoreStoreLimitations)
            {
                var currentStoreId = _storeContext.CurrentStore.Id;
                query = from c in query
                        join sc in _storeMappingRepository.TableNoTracking
                            on new { c1 = c.Id, c2 = "Country" } equals new { c1 = sc.EntityId, c2 = sc.EntityName } into c_sc
                        from sc in c_sc.DefaultIfEmpty()
                        where !c.LimitedToStores || currentStoreId == sc.StoreId
                        select c;

                query = from c in query
                        group c by c.Id into cGroup
                        orderby cGroup.Key
                        select cGroup.First();

                query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);
            }

            return Task.FromResult<IList<Country>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task<IList<Country>> GetAllCountriesForBillingAsync(int languageId = 0, bool showHidden = false)
    {
        var countries = await GetAllCountriesAsync(languageId, showHidden);
        return countries.Where(c => c.AllowsBilling).ToList();
    }

    public virtual async Task<IList<Country>> GetAllCountriesForShippingAsync(int languageId = 0, bool showHidden = false)
    {
        var countries = await GetAllCountriesAsync(languageId, showHidden);
        return countries.Where(c => c.AllowsShipping).ToList();
    }

    public virtual Task<Country?> GetCountryByIdAsync(int countryId)
    {
        if (countryId == 0)
            return Task.FromResult<Country?>(null);

        return Task.FromResult(_countryRepository.GetById(countryId));
    }

    public virtual Task<IList<Country>> GetCountriesByIdsAsync(int[] countryIds)
    {
        if (countryIds is not { Length: > 0 })
            return Task.FromResult<IList<Country>>([]);

        var countries = _countryRepository.TableNoTracking
            .Where(c => countryIds.Contains(c.Id))
            .ToList();

        // sort by passed identifiers
        var sorted = new List<Country>();
        foreach (var id in countryIds)
        {
            var country = countries.Find(x => x.Id == id);
            if (country is not null)
                sorted.Add(country);
        }

        return Task.FromResult<IList<Country>>(sorted);
    }

    public virtual Task<Country?> GetCountryByTwoLetterIsoCodeAsync(string twoLetterIsoCode)
    {
        if (string.IsNullOrEmpty(twoLetterIsoCode))
            return Task.FromResult<Country?>(null);

        return Task.FromResult<Country?>(_countryRepository.TableNoTracking
            .FirstOrDefault(c => c.TwoLetterIsoCode == twoLetterIsoCode));
    }

    public virtual Task<Country?> GetCountryByThreeLetterIsoCodeAsync(string threeLetterIsoCode)
    {
        if (string.IsNullOrEmpty(threeLetterIsoCode))
            return Task.FromResult<Country?>(null);

        return Task.FromResult<Country?>(_countryRepository.TableNoTracking
            .FirstOrDefault(c => c.ThreeLetterIsoCode == threeLetterIsoCode));
    }

    public virtual async Task InsertCountryAsync(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);
        _countryRepository.Insert(country);
        await _cacheManager.RemoveByPrefixAsync(CountriesPrefix);
        await _eventPublisher.EntityInsertedAsync(country);
    }

    public virtual async Task UpdateCountryAsync(Country country)
    {
        ArgumentNullException.ThrowIfNull(country);
        _countryRepository.Update(country);
        await _cacheManager.RemoveByPrefixAsync(CountriesPrefix);
        await _eventPublisher.EntityUpdatedAsync(country);
    }
}
