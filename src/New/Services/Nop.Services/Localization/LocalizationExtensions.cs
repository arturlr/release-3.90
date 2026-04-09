using System.Linq.Expressions;
using System.Reflection;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Services.Configuration;

namespace Nop.Services.Localization;

/// <summary>
/// Localization extension methods — entity property localization, enum localization, setting localization.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Get localized property of an entity.
    /// </summary>
    public static async Task<TPropType?> GetLocalizedAsync<T, TPropType>(this T entity,
        Expression<Func<T, TPropType>> keySelector,
        int languageId,
        ILocalizedEntityService localizedEntityService,
        ILanguageService languageService,
        bool returnDefaultValue = true,
        bool ensureTwoPublishedLanguages = true)
        where T : BaseEntity, ILocalizedEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var member = keySelector.Body as MemberExpression
            ?? throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");
        _ = member.Member as PropertyInfo
            ?? throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");

        var result = default(TPropType);
        var resultStr = string.Empty;

        var localeKeyGroup = typeof(T).Name;
        var localeKey = member.Member.Name;

        if (languageId > 0)
        {
            var loadLocalizedValue = true;
            if (ensureTwoPublishedLanguages)
            {
                var totalPublishedLanguages = (await languageService.GetAllLanguagesAsync()).Count;
                loadLocalizedValue = totalPublishedLanguages >= 2;
            }

            if (loadLocalizedValue)
            {
                resultStr = await localizedEntityService.GetLocalizedValueAsync(languageId, entity.Id, localeKeyGroup, localeKey);
                if (!string.IsNullOrEmpty(resultStr))
                    result = CommonHelper.To<TPropType>(resultStr);
            }
        }

        if (string.IsNullOrEmpty(resultStr) && returnDefaultValue)
        {
            var localizer = keySelector.Compile();
            result = localizer(entity);
        }

        return result;
    }

    /// <summary>
    /// Get localized value of an enum.
    /// </summary>
    public static async Task<string> GetLocalizedEnumAsync<T>(this T enumValue,
        ILocalizationService localizationService, int languageId)
        where T : struct, Enum
    {
        var resourceName = $"Enums.{typeof(T)}.{enumValue}";
        var result = await localizationService.GetResourceAsync(resourceName, languageId, false, "", true);

        if (string.IsNullOrEmpty(result))
            result = CommonHelper.ConvertEnum(enumValue.ToString());

        return result;
    }

    /// <summary>
    /// Get localized property of a setting.
    /// </summary>
    public static async Task<string?> GetLocalizedSettingAsync<T>(this T settings,
        Expression<Func<T, string>> keySelector,
        int languageId, int storeId,
        ISettingService settingService,
        ILocalizedEntityService localizedEntityService,
        ILanguageService languageService,
        bool returnDefaultValue = true,
        bool ensureTwoPublishedLanguages = true)
        where T : ISettings, new()
    {
        var key = settings.GetSettingKey(keySelector);
        var setting = await settingService.GetSettingAsync(key, storeId: storeId, loadSharedValueIfNotFound: true);
        if (setting == null)
            return null;

        return await setting.GetLocalizedAsync(x => x.Value, languageId,
            localizedEntityService, languageService, returnDefaultValue, ensureTwoPublishedLanguages);
    }

    /// <summary>
    /// Save localized property of a setting.
    /// </summary>
    public static async Task SaveLocalizedSettingAsync<T>(this T settings,
        Expression<Func<T, string>> keySelector,
        int languageId, string? value,
        ISettingService settingService,
        ILocalizedEntityService localizedEntityService)
        where T : ISettings, new()
    {
        var key = settings.GetSettingKey(keySelector);
        var setting = await settingService.GetSettingAsync(key, storeId: 0, loadSharedValueIfNotFound: false);
        if (setting == null)
            return;

        await localizedEntityService.SaveLocalizedValueAsync(setting, x => x.Value, value, languageId);
    }

    /// <summary>
    /// Add or update a locale resource across all languages.
    /// </summary>
    public static async Task AddOrUpdateLocaleResourceAsync(
        ILocalizationService localizationService,
        ILanguageService languageService,
        string resourceName, string resourceValue,
        string? languageCulture = null)
    {
        foreach (var lang in await languageService.GetAllLanguagesAsync(true))
        {
            if (!string.IsNullOrEmpty(languageCulture) && !languageCulture.Equals(lang.LanguageCulture))
                continue;

            var lsr = await localizationService.GetLocaleStringResourceByNameAsync(resourceName, lang.Id, false);
            if (lsr == null)
            {
                await localizationService.InsertLocaleStringResourceAsync(new LocaleStringResource
                {
                    LanguageId = lang.Id,
                    ResourceName = resourceName,
                    ResourceValue = resourceValue
                });
            }
            else
            {
                lsr.ResourceValue = resourceValue;
                await localizationService.UpdateLocaleStringResourceAsync(lsr);
            }
        }
    }

    /// <summary>
    /// Delete a locale resource across all languages.
    /// </summary>
    public static async Task DeleteLocaleResourceAsync(
        ILocalizationService localizationService,
        ILanguageService languageService,
        string resourceName)
    {
        foreach (var lang in await languageService.GetAllLanguagesAsync(true))
        {
            var lsr = await localizationService.GetLocaleStringResourceByNameAsync(resourceName, lang.Id, false);
            if (lsr != null)
                await localizationService.DeleteLocaleStringResourceAsync(lsr);
        }
    }

    /// <summary>
    /// Save localized permission name across all languages.
    /// </summary>
    public static void SaveLocalizedPermissionName(this PermissionRecord permissionRecord,
        ILocalizationService localizationService, ILanguageService languageService)
    {
        ArgumentNullException.ThrowIfNull(permissionRecord);
        var resourceName = $"Permission.{permissionRecord.SystemName}";
        AddOrUpdateLocaleResourceAsync(localizationService, languageService, resourceName, permissionRecord.Name ?? string.Empty)
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Delete localized permission name across all languages.
    /// </summary>
    public static void DeleteLocalizedPermissionName(this PermissionRecord permissionRecord,
        ILocalizationService localizationService, ILanguageService languageService)
    {
        ArgumentNullException.ThrowIfNull(permissionRecord);
        var resourceName = $"Permission.{permissionRecord.SystemName}";
        DeleteLocaleResourceAsync(localizationService, languageService, resourceName)
            .GetAwaiter().GetResult();
    }
}
