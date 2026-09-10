using Microsoft.AspNetCore.Http;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Marks an endpoint as <b>localizable</b>: its URL may carry a language SEO code
    /// (<c>/en/category/foo</c>) when
    /// <see cref="LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled"/> is on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>REDESIGN — task 6.4.</b> In 3.90 this was a <c>System.Web.Routing.Route</c>
    /// subclass overriding <c>GetRouteData</c> and <c>GetVirtualPath</c>. ASP.NET Core has
    /// no <c>RouteBase</c>, no <c>IRouteHandler</c>, no <c>RouteCollection</c> and no
    /// per-route match/generate hooks, so the class could not be ported as a class. Its two
    /// behaviours were split onto the two mechanisms that own them in ASP.NET Core:
    /// <list type="bullet">
    /// <item><b>inbound</b> (<c>GetRouteData</c>'s <c>httpContext.RewritePath(...)</c>, which
    /// stripped the language segment so the ordinary patterns could match) →
    /// <see cref="SeoFriendlyUrlsMiddleware"/>, registered BEFORE <c>UseRouting()</c>;</item>
    /// <item><b>outbound</b> (<c>GetVirtualPath</c>, which prefixed the generated path with
    /// the language code) → achieved by that same middleware moving the stripped segment
    /// into <see cref="HttpRequest.PathBase"/>. ASP.NET Core prefixes <c>PathBase</c> onto
    /// every URL produced by <c>LinkGenerator</c> / <c>IUrlHelper</c> / <c>Url.Content</c>,
    /// so no link-generation hook is needed.</item>
    /// </list>
    /// What remains of the type is its <i>identity</i>: a marker placed in endpoint metadata
    /// by <see cref="LocalizedRouteExtensions.MapLocalizedRoute(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string, string)"/>,
    /// which is exactly what <see cref="LanguageSeoCodeAttribute"/> needs for its
    /// "is this route localizable?" test (runtime deferral 24). The class is kept — rather
    /// than replaced by a new marker name — so that the 6.2 filter code and any third-party
    /// <c>is LocalizedRoute</c>-style checks keep referring to the same type.
    /// </para>
    /// <para>
    /// <b>Behaviour preserved:</b> which URLs are recognised as localized, which language
    /// code is extracted, and the fact that generated URLs carry the code.
    /// <b>Behaviour changed:</b> in 3.90 only routes created through
    /// <c>MapLocalizedRoute</c>/<c>MapGenericPathRoute</c> prefixed generated URLs. With
    /// <c>PathBase</c> the prefix applies to <i>every</i> URL generated during a request that
    /// arrived on a localized URL — including non-localized routes such as
    /// <c>widgetsbyzone</c>. Those URLs still resolve, because the inbound middleware strips
    /// the segment from any request. Requests that arrive without a language segment (the
    /// admin area, and the public store when the setting is off) have an empty
    /// <c>PathBase</c> and are completely unaffected.
    /// </para>
    /// <para>
    /// <b>No per-route settings cache any more.</b> 3.90 cached
    /// <c>SeoFriendlyUrlsForLanguagesEnabled</c> on each route instance and
    /// <c>Nop.Web/Administration/Controllers/SettingController.cs</c> line ~2024 cleared it
    /// with <c>RouteTable.Routes.ClearSeoFriendlyUrlsCachedValueForRoutes()</c>. There are no
    /// route instances to hold such a cache; the middleware resolves
    /// <see cref="LocalizationSettings"/> per request, which is already served from
    /// nopCommerce's static settings cache (the same thing
    /// <see cref="LanguageSeoCodeAttribute"/> does). <see cref="ClearSeoFriendlyUrlsCachedValue"/>
    /// is retained as a no-op so the admin call site has something to call.
    /// </para>
    /// </remarks>
    public partial class LocalizedRoute
    {
        /// <summary>
        /// Key under which <see cref="SeoFriendlyUrlsMiddleware"/> stores the language SEO
        /// code it removed from the request path, and under which
        /// <see cref="Nop.Web.Framework.Seo.SlugRouteTransformer"/> records that the request
        /// was resolved through the (localizable) generic-path route.
        /// </summary>
        public const string LocalizableRequestItemKey = "nop.LocalizableRequest";

        /// <summary>
        /// Key under which <see cref="SeoFriendlyUrlsMiddleware"/> stores the language SEO
        /// code stripped from the incoming path, or <c>null</c> when there was none.
        /// </summary>
        public const string LanguageSeoCodeItemKey = "nop.LanguageSeoCode";

        /// <summary>
        /// Determines whether the endpoint matched for the current request is localizable.
        /// </summary>
        /// <remarks>
        /// Two sources, because ASP.NET Core's
        /// <c>MapDynamicControllerRoute&lt;TTransformer&gt;</c> returns <c>void</c> — verified
        /// against the net10.0 reference assemblies — so the generic-path (slug) route cannot
        /// be given endpoint metadata. It instead flags itself through
        /// <see cref="LocalizableRequestItemKey"/> from
        /// <see cref="Nop.Web.Framework.Seo.SlugRouteTransformer"/>, which runs during
        /// routing and therefore before any action filter.
        /// </remarks>
        public static bool IsLocalizableRequest(HttpContext httpContext)
        {
            if (httpContext == null)
                return false;

            var endpoint = httpContext.GetEndpoint();
            if (endpoint != null && endpoint.Metadata.GetMetadata<LocalizedRoute>() != null)
                return true;

            return httpContext.Items.ContainsKey(LocalizableRequestItemKey);
        }

        /// <summary>
        /// Whether SEO-friendly language URLs are enabled. Resolved per call from the
        /// (statically cached) settings, so there is nothing left to invalidate.
        /// </summary>
        public static bool SeoFriendlyUrlsForLanguagesEnabled
        {
            get
            {
                if (!DataSettingsHelper.DatabaseIsInstalled())
                    return false;

                try
                {
                    var settings = EngineContext.Current.Resolve<LocalizationSettings>();
                    return settings != null && settings.SeoFriendlyUrlsForLanguagesEnabled;
                }
                catch
                {
                    //the engine/container may not be usable yet (install mode, or a store
                    //whose settings cannot be loaded). 3.90's route-level cache would have
                    //thrown out of route matching here; failing closed keeps the request
                    //resolving from the non-localized patterns instead.
                    return false;
                }
            }
        }

        /// <summary>
        /// Retained for source compatibility with 3.90's admin settings screen. It is a
        /// no-op: see the class remarks.
        /// </summary>
        public static void ClearSeoFriendlyUrlsCachedValue()
        {
        }
    }
}
