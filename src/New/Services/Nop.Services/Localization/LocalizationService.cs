using System.Text;
using System.Xml;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Services.Localization;

/// <summary>
/// Localization service — manages locale string resources (key/value translations per language).
/// </summary>
public class LocalizationService : ILocalizationService
{
    private const string LsrAllKey = "Nop.lsr.all-{0}";
    private const string LsrByNameKey = "Nop.lsr.{0}-{1}";
    private const string LsrPrefix = "Nop.lsr.";

    private readonly IRepository<LocaleStringResource> _lsrRepository;
    private readonly IWorkContext _workContext;
    private readonly INopLogger _logger;
    private readonly ILanguageService _languageService;
    private readonly IStaticCacheManager _cacheManager;
    private readonly LocalizationSettings _localizationSettings;
    private readonly IEventPublisher _eventPublisher;

    public LocalizationService(
        IRepository<LocaleStringResource> lsrRepository,
        IWorkContext workContext,
        INopLogger logger,
        ILanguageService languageService,
        IStaticCacheManager cacheManager,
        LocalizationSettings localizationSettings,
        IEventPublisher eventPublisher)
    {
        _lsrRepository = lsrRepository;
        _workContext = workContext;
        _logger = logger;
        _languageService = languageService;
        _cacheManager = cacheManager;
        _localizationSettings = localizationSettings;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task DeleteLocaleStringResourceAsync(LocaleStringResource localeStringResource)
    {
        ArgumentNullException.ThrowIfNull(localeStringResource);
        _lsrRepository.Delete(localeStringResource);
        await _cacheManager.RemoveByPrefixAsync(LsrPrefix);
        await _eventPublisher.EntityDeletedAsync(localeStringResource);
    }

    public virtual Task<LocaleStringResource?> GetLocaleStringResourceByIdAsync(int localeStringResourceId)
    {
        if (localeStringResourceId == 0)
            return Task.FromResult<LocaleStringResource?>(null);
        return Task.FromResult<LocaleStringResource?>(_lsrRepository.GetById(localeStringResourceId));
    }

    public virtual Task<LocaleStringResource?> GetLocaleStringResourceByNameAsync(string resourceName, int languageId, bool logIfNotFound = true)
    {
        var resource = _lsrRepository.Table
            .Where(lsr => lsr.LanguageId == languageId && lsr.ResourceName == resourceName)
            .OrderBy(lsr => lsr.ResourceName)
            .FirstOrDefault();

        if (resource == null && logIfNotFound)
            _logger.Warning($"Resource string ({resourceName}) not found. Language ID = {languageId}");

        return Task.FromResult<LocaleStringResource?>(resource);
    }

    public virtual Task<IList<LocaleStringResource>> GetAllResourcesAsync(int languageId)
    {
        var resources = _lsrRepository.Table
            .Where(l => l.LanguageId == languageId)
            .OrderBy(l => l.ResourceName)
            .ToList();
        return Task.FromResult<IList<LocaleStringResource>>(resources);
    }

    public virtual async Task InsertLocaleStringResourceAsync(LocaleStringResource localeStringResource)
    {
        ArgumentNullException.ThrowIfNull(localeStringResource);
        _lsrRepository.Insert(localeStringResource);
        await _cacheManager.RemoveByPrefixAsync(LsrPrefix);
        await _eventPublisher.EntityInsertedAsync(localeStringResource);
    }

    public virtual async Task UpdateLocaleStringResourceAsync(LocaleStringResource localeStringResource)
    {
        ArgumentNullException.ThrowIfNull(localeStringResource);
        _lsrRepository.Update(localeStringResource);
        await _cacheManager.RemoveByPrefixAsync(LsrPrefix);
        await _eventPublisher.EntityUpdatedAsync(localeStringResource);
    }

    public virtual Task<Dictionary<string, KeyValuePair<int, string>>> GetAllResourceValuesAsync(int languageId)
    {
        var key = new CacheKey(string.Format(LsrAllKey, languageId), LsrPrefix);
        return _cacheManager.GetAsync(key, () =>
        {
            var locales = _lsrRepository.TableNoTracking
                .Where(l => l.LanguageId == languageId)
                .OrderBy(l => l.ResourceName)
                .ToList();

            var dictionary = new Dictionary<string, KeyValuePair<int, string>>();
            foreach (var locale in locales)
            {
                var resourceName = (locale.ResourceName ?? string.Empty).ToLowerInvariant();
                dictionary.TryAdd(resourceName, new KeyValuePair<int, string>(locale.Id, locale.ResourceValue ?? string.Empty));
            }
            return Task.FromResult(dictionary);
        })!;
    }

    public virtual async Task<string> GetResourceAsync(string resourceKey)
    {
        if (_workContext.WorkingLanguage != null)
            return await GetResourceAsync(resourceKey, _workContext.WorkingLanguage.Id);
        return string.Empty;
    }

    public virtual async Task<string> GetResourceAsync(string resourceKey, int languageId,
        bool logIfNotFound = true, string defaultValue = "", bool returnEmptyIfNotFound = false)
    {
        var result = string.Empty;
        resourceKey = (resourceKey ?? string.Empty).Trim().ToLowerInvariant();

        if (_localizationSettings.LoadAllLocaleRecordsOnStartup)
        {
            var resources = await GetAllResourceValuesAsync(languageId);
            if (resources.TryGetValue(resourceKey, out var kvp))
                result = kvp.Value;
        }
        else
        {
            var key = new CacheKey(string.Format(LsrByNameKey, languageId, resourceKey), LsrPrefix);
            var lsr = await _cacheManager.GetAsync(key, () =>
            {
                var value = _lsrRepository.Table
                    .Where(l => l.ResourceName == resourceKey && l.LanguageId == languageId)
                    .Select(l => l.ResourceValue)
                    .FirstOrDefault();
                return Task.FromResult(value ?? string.Empty);
            });
            if (!string.IsNullOrEmpty(lsr))
                result = lsr!;
        }

        if (string.IsNullOrEmpty(result))
        {
            if (logIfNotFound)
                _logger.Warning($"Resource string ({resourceKey}) is not found. Language ID = {languageId}");

            if (!string.IsNullOrEmpty(defaultValue))
                result = defaultValue;
            else if (!returnEmptyIfNotFound)
                result = resourceKey;
        }

        return result;
    }

    public virtual async Task<string> ExportResourcesToXmlAsync(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var sb = new StringBuilder();
        using var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Async = true });
        await xw.WriteStartDocumentAsync();
        await xw.WriteStartElementAsync(null, "Language", null);
        xw.WriteAttributeString("Name", language.Name);
        xw.WriteAttributeString("SupportedVersion", NopVersion.CurrentVersion);

