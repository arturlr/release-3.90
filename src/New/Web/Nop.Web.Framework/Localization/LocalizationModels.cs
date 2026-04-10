using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Services.Localization;

namespace Nop.Web.Framework.Localization;

/// <summary>
/// Localized display name attribute for model properties.
/// Resolves localized resource string at display time via ILocalizationService.
/// Uses IHttpContextAccessor to resolve services (unavoidable — attribute metadata provider has no DI).
/// </summary>
public class NopResourceDisplayName : DisplayNameAttribute
{
    private static IHttpContextAccessor? _httpContextAccessor;

    public NopResourceDisplayName(string resourceKey) : base(resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public string ResourceKey { get; set; }

    public override string DisplayName
    {
        get
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return ResourceKey;

            var workContext = httpContext.RequestServices.GetService<IWorkContext>();
            var localizationService = httpContext.RequestServices.GetService<ILocalizationService>();
            if (workContext == null || localizationService == null)
                return ResourceKey;

            var langId = workContext.WorkingLanguage.Id;
            return localizationService.GetResourceAsync(ResourceKey, langId, true, ResourceKey)
                .GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Called once at startup to provide the IHttpContextAccessor.
    /// </summary>
    public static void Configure(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;
}

/// <summary>
/// Marker interface for localized models.
/// </summary>
public interface ILocalizedModel
{
}

/// <summary>
/// Localized model with a list of locale-specific models.
/// </summary>
public interface ILocalizedModel<TLocalizedModel> : ILocalizedModel
{
    IList<TLocalizedModel> Locales { get; set; }
}

/// <summary>
/// Locale-specific model with a language ID.
/// </summary>
public interface ILocalizedModelLocal
{
    int LanguageId { get; set; }
}
