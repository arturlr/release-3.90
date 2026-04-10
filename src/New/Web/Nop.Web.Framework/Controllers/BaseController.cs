using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.UI;
using Nop.Services.Localization;

namespace Nop.Web.Framework.Controllers;

/// <summary>
/// Base controller with shared notification system, Kendo grid error helper, and localization support.
/// </summary>
public abstract class BaseController : Controller
{
    protected virtual void SuccessNotification(string message, bool persistForTheNextRequest = true) =>
        AddNotification(NotifyType.Success, message, persistForTheNextRequest);

    protected virtual void ErrorNotification(string message, bool persistForTheNextRequest = true) =>
        AddNotification(NotifyType.Error, message, persistForTheNextRequest);

    protected virtual void ErrorNotification(Exception exception, bool persistForTheNextRequest = true)
    {
        var logger = HttpContext.RequestServices.GetService(typeof(ILogger<BaseController>)) as ILogger;
        logger?.LogError(exception, exception.Message);
        AddNotification(NotifyType.Error, exception.Message, persistForTheNextRequest);
    }

    protected virtual void WarningNotification(string message, bool persistForTheNextRequest = true) =>
        AddNotification(NotifyType.Warning, message, persistForTheNextRequest);

    protected virtual void AddNotification(NotifyType type, string message, bool persistForTheNextRequest)
    {
        var dataKey = $"nop.notifications.{type}";
        if (persistForTheNextRequest)
        {
            TempData[dataKey] ??= new List<string>();
            ((List<string>)TempData[dataKey]!).Add(message);
        }
        else
        {
            ViewData[dataKey] ??= new List<string>();
            ((List<string>)ViewData[dataKey]!).Add(message);
        }
    }

    /// <summary>
    /// Error JSON for Kendo grid
    /// </summary>
    protected JsonResult ErrorForKendoGridJson(string errorMessage) =>
        Json(new DataSourceResult { Errors = errorMessage });

    /// <summary>
    /// Add locales for localizable entities
    /// </summary>
    protected virtual void AddLocales<TLocalizedModelLocal>(ILanguageService languageService,
        IList<TLocalizedModelLocal> locales, Action<TLocalizedModelLocal, int>? configure = null)
        where TLocalizedModelLocal : ILocalizedModelLocal
    {
        foreach (var language in languageService.GetAllLanguagesAsync(showHidden: true).GetAwaiter().GetResult())
        {
            var locale = Activator.CreateInstance<TLocalizedModelLocal>();
            locale.LanguageId = language.Id;
            configure?.Invoke(locale, locale.LanguageId);
            locales.Add(locale);
        }
    }
}
