using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Directory;
using Nop.Services.Events;

namespace Nop.Services.Directory;

public class StateProvinceService : IStateProvinceService
{
    private const string StateProvincesAllKey = "Nop.stateprovince.all-{0}-{1}-{2}";
    private const string StateProvincesPrefix = "Nop.stateprovince.";

    private readonly IRepository<StateProvince> _stateProvinceRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public StateProvinceService(
        IRepository<StateProvince> stateProvinceRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _stateProvinceRepository = stateProvinceRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task DeleteStateProvinceAsync(StateProvince stateProvince)
    {
        ArgumentNullException.ThrowIfNull(stateProvince);
        _stateProvinceRepository.Delete(stateProvince);
        await _cacheManager.RemoveByPrefixAsync(StateProvincesPrefix);
        await _eventPublisher.EntityDeletedAsync(stateProvince);
    }

    public virtual Task<StateProvince?> GetStateProvinceByIdAsync(int stateProvinceId)
    {
        if (stateProvinceId == 0)
            return Task.FromResult<StateProvince?>(null);

        return Task.FromResult(_stateProvinceRepository.GetById(stateProvinceId));
    }

    public virtual Task<StateProvince?> GetStateProvinceByAbbreviationAsync(string abbreviation)
    {
        if (string.IsNullOrEmpty(abbreviation))
            return Task.FromResult<StateProvince?>(null);

        return Task.FromResult<StateProvince?>(_stateProvinceRepository.TableNoTracking
            .FirstOrDefault(sp => sp.Abbreviation == abbreviation));
    }

    public virtual async Task<IList<StateProvince>> GetStateProvincesByCountryIdAsync(int countryId, int languageId = 0, bool showHidden = false)
    {
        var key = new CacheKey(string.Format(StateProvincesAllKey, countryId, languageId, showHidden), StateProvincesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _stateProvinceRepository.TableNoTracking
                .Where(sp => sp.CountryId == countryId && (showHidden || sp.Published))
                .OrderBy(sp => sp.DisplayOrder).ThenBy(sp => sp.Name);

            return Task.FromResult<IList<StateProvince>>(query.ToList());
        }) ?? [];
    }

    public virtual Task<IList<StateProvince>> GetStateProvincesAsync(bool showHidden = false)
    {
        var query = _stateProvinceRepository.TableNoTracking
            .Where(sp => showHidden || sp.Published)
            .OrderBy(sp => sp.CountryId).ThenBy(sp => sp.DisplayOrder).ThenBy(sp => sp.Name);

        return Task.FromResult<IList<StateProvince>>(query.ToList());
    }

    public virtual async Task InsertStateProvinceAsync(StateProvince stateProvince)
    {
        ArgumentNullException.ThrowIfNull(stateProvince);
        _stateProvinceRepository.Insert(stateProvince);
        await _cacheManager.RemoveByPrefixAsync(StateProvincesPrefix);
        await _eventPublisher.EntityInsertedAsync(stateProvince);
    }

    public virtual async Task UpdateStateProvinceAsync(StateProvince stateProvince)
    {
        ArgumentNullException.ThrowIfNull(stateProvince);
        _stateProvinceRepository.Update(stateProvince);
        await _cacheManager.RemoveByPrefixAsync(StateProvincesPrefix);
        await _eventPublisher.EntityUpdatedAsync(stateProvince);
    }
}
