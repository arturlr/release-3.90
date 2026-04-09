using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Domain.Configuration;
using Nop.Services.Events;

namespace Nop.Services.Configuration;

/// <summary>
/// Setting manager
/// </summary>
public class SettingService : ISettingService
{
    private const string SettingsAllKey = "Nop.setting.all";
    private const string SettingsPrefix = "Nop.setting.";

    private readonly IRepository<Setting> _settingRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public SettingService(
        IRepository<Setting> settingRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _settingRepository = settingRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Gets all settings cached as dictionary keyed by lowercase name
    /// </summary>
    protected virtual async Task<IDictionary<string, IList<SettingForCaching>>> GetAllSettingsCachedAsync()
    {
        var key = new CacheKey(SettingsAllKey, SettingsPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var settings = _settingRepository.TableNoTracking
                .OrderBy(s => s.Name).ThenBy(s => s.StoreId)
                .ToList();

            var dictionary = new Dictionary<string, IList<SettingForCaching>>();
            foreach (var s in settings)
            {
                var resourceName = (s.Name ?? string.Empty).ToLowerInvariant();
                var cached = new SettingForCaching
                {
                    Id = s.Id,
                    Name = s.Name ?? string.Empty,
                    Value = s.Value ?? string.Empty,
                    StoreId = s.StoreId
                };

                if (!dictionary.TryGetValue(resourceName, out var list))
                {
                    list = new List<SettingForCaching>();
                    dictionary[resourceName] = list;
                }
                list.Add(cached);
            }
            return Task.FromResult((IDictionary<string, IList<SettingForCaching>>)dictionary);
        }) ?? new Dictionary<string, IList<SettingForCaching>>();
    }

    public virtual Task<Setting?> GetSettingByIdAsync(int settingId)
    {
        return Task.FromResult(settingId == 0 ? null : _settingRepository.GetById(settingId));
    }

    public virtual async Task<Setting?> GetSettingAsync(string key, int storeId = 0, bool loadSharedValueIfNotFound = false)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var settings = await GetAllSettingsCachedAsync();
        key = key.Trim().ToLowerInvariant();

        if (!settings.TryGetValue(key, out var settingsByKey))
            return null;

        var setting = settingsByKey.FirstOrDefault(x => x.StoreId == storeId);

        if (setting is null && storeId > 0 && loadSharedValueIfNotFound)
            setting = settingsByKey.FirstOrDefault(x => x.StoreId == 0);

