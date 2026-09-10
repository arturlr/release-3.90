using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Events;
using Nop.Services.Seo;
using Nop.Web.Framework.Localization;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Resolves a nopCommerce SEO slug (<c>UrlRecord</c>) to the controller/action that serves
    /// it — <c>/some-product-slug</c> → <c>Product/ProductDetails/17</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>REDESIGN — task 6.4.</b> This replaces 3.90's <c>Seo/GenericPathRoute.cs</c>, a
    /// <c>LocalizedRoute</c> (hence <c>System.Web.Routing.Route</c>) subclass that overrode
    /// <c>GetRouteData</c> and mutated <c>RouteData.Values</c> after a database lookup.
    /// ASP.NET Core's counterpart for database-driven route resolution is a
    /// <see cref="DynamicRouteValueTransformer"/> registered with
    /// <c>MapDynamicControllerRoute&lt;TTransformer&gt;</c> — verified present in the net10.0
    /// reference assemblies. The transformer receives the values matched by the pattern
    /// (<c>{generic_se_name}</c>) and returns the values MVC should use to select an action.
    /// </para>
    /// <para>
    /// <b>Every branch of the 3.90 logic is preserved</b>: the cached
    /// <c>GetBySlugCached</c> lookup and the commented-out non-cached alternative; the
    /// "no URL record" → <c>Common/PageNotFound</c> fallback; the inactive-record →
    /// <c>GetActiveSlug</c> → 301 redirect; the wrong-language-slug → 302 redirect; the seven
    /// entity-name cases with their exact route-value names; and the
    /// <see cref="CustomUrlRecordEntityNameRequested"/> event for unknown entity names.
    /// </para>
    /// <para>
    /// <b>How the two redirects work now.</b> 3.90 wrote <c>Response.Status</c> /
    /// <c>Response.RedirectLocation</c> and called <c>Response.End()</c> from inside route
    /// matching, then returned <c>null</c>. A transformer cannot terminate the pipeline, and
    /// writing a status code and returning <c>null</c> would be clobbered by ASP.NET Core's
    /// terminal 404 middleware. Instead the transformer records the target and status on
    /// <see cref="HttpContext.Items"/> and returns <c>null</c> (no endpoint), and
    /// <see cref="SlugRedirectMiddleware"/> — registered immediately after
    /// <c>UseRouting()</c>, so it runs whether or not an endpoint matched — issues the
    /// redirect and short-circuits. Same status codes, same <c>Location</c>, same
    /// "nothing else runs" outcome.
    /// </para>
    /// <para>
    /// <b>Registration.</b> The type must be resolvable per request; it is registered in
    /// <c>DependencyRegistrar</c> with <c>InstancePerDependency()</c> (the transient lifetime
    /// ASP.NET Core requires for a transformer) and is resolved from
    /// <c>HttpContext.RequestServices</c>, which is Autofac-backed once the
    /// <c>AutofacServiceProviderFactory</c> integration is in place.
    /// </para>
    /// <para>
    /// <b>Localizability.</b> <c>GenericPathRoute</c> derived from <c>LocalizedRoute</c>, so
    /// slug URLs were localizable. <c>MapDynamicControllerRoute</c> returns <c>void</c> and
    /// gives no way to attach endpoint metadata, so the transformer flags the request through
    /// <see cref="LocalizedRoute.LocalizableRequestItemKey"/> instead;
    /// <see cref="LocalizedRoute.IsLocalizableRequest"/> checks both sources.
    /// </para>
    /// </remarks>
    public partial class SlugRouteTransformer : DynamicRouteValueTransformer
    {
        /// <summary>
        /// Route-value key holding the slug, matched by the <c>{generic_se_name}</c> pattern.
        /// Unchanged from 3.90.
        /// </summary>
        public const string SlugRouteValueKey = "generic_se_name";

        /// <summary>
        /// <see cref="HttpContext.Items"/> key under which a pending SEO redirect is parked
        /// for <see cref="SlugRedirectMiddleware"/>.
        /// </summary>
        public const string RedirectItemKey = "nop.SeoSlugRedirect";

        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext,
            RouteValueDictionary values)
        {
            return new ValueTask<RouteValueDictionary>(Transform(httpContext, values));
        }

        /// <summary>
        /// The synchronous body. <c>protected virtual</c> so a plugin can substitute slug
        /// resolution, mirroring the fact that 3.90's <c>GetRouteData</c> was overridable.
        /// </summary>
        protected virtual RouteValueDictionary Transform(HttpContext httpContext, RouteValueDictionary values)
        {
            if (values == null)
                return null;

            //mark the request as localizable - GenericPathRoute derived from LocalizedRoute
            if (httpContext != null)
                httpContext.Items[LocalizedRoute.LocalizableRequestItemKey] = true;

            var data = new RouteValueDictionary(values);

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return data;

            var urlRecordService = EngineContext.Current.Resolve<IUrlRecordService>();
            var slug = data.ContainsKey(SlugRouteValueKey) ? data[SlugRouteValueKey] as string : null;
            //performance optimization.
            //we load a cached verion here. it reduces number of SQL requests for each page load
            var urlRecord = urlRecordService.GetBySlugCached(slug);
            //comment the line above and uncomment the line below in order to disable this performance "workaround"
            //var urlRecord = urlRecordService.GetBySlug(slug);
            if (urlRecord == null)
            {
                //no URL record found
                data["controller"] = "Common";
                data["action"] = "PageNotFound";
                return data;
            }
            //ensure that URL record is active
            if (!urlRecord.IsActive)
            {
                //URL record is not active. let's find the latest one
                var activeSlug = urlRecordService.GetActiveSlug(urlRecord.EntityId, urlRecord.EntityName, urlRecord.LanguageId);
                if (string.IsNullOrWhiteSpace(activeSlug))
                {
                    //no active slug found
                    data["controller"] = "Common";
                    data["action"] = "PageNotFound";
                    return data;
                }

                //the active one is found
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                SetPendingRedirect(httpContext, string.Format("{0}{1}", webHelper.GetStoreLocation(), activeSlug), true);
                return null;
            }

            //ensure that the slug is the same for the current language
            //otherwise, it can cause some issues when customers choose a new language but a slug stays the same
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var slugForCurrentLanguage = SeoExtensions.GetSeName(urlRecord.EntityId, urlRecord.EntityName, workContext.WorkingLanguage.Id);
            if (!String.IsNullOrEmpty(slugForCurrentLanguage) &&
                !slugForCurrentLanguage.Equals(slug, StringComparison.InvariantCultureIgnoreCase))
            {
                //we should make not null or "" validation above because some entities does not have SeName for standard (ID=0) language (e.g. news, blog posts)
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                //302 Moved Temporarily
                SetPendingRedirect(httpContext, string.Format("{0}{1}", webHelper.GetStoreLocation(), slugForCurrentLanguage), false);
                return null;
            }

            //process URL
            switch (urlRecord.EntityName.ToLowerInvariant())
            {
                case "product":
                    {
                        data["controller"] = "Product";
                        data["action"] = "ProductDetails";
                        data["productid"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                case "category":
                    {
                        data["controller"] = "Catalog";
                        data["action"] = "Category";
                        data["categoryid"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                case "manufacturer":
                    {
                        data["controller"] = "Catalog";
                        data["action"] = "Manufacturer";
                        data["manufacturerid"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                case "vendor":
                    {
                        data["controller"] = "Catalog";
                        data["action"] = "Vendor";
                        data["vendorid"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                case "newsitem":
                    {
                        data["controller"] = "News";
                        data["action"] = "NewsItem";
                        data["newsItemId"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                case "blogpost":
                    {
                        data["controller"] = "Blog";
                        data["action"] = "BlogPost";
                        data["blogPostId"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                case "topic":
                    {
                        data["controller"] = "Topic";
                        data["action"] = "TopicDetails";
                        data["topicId"] = urlRecord.EntityId;
                        data["SeName"] = urlRecord.Slug;
                    }
                    break;
                default:
                    {
                        //no record found

                        //generate an event this way developers could insert their own types
                        EngineContext.Current.Resolve<IEventPublisher>()
                            .Publish(new CustomUrlRecordEntityNameRequested(data, urlRecord));
                    }
                    break;
            }

            return data;
        }

        /// <summary>
        /// Parks a redirect for <see cref="SlugRedirectMiddleware"/>. Replaces 3.90's
        /// <c>Response.Status</c> + <c>RedirectLocation</c> + <c>Response.End()</c>.
        /// </summary>
        protected virtual void SetPendingRedirect(HttpContext httpContext, string location, bool permanent)
        {
            if (httpContext == null || string.IsNullOrEmpty(location))
                return;

            httpContext.Items[RedirectItemKey] = new PendingRedirect(location, permanent);
        }

        /// <summary>
        /// A SEO redirect decided during route matching and executed by
        /// <see cref="SlugRedirectMiddleware"/>.
        /// </summary>
        public class PendingRedirect
        {
            public PendingRedirect(string location, bool permanent)
            {
                Location = location;
                Permanent = permanent;
            }

            /// <summary>Absolute URL to redirect to.</summary>
            public string Location { get; private set; }

            /// <summary>True for 301 Moved Permanently, false for 302 Moved Temporarily.</summary>
            public bool Permanent { get; private set; }
        }
    }
}
