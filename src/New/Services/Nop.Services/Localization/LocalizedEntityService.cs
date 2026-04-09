using System.Linq.Expressions;
using System.Reflection;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;

namespace Nop.Services.Localization;

/// <summary>
/// Localized entity service — stores per-language translations for any entity property.
/// </summary>
public class LocalizedEntityService : ILocalizedEntityService
{
    private const string LocalizedPropertyKey = "Nop.localizedproperty.value-{0}-{1}-{2}-{3}";
    private const string LocalizedPropertyAllKey = "Nop.localizedproperty.all";
    private const string LocalizedPropertyPrefix = "Nop.localizedproperty.";

    private readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly LocalizationSettings _localizationSettings;

    public LocalizedEntityService(
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IStaticCacheManager cacheManager,
        LocalizationSettings localizationSettings)
    {
        _localizedPropertyRepository = localizedPropertyRepository;
        _cacheManager = cacheManager;
        _localizationSettings = localizationSettings;
    }

    public virtual async Task DeleteLocalizedPropertyAsync(LocalizedProperty localizedProperty)
    {
        ArgumentNullException.ThrowIfNull(localizedProperty);
        _localizedPropertyRepository.Delete(localizedProperty);
        await _cacheManager.RemoveByPrefixAsync(LocalizedPropertyPrefix);
    }

    public virtual Task<LocalizedProperty?> GetLocalizedPropertyByIdAsync(int localizedPropertyId)
    {
        if (localizedPropertyId == 0)
            return Task.FromResult<LocalizedProperty?>(null);
        return Task.FromResult<LocalizedProperty?>(_localizedPropertyRepository.GetById(localizedPropertyId));
    }

    public virtual Task<string> GetLocalizedValueAsync(int languageId, int entityId, string localeKeyGroup, string localeKey)
    {
        var key = new CacheKey(
            string.Format(LocalizedPropertyKey, languageId, entityId, localeKeyGroup, localeKey),
            LocalizedPropertyPrefix);

        if (_localizationSettings.LoadAllLocalizedPropertiesOnStartup)
        {
            return _cacheManager.GetAsync(key, async () =>
            {
                var all = await GetAllLocalizedPropertiesCachedAsync();
                return all
                    .Where(lp => lp.LanguageId == languageId && lp.EntityId == entityId
                        && lp.LocaleKeyGroup == localeKeyGroup && lp.LocaleKey == localeKey)
                    .Select(lp => lp.LocaleValue)
                    .FirstOrDefault() ?? string.Empty;
            })!;
        }

        // gradual loading
        return _cacheManager.GetAsync(key, () =>
        {
            var value = _localizedPropertyRepository.Table
                .Where(lp => lp.LanguageId == languageId && lp.EntityId == entityId
                    && lp.LocaleKeyGroup == localeKeyGroup && lp.LocaleKey == localeKey)
                .Select(lp => lp.LocaleValue)
                .FirstOrDefault() ?? string.Empty;
            return Task.FromResult(value);
        })!;
    }

    public virtual async Task InsertLocalizedPropertyAsync(LocalizedProperty localizedProperty)
    {
        ArgumentNullException.ThrowIfNull(localizedProperty);
        _localizedPropertyRepository.Insert(localizedProperty);
        await _cacheManager.RemoveByPrefixAsync(LocalizedPropertyPrefix);
    }

    public virtual async Task UpdateLocalizedPropertyAsync(LocalizedProperty localizedProperty)
    {
        ArgumentNullException.ThrowIfNull(localizedProperty);
        _localizedPropertyRepository.Update(localizedProperty);
        await _cacheManager.RemoveByPrefixAsync(LocalizedPropertyPrefix);
    }

    public virtual Task SaveLocalizedValueAsync<T>(T entity, Expression<Func<T, string?>> keySelector,
        string? localeValue, int languageId) where T : BaseEntity, ILocalizedEntity
    {
        return SaveLocalizedValueAsync<T, string?>(entity, keySelector, localeValue, languageId);
    }

    public virtual async Task SaveLocalizedValueAsync<T, TPropType>(T entity, Expression<Func<T, TPropType>> keySelector,
        TPropType localeValue, int languageId) where T : BaseEntity, ILocalizedEntity
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentOutOfRangeException.ThrowIfZero(languageId);

        var member = keySelector.Body as MemberExpression
            ?? throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");
        _ = member.Member as PropertyInfo
            ?? throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");

        var localeKeyGroup = typeof(T).Name;
        var localeKey = member.Member.Name;

        var props = _localizedPropertyRepository.Table
            .Where(lp => lp.EntityId == entity.Id && lp.LocaleKeyGroup == localeKeyGroup)
            .ToList();

        var prop = props.FirstOrDefault(lp => lp.LanguageId == languageId
            && string.Equals(lp.LocaleKey, localeKey, StringComparison.InvariantCultureIgnoreCase));

        var localeValueStr = CommonHelper.To<string>(localeValue!);

        if (prop != null)
        {
            if (string.IsNullOrWhiteSpace(localeValueStr))
                await DeleteLocalizedPropertyAsync(prop);
            else
            {
                prop.LocaleValue = localeValueStr;
                await UpdateLocalizedPropertyAsync(prop);
            }
        }
        else if (!string.IsNullOrWhiteSpace(localeValueStr))
        {
            await InsertLocalizedPropertyAsync(new LocalizedProperty
            {
                EntityId = entity.Id,
                LanguageId = languageId,
                LocaleKey = localeKey,
                LocaleKeyGroup = localeKeyGroup,
                LocaleValue = localeValueStr
            });
        }
    }

    /// <summary>
    /// Gets all localized properties cached (for bulk loading mode).
    /// </summary>
    protected virtual Task<IList<LocalizedProperty>> GetAllLocalizedPropertiesCachedAsync()
    {
        var key = new CacheKey(LocalizedPropertyAllKey, LocalizedPropertyPrefix);
        return _cacheManager.GetAsync(key, () =>
        {
            IList<LocalizedProperty> result = _localizedPropertyRepository.TableNoTracking.ToList();
            return Task.FromResult(result);
        })!;
    }
}
