using System.Linq.Expressions;
using System.Reflection;
using Nop.Core.Configuration;

namespace Nop.Services.Configuration;

/// <summary>
/// Setting key helper extensions
/// </summary>
public static class SettingExtensions
{
    /// <summary>
    /// Get setting key (stored into database) from a property expression
    /// </summary>
    public static string GetSettingKey<T, TPropType>(this T entity,
        Expression<Func<T, TPropType>> keySelector)
        where T : ISettings, new()
    {
        if (keySelector.Body is not MemberExpression member)
            throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");

        if (member.Member is not PropertyInfo)
            throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");

        return typeof(T).Name + "." + member.Member.Name;
    }
}
