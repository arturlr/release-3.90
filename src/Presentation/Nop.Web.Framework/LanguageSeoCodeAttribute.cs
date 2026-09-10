using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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
    /// <remarks>
    /// Task 6.2: ported to ASP.NET Core MVC filters.
    /// <list type="bullet">
    /// <item><c>HttpRequestBase.HttpMethod</c> -&gt; <c>HttpRequest.Method</c>.</item>
    /// <item><c>HttpRequestBase.RawUrl</c> -&gt; <c>UriHelper.GetEncodedPathAndQuery()</c>,
    /// which yields <c>PathBase + Path + QueryString</c> - the same shape <c>RawUrl</c>
    /// produced.</item>
    /// <item><c>HttpRequestBase.ApplicationPath</c> -&gt; <c>HttpRequest.PathBase</c>, mapped to
    /// "/" when empty because <c>LocalizedUrlExtenstions</c> treats "/" as "not a virtual
    /// directory" and throws on an empty string.</item>
    /// <item><c>filterContext.RouteData.Route is LocalizedRoute</c> -&gt; ASP.NET Core's
    /// <c>RouteData</c> exposes no single <c>Route</c>; the equivalent test is for
    /// <see cref="LocalizedRoute"/> in the matched endpoint's metadata. SEE THE RUNTIME
    /// DEFERRAL NOTE BELOW - this needs task 6.4 to attach that metadata.</item>
    /// <item>the <c>IsChildAction</c> guard is removed (View Components do not execute
    /// action filters).</item>
    /// </list>
    /// RUNTIME DEFERRAL for task 6.4 (routing): the "is this route localizable?" test now reads
    /// endpoint metadata. When 6.4 reimplements <see cref="LocalizedRoute"/> over endpoint
    /// routing it MUST add a <see cref="LocalizedRoute"/> instance (or, if the class is retired,
    /// an equivalent marker type - and then update the <c>GetMetadata</c> call below) to the
    /// metadata of every localizable endpoint. Until that happens this filter finds no metadata
    /// and returns early, i.e. SEO language codes are never injected into or validated on URLs.
    /// It fails open with respect to redirects only - no authorization decision depends on it.
    /// </remarks>
    public class LanguageSeoCodeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            HttpRequest request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            //only GET requests
            if (!String.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var localizationSettings = EngineContext.Current.Resolve<LocalizationSettings>();
            if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                return;

            //ensure that this route is registered and localizable (LocalizedRoute in RouteProvider.cs)
            var endpoint = filterContext.HttpContext.GetEndpoint();
            if (endpoint == null || endpoint.Metadata.GetMetadata<LocalizedRoute>() == null)
                return;


            //process current URL
            var pageUrl = request.GetEncodedPathAndQuery();
            string applicationPath = request.PathBase.HasValue ? request.PathBase.Value : "/";
            if (pageUrl.IsLocalizedUrl(applicationPath, true))
            {
                //already localized URL
                //let's ensure that this language exists
                var seoCode = pageUrl.GetLanguageSeoCodeFromUrl(applicationPath, true);
                
                var languageService = EngineContext.Current.Resolve<ILanguageService>();
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
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            pageUrl = pageUrl.AddLanguageSeoCodeToRawUrl(applicationPath, workContext.WorkingLanguage);
            //301 (permanent) redirection
            filterContext.Result = new RedirectResult(pageUrl, true);
        }
    }
}
