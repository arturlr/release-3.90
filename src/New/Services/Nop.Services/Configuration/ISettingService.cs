using System.Linq.Expressions;
using Nop.Core.Configuration;
using Nop.Core.Domain.Configuration;

namespace Nop.Services.Configuration;

/// <summary>
/// Setting service interface
/// </summary>
public interface ISettingService
{
    /// <summary>
    /// Gets a setting by identifier
    /// </summary>
    Task<Setting?> GetSettingByIdAsync(int settingId);

    /// <summary>
    /// Get setting by key
    /// </summary>
    Task<Setting?> GetSettingAsync(string key, int storeId = 0, bool loadSharedValueIfNotFound = false);

    /// <summary>
    /// Get setting value by key
    /// </summary>
    Task<T> GetSettingByKeyAsync<T>(string key, T defaultValue = default!, int storeId = 0, bool loadSharedValueIfNotFound = false);

    /// <summary>
    /// Set setting value
    /// </summary>
    Task SetSettingAsync<T>(string key, T value, int storeId = 0, bool clearCache = true);

    /// <summary>
    /// Gets all settings
    /// </summary>
    Task<IList<Setting>> GetAllSettingsAsync();

    /// <summary>
    /// Determines whether a setting exists
    /// </summary>
    Task<bool> SettingExistsAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector, int storeId = 0)
        where T : ISettings, new();

    /// <summary>
    /// Load settings
    /// </summary>
    Task<T> LoadSettingAsync<T>(int storeId = 0) where T : ISettings, new();

    /// <summary>
    /// Save settings object
    /// </summary>
    Task SaveSettingAsync<T>(T settings, int storeId = 0) where T : ISettings, new();

    /// <summary>
    /// Save settings object (single property)
    /// </summary>
    Task SaveSettingAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector,
        int storeId = 0, bool clearCache = true) where T : ISettings, new();

    /// <summary>
    /// Save settings object (per store). If not overridden per store, deletes the store-specific setting.
    /// </summary>
    Task SaveSettingOverridablePerStoreAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector,
        bool overrideForStore, int storeId = 0, bool clearCache = true) where T : ISettings, new();

    /// <summary>
    /// Delete all settings for a type
    /// </summary>
    Task DeleteSettingAsync<T>() where T : ISettings, new();

    /// <summary>
    /// Delete settings object (single property)
    /// </summary>
    Task DeleteSettingAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector, int storeId = 0)
        where T : ISettings, new();

    /// <summary>
    /// Deletes a setting
    /// </summary>
    Task DeleteSettingAsync(Setting setting);

    /// <summary>
    /// Deletes settings
    /// </summary>
    Task DeleteSettingsAsync(IList<Setting> settings);

    /// <summary>
    /// Clear cache
    /// </summary>
    Task ClearCacheAsync();
}
