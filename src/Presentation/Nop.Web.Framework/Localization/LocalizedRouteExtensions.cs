using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Registers a <b>localizable</b> controller route — the endpoint-routing replacement for
    /// 3.90's <c>RouteCollection.MapLocalizedRoute</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 6.4. Each overload maps onto
    /// <c>Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapControllerRoute</c>
    /// and then attaches a <see cref="LocalizedRoute"/> instance to the endpoint's metadata,
    /// which is what <see cref="LanguageSeoCodeAttribute"/> tests for (runtime deferral 24).
    /// </para>
    /// <para>
    /// <b>The argument lists are deliberately unchanged</b>, including the
    /// <c>string[] namespaces</c> overloads, so that the 14 <c>IRouteProvider</c>
    /// implementations in <c>Nop.Web</c> and the plugins need only change the parameter type
    /// of <c>RegisterRoutes</c> and their <c>using</c> directives. Two arguments no longer do
    /// anything and are documented as such:
    /// <list type="bullet">
    /// <item><c>namespaces</c> — MVC 5 used it to restrict controller lookup to a namespace
    /// (it became <c>DataTokens["Namespaces"]</c>). ASP.NET Core resolves controllers from
    /// application parts and has no namespace filter. <b>Ignored.</b></item>
    /// <item>an empty or null <c>name</c> — 3.90's
    /// <c>BackwardCompatibility2XRouteProvider</c> registers five routes named <c>""</c>.
    /// Endpoint route names must be unique, so an empty name is normalised to <c>null</c>
    /// (an unnamed route), which is what 3.90 effectively had.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>The return type changed</b> from <c>System.Web.Routing.Route</c> to
    /// <see cref="IEndpointConventionBuilder"/>. No in-tree caller uses the return value.
    /// It is returned rather than discarded because callers may need to attach endpoint
    /// metadata — for example <c>SuppressMatchingMetadata</c> on a route that exists only for
    /// URL generation, or <c>WithOrder(int)</c>. Note that endpoint routing resolves
    /// equal-precedence candidates by <b>Order</b>, and <c>MapControllerRoute</c> assigns an
    /// auto-incrementing order per call, so registration sequence already reproduces MVC 5's
    /// "first registered wins" — see
    /// <see cref="Nop.Web.Framework.Seo.GenericPathRouteExtensions"/>, whose remarks task 7.3
    /// corrected after measuring the actual behaviour.
    /// </para>
    /// <para>
    /// Note also that <c>System.Web.Mvc.UrlParameter.Optional</c> has no counterpart: an
    /// optional segment must be expressed in the pattern as <c>{SeName?}</c>.
    /// </para>
    /// </remarks>
    public static class LocalizedRouteExtensions
    {
        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder routeBuilder,
            string name, string pattern)
        {
            return MapLocalizedRoute(routeBuilder, name, pattern, null /* defaults */, (object)null /* constraints */);
        }

        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder routeBuilder,
            string name, string pattern, object defaults)
        {
            return MapLocalizedRoute(routeBuilder, name, pattern, defaults, (object)null /* constraints */);
        }

        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder routeBuilder,
            string name, string pattern, object defaults, object constraints)
        {
            return MapLocalizedRoute(routeBuilder, name, pattern, defaults, constraints, null /* namespaces */);
        }

        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder routeBuilder,
            string name, string pattern, string[] namespaces)
        {
            return MapLocalizedRoute(routeBuilder, name, pattern, null /* defaults */, null /* constraints */, namespaces);
        }

        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder routeBuilder,
            string name, string pattern, object defaults, string[] namespaces)
        {
            return MapLocalizedRoute(routeBuilder, name, pattern, defaults, null /* constraints */, namespaces);
        }

        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder routeBuilder,
            string name, string pattern, object defaults, object constraints, string[] namespaces)
        {
            if (routeBuilder == null)
                throw new ArgumentNullException("routeBuilder");
            if (pattern == null)
                throw new ArgumentNullException("pattern");

            //"namespaces" has no ASP.NET Core counterpart - see the class remarks.

            var builder = routeBuilder.MapControllerRoute(
                name: string.IsNullOrEmpty(name) ? null : name,
                pattern: pattern,
                defaults: defaults,
                constraints: constraints);

            //this is what makes the endpoint "localizable" (runtime deferral 24)
            builder.WithMetadata(new LocalizedRoute());

            return builder;
        }

        /// <summary>
        /// Retained for source compatibility. 3.90 walked <c>RouteTable.Routes</c> clearing a
        /// per-route cached copy of <c>SeoFriendlyUrlsForLanguagesEnabled</c>; there are no
        /// route instances and no such cache any more, so this is a no-op.
        /// </summary>
        /// <remarks>
        /// <b>Task 8.x action:</b> <c>Nop.Web/Administration/Controllers/SettingController.cs</c>
        /// line ~2024 calls
        /// <c>System.Web.Routing.RouteTable.Routes.ClearSeoFriendlyUrlsCachedValueForRoutes()</c>.
        /// Replace it with <see cref="LocalizedRoute.ClearSeoFriendlyUrlsCachedValue"/> or
        /// delete the call outright — both are equivalent now.
        /// </remarks>
        public static void ClearSeoFriendlyUrlsCachedValueForRoutes(this IEndpointRouteBuilder routeBuilder)
        {
            LocalizedRoute.ClearSeoFriendlyUrlsCachedValue();
        }
    }
}