        return setting is not null ? _settingRepository.GetById(setting.Id) : null;
    }

    public virtual async Task<T> GetSettingByKeyAsync<T>(string key, T defaultValue = default!,
        int storeId = 0, bool loadSharedValueIfNotFound = false)
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        var settings = await GetAllSettingsCachedAsync();
        key = key.Trim().ToLowerInvariant();

        if (!settings.TryGetValue(key, out var settingsByKey))
            return defaultValue;

        var setting = settingsByKey.FirstOrDefault(x => x.StoreId == storeId);

        if (setting is null && storeId > 0 && loadSharedValueIfNotFound)
            setting = settingsByKey.FirstOrDefault(x => x.StoreId == 0);

        return setting is not null ? CommonHelper.To<T>(setting.Value) : defaultValue;
    }

    public virtual async Task SetSettingAsync<T>(string key, T value, int storeId = 0, bool clearCache = true)
    {
        ArgumentNullException.ThrowIfNull(key);
        key = key.Trim().ToLowerInvariant();

        var valueStr = TypeDescriptor.GetConverter(typeof(T)).ConvertToInvariantString(value) ?? string.Empty;

        var allSettings = await GetAllSettingsCachedAsync();
        var settingForCaching = allSettings.TryGetValue(key, out var list)
            ? list.FirstOrDefault(x => x.StoreId == storeId)
            : null;

        if (settingForCaching is not null)
        {
            var setting = _settingRepository.GetById(settingForCaching.Id);
            if (setting is not null)
            {
                setting.Value = valueStr;
                _settingRepository.Update(setting);

                if (clearCache)
                    await _cacheManager.RemoveByPrefixAsync(SettingsPrefix);

                await _eventPublisher.EntityUpdatedAsync(setting);
            }
        }
        else
        {
            var setting = new Setting(key, valueStr, storeId);
            _settingRepository.Insert(setting);

            if (clearCache)
                await _cacheManager.RemoveByPrefixAsync(SettingsPrefix);

            await _eventPublisher.EntityInsertedAsync(setting);
        }
    }

    public virtual Task<IList<Setting>> GetAllSettingsAsync()
    {
        IList<Setting> result = _settingRepository.Table
            .OrderBy(s => s.Name).ThenBy(s => s.StoreId)
            .ToList();
        return Task.FromResult(result);
    }

    public virtual async Task<bool> SettingExistsAsync<T, TPropType>(T settings,
        Expression<Func<T, TPropType>> keySelector, int storeId = 0)
        where T : ISettings, new()
    {
        var key = settings.GetSettingKey(keySelector);
        var setting = await GetSettingByKeyAsync<string>(key, storeId: storeId);
        return setting is not null;
    }

    public virtual async Task<T> LoadSettingAsync<T>(int storeId = 0) where T : ISettings, new()
    {
        var settings = new T();

        foreach (var prop in typeof(T).GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            var key = typeof(T).Name + "." + prop.Name;
            var setting = await GetSettingByKeyAsync<string>(key, storeId: storeId, loadSharedValueIfNotFound: true);
            if (setting is null)
                continue;

            var converter = TypeDescriptor.GetConverter(prop.PropertyType);
            if (!converter.CanConvertFrom(typeof(string)) || !converter.IsValid(setting))
                continue;

            prop.SetValue(settings, converter.ConvertFromInvariantString(setting));
        }

        return settings;
    }

    public virtual async Task SaveSettingAsync<T>(T settings, int storeId = 0) where T : ISettings, new()
    {
        foreach (var prop in typeof(T).GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            if (!TypeDescriptor.GetConverter(prop.PropertyType).CanConvertFrom(typeof(string)))
                continue;

            var key = typeof(T).Name + "." + prop.Name;
            var value = prop.GetValue(settings);
            await SetSettingAsync(key, value ?? string.Empty, storeId, clearCache: false);
        }

        await ClearCacheAsync();
    }

    public virtual async Task SaveSettingAsync<T, TPropType>(T settings,
        Expression<Func<T, TPropType>> keySelector,
        int storeId = 0, bool clearCache = true) where T : ISettings, new()
    {
        if (keySelector.Body is not MemberExpression member || member.Member is not PropertyInfo propInfo)
            throw new ArgumentException($"Expression '{keySelector}' does not refer to a property.");

        var key = settings.GetSettingKey(keySelector);
        var value = propInfo.GetValue(settings);
        await SetSettingAsync(key, value ?? string.Empty, storeId, clearCache);
    }

    public virtual async Task SaveSettingOverridablePerStoreAsync<T, TPropType>(T settings,
        Expression<Func<T, TPropType>> keySelector,
        bool overrideForStore, int storeId = 0, bool clearCache = true) where T : ISettings, new()
    {
        if (overrideForStore || storeId == 0)
            await SaveSettingAsync(settings, keySelector, storeId, clearCache);
        else if (storeId > 0)
            await DeleteSettingAsync(settings, keySelector, storeId);
    }

    public virtual async Task DeleteSettingAsync<T>() where T : ISettings, new()
    {
        var allSettings = await GetAllSettingsAsync();
        var settingsToDelete = new List<Setting>();

        foreach (var prop in typeof(T).GetProperties())
        {
            var key = typeof(T).Name + "." + prop.Name;
            settingsToDelete.AddRange(allSettings.Where(x =>
                x.Name?.Equals(key, StringComparison.OrdinalIgnoreCase) == true));
        }

        if (settingsToDelete.Count > 0)
            await DeleteSettingsAsync(settingsToDelete);
    }

    public virtual async Task DeleteSettingAsync<T, TPropType>(T settings,
        Expression<Func<T, TPropType>> keySelector, int storeId = 0)
        where T : ISettings, new()
    {
        var key = settings.GetSettingKey(keySelector).Trim().ToLowerInvariant();

        var allSettings = await GetAllSettingsCachedAsync();
        if (!allSettings.TryGetValue(key, out var list))
            return;

        var settingForCaching = list.FirstOrDefault(x => x.StoreId == storeId);
        if (settingForCaching is null)
            return;

        var setting = _settingRepository.GetById(settingForCaching.Id);
        if (setting is not null)
            await DeleteSettingAsync(setting);
    }

    public virtual async Task DeleteSettingAsync(Setting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _settingRepository.Delete(setting);
        await _cacheManager.RemoveByPrefixAsync(SettingsPrefix);
        await _eventPublisher.EntityDeletedAsync(setting);
    }

    public virtual async Task DeleteSettingsAsync(IList<Setting> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settingRepository.Delete(settings);
        await _cacheManager.RemoveByPrefixAsync(SettingsPrefix);

        foreach (var setting in settings)
            await _eventPublisher.EntityDeletedAsync(setting);
    }

    public virtual async Task ClearCacheAsync()
    {
        await _cacheManager.RemoveByPrefixAsync(SettingsPrefix);
    }

    /// <summary>
    /// Lightweight DTO for caching settings without EF tracking overhead
    /// </summary>
    public class SettingForCaching
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int StoreId { get; set; }
    }
}
