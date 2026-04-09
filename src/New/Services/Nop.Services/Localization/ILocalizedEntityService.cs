using System.Linq.Expressions;
using Nop.Core;
using Nop.Core.Domain.Localization;

namespace Nop.Services.Localization;

/// <summary>
/// Localized entity service — stores per-language translations for any entity property.
/// </summary>
public interface ILocalizedEntityService
{
    Task DeleteLocalizedPropertyAsync(LocalizedProperty localizedProperty);
    Task<LocalizedProperty?> GetLocalizedPropertyByIdAsync(int localizedPropertyId);

    /// <summary>
    /// Find a localized value for a specific entity/property/language combination.
    /// </summary>
    Task<string> GetLocalizedValueAsync(int languageId, int entityId, string localeKeyGroup, string localeKey);

    Task InsertLocalizedPropertyAsync(LocalizedProperty localizedProperty);
    Task UpdateLocalizedPropertyAsync(LocalizedProperty localizedProperty);

    /// <summary>
    /// Save a localized value for an entity property (insert/update/delete as needed).
    /// </summary>
    Task SaveLocalizedValueAsync<T>(T entity, Expression<Func<T, string?>> keySelector,
        string? localeValue, int languageId) where T : BaseEntity, ILocalizedEntity;

    /// <summary>
    /// Save a localized value for an entity property of any type.
    /// </summary>
    Task SaveLocalizedValueAsync<T, TPropType>(T entity, Expression<Func<T, TPropType>> keySelector,
        TPropType localeValue, int languageId) where T : BaseEntity, ILocalizedEntity;
}