        var resources = await GetAllResourcesAsync(language.Id);
        foreach (var resource in resources)
        {
            await xw.WriteStartElementAsync(null, "LocaleResource", null);
            xw.WriteAttributeString("Name", resource.ResourceName);
            await xw.WriteElementStringAsync(null, "Value", null, resource.ResourceValue ?? string.Empty);
            await xw.WriteEndElementAsync();
        }

        await xw.WriteEndElementAsync();
        await xw.WriteEndDocumentAsync();
        await xw.FlushAsync();
        return sb.ToString();
    }

    public virtual async Task ImportResourcesFromXmlAsync(Language language, string xml, bool updateExistingResources = true)
    {
        ArgumentNullException.ThrowIfNull(language);
        if (string.IsNullOrEmpty(xml))
            return;

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);

        var nodes = xmlDoc.SelectNodes(@"//Language/LocaleResource");
        if (nodes == null)
            return;

        // load existing resources for this language into a lookup
        var existing = _lsrRepository.Table
            .Where(r => r.LanguageId == language.Id)
            .ToList()
            .ToDictionary(r => (r.ResourceName ?? string.Empty).ToLowerInvariant(), r => r);

        var toInsert = new List<LocaleStringResource>();

        foreach (XmlNode node in nodes)
        {
            var name = node.Attributes?["Name"]?.InnerText.Trim();
            if (string.IsNullOrEmpty(name))
                continue;

            var value = node.SelectSingleNode("Value")?.InnerText ?? string.Empty;
            var nameKey = name.ToLowerInvariant();

            if (existing.TryGetValue(nameKey, out var existingResource))
            {
                if (updateExistingResources)
                {
                    existingResource.ResourceValue = value;
                    _lsrRepository.Update(existingResource);
                }
            }
            else
            {
                toInsert.Add(new LocaleStringResource
                {
                    LanguageId = language.Id,
                    ResourceName = name,
                    ResourceValue = value
                });
            }
        }

        if (toInsert.Count > 0)
            _lsrRepository.Insert(toInsert);

        await _cacheManager.RemoveByPrefixAsync(LsrPrefix);
    }
}
