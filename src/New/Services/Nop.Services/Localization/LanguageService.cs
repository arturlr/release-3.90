using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Services.Configuration;
using Nop.Services.Events;

namespace Nop.Services.Localization;

/// <summary>
/// Language service
/// </summary>
public class LanguageService : ILanguageService
{
    private const string LanguagesAllKey = "Nop.language.all-{0}";
    private const string LanguagesByIdKey = "Nop.language.id-{0}";
    private const string LanguagesPrefix = "Nop.language.";

    private readonly IRepository<Language> _languageRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly ISettingService _settingService;
    private readonly LocalizationSettings _localizationSettings;
    private readonly IEventPublisher _eventPublisher;

    public LanguageService(
        IRepository<Language> languageRepository,
        IStaticCacheManager cacheManager,
        ISettingService settingService,
        LocalizationSettings localizationSettings,
        IEventPublisher eventPublisher)
    {
        _languageRepository = languageRepository;
        _cacheManager = cacheManager;
        _settingService = settingService;
        _localizationSettings = localizationSettings;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task DeleteLanguageAsync(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        // update default admin language if we're deleting it
        if (_localizationSettings.DefaultAdminLanguageId == language.Id)
        {
            var allLanguages = await GetAllLanguagesAsync();
            foreach (var lang in allLanguages)
            {
                if (lang.Id != language.Id)
                {
                    _localizationSettings.DefaultAdminLanguageId = lang.Id;
                    await _settingService.SaveSettingAsync(_localizationSettings);
                    break;
                }
            }
        }

        _languageRepository.Delete(language);
        await _cacheManager.RemoveByPrefixAsync(LanguagesPrefix);
        await _eventPublisher.EntityDeletedAsync(language);
    }

    public virtual Task<IList<Language>> GetAllLanguagesAsync(bool showHidden = false, int storeId = 0)
    {
        var key = new CacheKey(string.Format(LanguagesAllKey, showHidden), LanguagesPrefix);
        return _cacheManager.GetAsync(key, () =>
        {
            var query = _languageRepository.Table;
            if (!showHidden)
                query = query.Where(l => l.Published);
            query = query.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Id);
            return Task.FromResult<IList<Language>>(query.ToList());
        })!;
    }

    public virtual Task<Language?> GetLanguageByIdAsync(int languageId)
    {
        if (languageId == 0)
            return Task.FromResult<Language?>(null);

        var key = new CacheKey(string.Format(LanguagesByIdKey, languageId), LanguagesPrefix);
        return _cacheManager.GetAsync(key, () =>
            Task.FromResult<Language?>(_languageRepository.GetById(languageId)));
    }

    public virtual async Task InsertLanguageAsync(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);
        _languageRepository.Insert(language);
        await _cacheManager.RemoveByPrefixAsync(LanguagesPrefix);
        await _eventPublisher.EntityInsertedAsync(language);
    }

    public virtual async Task UpdateLanguageAsync(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);
        _languageRepository.Update(language);
        await _cacheManager.RemoveByPrefixAsync(LanguagesPrefix);
        await _eventPublisher.EntityUpdatedAsync(language);
    }
}
