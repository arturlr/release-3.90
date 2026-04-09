using Nop.Core.Domain.Localization;

namespace Nop.Services.Localization;

/// <summary>
/// Localization service interface — manages locale string resources (key/value translations).
/// </summary>
public interface ILocalizationService
{
    Task DeleteLocaleStringResourceAsync(LocaleStringResource localeStringResource);
    Task<LocaleStringResource?> GetLocaleStringResourceByIdAsync(int localeStringResourceId);
    Task<LocaleStringResource?> GetLocaleStringResourceByNameAsync(string resourceName, int languageId, bool logIfNotFound = true);
    Task<IList<LocaleStringResource>> GetAllResourcesAsync(int languageId);
    Task InsertLocaleStringResourceAsync(LocaleStringResource localeStringResource);
    Task UpdateLocaleStringResourceAsync(LocaleStringResource localeStringResource);

    /// <summary>
    /// Gets all resources for a language as a cached dictionary.
    /// Key: lowercase resource name. Value: (id, value).
    /// </summary>
    Task<Dictionary<string, KeyValuePair<int, string>>> GetAllResourceValuesAsync(int languageId);

    /// <summary>
    /// Gets a resource string for the current working language.
    /// </summary>
    Task<string> GetResourceAsync(string resourceKey);

    /// <summary>
    /// Gets a resource string for a specific language.
    /// </summary>
    Task<string> GetResourceAsync(string resourceKey, int languageId,
        bool logIfNotFound = true, string defaultValue = "", bool returnEmptyIfNotFound = false);

    /// <summary>
    /// Export language resources to XML.
    /// </summary>
    Task<string> ExportResourcesToXmlAsync(Language language);

    /// <summary>
    /// Import language resources from XML.
    /// </summary>
    Task ImportResourcesFromXmlAsync(Language language, string xml, bool updateExistingResources = true);
}
