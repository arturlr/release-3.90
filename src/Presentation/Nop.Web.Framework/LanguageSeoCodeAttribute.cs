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
    /// RUNTIME DEFERRAL for task 6.4 (routing): RESOLVED by task 6.4. The "is this route
    /// localizable?" test now calls <see cref="LocalizedRoute.IsLocalizableRequest"/>, which
    /// reads the <see cref="LocalizedRoute"/> marker that
    /// <c>LocalizedRouteExtensions.MapLocalizedRoute</c> attaches to endpoint metadata, or the
    /// <c>HttpContext.Items</c> flag set by <c>SlugRouteTransformer</c> for the generic-path
    /// route. Note the SEO code is stripped out of <c>Request.Path</c> into
    /// <c>Request.PathBase</c> by <c>SeoFriendlyUrlsMiddleware</c> before routing, and
    /// <c>GetEncodedPathAndQuery()</c> yields <c>PathBase + Path + QueryString</c>, so the
    /// <c>pageUrl</c> read below still contains the code exactly as 3.90's <c>RawUrl</c> did.
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
            //Task 6.4: LocalizedRoute is now an endpoint-metadata marker attached by
            //MapLocalizedRoute, plus an HttpContext.Items flag for the generic-path (slug)
            //route, which cannot carry metadata because MapDynamicControllerRoute returns void.
            //LocalizedRoute.IsLocalizableRequest checks both. Runtime deferral 24 is CLOSED.
            if (!LocalizedRoute.IsLocalizableRequest(filterContext.HttpContext))
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
