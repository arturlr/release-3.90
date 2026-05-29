using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Localization;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Attribute which ensures that store URL contains a language SEO code if "SEO friendly URLs with multiple languages" setting is enabled
    /// </summary>
    public class LanguageSeoCodeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null || context.HttpContext == null)
                return;

            var request = context.HttpContext.Request;
            if (request == null)
                return;

            //only GET requests
            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var localizationSettings = EngineContext.Current.Resolve<LocalizationSettings>();
            if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                return;

            //process current URL
            var pageUrl = request.Path.Value + request.QueryString.Value;
            string applicationPath = request.PathBase.Value ?? string.Empty;
            if (pageUrl.IsLocalizedUrl(applicationPath, true))
            {
                //already localized URL
                var seoCode = pageUrl.GetLanguageSeoCodeFromUrl(applicationPath, true);

                var languageService = EngineContext.Current.Resolve<ILanguageService>();
                var language = languageService.GetAllLanguages()
                    .FirstOrDefault(l => seoCode.Equals(l.UniqueSeoCode, StringComparison.InvariantCultureIgnoreCase));
                if (language != null && language.Published)
                {
                    return;
                }
                else
                {
                    pageUrl = pageUrl.RemoveLanguageSeoCodeFromRawUrl(applicationPath);
                    context.Result = new RedirectResult(pageUrl);
                }
            }
            //add language code to URL
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            pageUrl = pageUrl.AddLanguageSeoCodeToRawUrl(applicationPath, workContext.WorkingLanguage.UniqueSeoCode);
            //301 (permanent) redirection
            context.Result = new RedirectResult(pageUrl, true);
        }
    }
}
