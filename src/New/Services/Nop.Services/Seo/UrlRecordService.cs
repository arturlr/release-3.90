using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;

namespace Nop.Services.Seo;

public class UrlRecordService : IUrlRecordService
{
    private const string ActiveSlugKey = "Nop.urlrecord.active.id-name-language-{0}-{1}-{2}";
    private const string AllKey = "Nop.urlrecord.all";
    private const string BySlugKey = "Nop.urlrecord.active.slug-{0}";
    private const string Prefix = "Nop.urlrecord.";

    private readonly IRepository<UrlRecord> _urlRecordRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly LocalizationSettings _localizationSettings;

    public UrlRecordService(
        IRepository<UrlRecord> urlRecordRepository,
        IStaticCacheManager cacheManager,
        LocalizationSettings localizationSettings)
    {
        _urlRecordRepository = urlRecordRepository;
        _cacheManager = cacheManager;
        _localizationSettings = localizationSettings;
    }

    public virtual async Task DeleteUrlRecordAsync(UrlRecord urlRecord)
    {
        ArgumentNullException.ThrowIfNull(urlRecord);
        _urlRecordRepository.Delete(urlRecord);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
    }

    public virtual async Task DeleteUrlRecordsAsync(IList<UrlRecord> urlRecords)
    {
        ArgumentNullException.ThrowIfNull(urlRecords);
        _urlRecordRepository.Delete(urlRecords);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
    }

    public virtual Task<UrlRecord?> GetUrlRecordByIdAsync(int urlRecordId)
    {
        if (urlRecordId == 0)
            return Task.FromResult<UrlRecord?>(null);
        return Task.FromResult(_urlRecordRepository.GetById(urlRecordId));
    }

    public virtual async Task InsertUrlRecordAsync(UrlRecord urlRecord)
    {
        ArgumentNullException.ThrowIfNull(urlRecord);
        _urlRecordRepository.Insert(urlRecord);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
    }

    public virtual async Task UpdateUrlRecordAsync(UrlRecord urlRecord)
    {
        ArgumentNullException.ThrowIfNull(urlRecord);
        _urlRecordRepository.Update(urlRecord);
        await _cacheManager.RemoveByPrefixAsync(Prefix);
    }

    public virtual Task<UrlRecord?> GetBySlugAsync(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return Task.FromResult<UrlRecord?>(null);

        var query = from ur in _urlRecordRepository.Table
                    where ur.Slug == slug
                    orderby ur.IsActive descending, ur.Id
                    select ur;
        return Task.FromResult(query.FirstOrDefault());
    }

    public virtual Task<IPagedList<UrlRecord>> GetAllUrlRecordsAsync(string slug = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _urlRecordRepository.Table;
        if (!string.IsNullOrWhiteSpace(slug))
            query = query.Where(ur => ur.Slug!.Contains(slug));
        query = query.OrderBy(ur => ur.Slug);
        IPagedList<UrlRecord> result = new PagedList<UrlRecord>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public virtual async Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId)
    {
        var key = new CacheKey(string.Format(ActiveSlugKey, entityId, entityName, languageId), Prefix);

        if (_localizationSettings.LoadAllUrlRecordsOnStartup)
        {
            return await _cacheManager.GetAsync(key, () =>
            {
                var allRecords = GetAllUrlRecordsCached();
                var slug = allRecords
                    .Where(ur => ur.EntityId == entityId && ur.EntityName == entityName && ur.LanguageId == languageId && ur.IsActive)
                    .OrderByDescending(ur => ur.Id)
                    .Select(ur => ur.Slug)
                    .FirstOrDefault();
                return Task.FromResult(slug ?? string.Empty);
            }) ?? string.Empty;
        }

        return await _cacheManager.GetAsync(key, () =>
        {
            var slug = _urlRecordRepository.Table
                .Where(ur => ur.EntityId == entityId && ur.EntityName == entityName && ur.LanguageId == languageId && ur.IsActive)
                .OrderByDescending(ur => ur.Id)
                .Select(ur => ur.Slug)
                .FirstOrDefault();
            return Task.FromResult(slug ?? string.Empty);
        }) ?? string.Empty;
    }

    public virtual async Task SaveSlugAsync<T>(T entity, string slug, int languageId) where T : BaseEntity, ISlugSupported
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityId = entity.Id;
        var entityName = typeof(T).Name;

        var allUrlRecords = _urlRecordRepository.Table
            .Where(ur => ur.EntityId == entityId && ur.EntityName == entityName && ur.LanguageId == languageId)
            .OrderByDescending(ur => ur.Id)
            .ToList();
        var activeUrlRecord = allUrlRecords.FirstOrDefault(x => x.IsActive);

        if (activeUrlRecord == null && !string.IsNullOrWhiteSpace(slug))
        {
            var nonActiveMatch = allUrlRecords.FirstOrDefault(x =>
                string.Equals(x.Slug, slug, StringComparison.InvariantCultureIgnoreCase) && !x.IsActive);
            if (nonActiveMatch != null)
            {
                nonActiveMatch.IsActive = true;
                await UpdateUrlRecordAsync(nonActiveMatch);
            }
            else
            {
                await InsertUrlRecordAsync(new UrlRecord
                {
                    EntityId = entityId,
                    EntityName = entityName,
                    Slug = slug,
                    LanguageId = languageId,
                    IsActive = true
                });
            }
        }

        if (activeUrlRecord != null && string.IsNullOrWhiteSpace(slug))
        {
            activeUrlRecord.IsActive = false;
            await UpdateUrlRecordAsync(activeUrlRecord);
        }

        if (activeUrlRecord != null && !string.IsNullOrWhiteSpace(slug))
        {
            if (!string.Equals(activeUrlRecord.Slug, slug, StringComparison.InvariantCultureIgnoreCase))
            {
                var nonActiveMatch = allUrlRecords.FirstOrDefault(x =>
                    string.Equals(x.Slug, slug, StringComparison.InvariantCultureIgnoreCase) && !x.IsActive);
                if (nonActiveMatch != null)
                {
                    nonActiveMatch.IsActive = true;
                    await UpdateUrlRecordAsync(nonActiveMatch);
                }
                else
                {
                    await InsertUrlRecordAsync(new UrlRecord
                    {
                        EntityId = entityId,
                        EntityName = entityName,
                        Slug = slug,
                        LanguageId = languageId,
                        IsActive = true
                    });
                }

                activeUrlRecord.IsActive = false;
                await UpdateUrlRecordAsync(activeUrlRecord);
            }
        }
    }

    private IList<UrlRecord> GetAllUrlRecordsCached()
    {
        var key = new CacheKey(AllKey, Prefix);
        return _cacheManager.Get(key, () =>
            _urlRecordRepository.TableNoTracking.ToList() as IList<UrlRecord>)
            ?? Array.Empty<UrlRecord>();
    }
}
