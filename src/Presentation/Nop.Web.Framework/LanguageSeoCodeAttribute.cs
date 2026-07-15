using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Services.Localization;
using Nop.Web.Framework.Localization;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Attribute which ensures that store URL contains a language SEO code if "SEO friendly URLs with multiple languages" setting is enabled
    /// </summary>
    public class LanguageSeoCodeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            var request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            //only GET requests
            if (!String.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var localizationSettings = filterContext.HttpContext.RequestServices.GetService<LocalizationSettings>();
            if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                return;

            //process current URL
            var pageUrl = request.Path.Value + request.QueryString.Value;
            string applicationPath = request.PathBase.Value ?? "/";
            if (pageUrl.IsLocalizedUrl(applicationPath, true))
            {
                //already localized URL
                //let's ensure that this language exists
                var seoCode = pageUrl.GetLanguageSeoCodeFromUrl(applicationPath, true);

                var languageService = filterContext.HttpContext.RequestServices.GetService<ILanguageService>();
                var language = languageService.GetAllLanguages()
                    .FirstOrDefault(l => seoCode.Equals(l.UniqueSeoCode, StringComparison.InvariantCultureIgnoreCase));
                if (language != null && language.Published)
                {
                    //exists
                    return;
                }
                else
                {
                    //doesn't exist. redirect to the original page (not permanent)
                    pageUrl = pageUrl.RemoveLanguageSeoCodeFromRawUrl(applicationPath);
                    filterContext.Result = new RedirectResult(pageUrl);
                }
            }
            //add language code to URL
            var workContext = filterContext.HttpContext.RequestServices.GetService<IWorkContext>();
            pageUrl = pageUrl.AddLanguageSeoCodeToRawUrl(applicationPath, workContext.WorkingLanguage);
            //301 (permanent) redirection
            filterContext.Result = new RedirectResult(pageUrl, true);
        }
    }
}
