using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Registers the SEO-slug (generic path) route — the endpoint-routing replacement for
    /// 3.90's <c>RouteCollection.MapGenericPathRoute</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 6.4. 3.90 created a <c>GenericPathRoute</c> (a <c>System.Web.Routing.Route</c>
    /// subclass) with an <c>MvcRouteHandler</c>. The ASP.NET Core equivalent is
    /// <c>MapDynamicControllerRoute&lt;<see cref="SlugRouteTransformer"/>&gt;</c>, which pairs
    /// the URL pattern with a <c>DynamicRouteValueTransformer</c> that performs the
    /// database-driven resolution.
    /// </para>
    /// <para>
    /// <b>Two signature consequences, both verified against the net10.0 reference assemblies:</b>
    /// <list type="number">
    /// <item><c>MapDynamicControllerRoute&lt;T&gt;</c> returns <c>void</c>, so these methods
    /// return <c>void</c> where 3.90 returned <c>Route</c>. The single in-tree caller
    /// (<c>Nop.Web/Infrastructure/GenericUrlRouteProvider.cs</c>) discards the result, so
    /// nothing breaks.</item>
    /// <item>a dynamic route cannot carry endpoint metadata, so the "this route is
    /// localizable" marker is set at request time by the transformer instead — see
    /// <c>LocalizedRoute.IsLocalizableRequest</c>.</item>
    /// </list>
    /// The <c>defaults</c>, <c>constraints</c> and <c>namespaces</c> arguments are accepted for
    /// source compatibility and <b>ignored</b>: a dynamic controller route derives its
    /// controller/action from the transformer's output, not from route defaults, which is
    /// exactly how 3.90 behaved too (<c>GenericPathRoute.GetRouteData</c> overwrote
    /// <c>controller</c>/<c>action</c> unconditionally). <c>namespaces</c> has no ASP.NET Core
    /// counterpart at all.
    /// </para>
    /// <para>
    /// <b>REQUIRED ACTION FOR TASK 7.3 — endpoint ambiguity.</b>
    /// <c>GenericUrlRouteProvider</c> registers this route with the pattern
    /// <c>"{generic_se_name}"</c> and then registers seven <c>MapLocalizedRoute</c> routes
    /// (<c>Product</c>, <c>Category</c>, <c>Manufacturer</c>, <c>Vendor</c>, <c>NewsItem</c>,
    /// <c>BlogPost</c>, <c>Topic</c>) that all share the pattern <c>"{SeName}"</c>. Those
    /// exist purely so views can generate URLs <i>by route name</i>; in MVC 5 they were never
    /// matched because <c>RouteCollection</c> matching stopped at the first candidate.
    /// Endpoint routing has no such rule: eight single-segment patterns of identical
    /// precedence and identical order cause <c>AmbiguousMatchException</c> at request time.
    /// Task 7.3 MUST disambiguate, e.g. by giving each of the seven a losing order:
    /// <code>
    /// routeBuilder.MapLocalizedRoute("Product", "{SeName}", new { controller = "Product", action = "ProductDetails" })
    ///             .WithOrder(1000);
    /// </code>
    /// (<c>WithOrder</c> is <c>Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithOrder</c>;
    /// it is why <see cref="Nop.Web.Framework.Localization.LocalizedRouteExtensions"/> returns
    /// the convention builder.) The generic-path route keeps the default order 0 and therefore
    /// wins, reproducing 3.90's effective behaviour.
    /// </para>
    /// </remarks>
    public static class GenericPathRouteExtensions
    {
        public static void MapGenericPathRoute(this IEndpointRouteBuilder routeBuilder, string name, string pattern)
        {
            MapGenericPathRoute(routeBuilder, name, pattern, null /* defaults */, (object)null /* constraints */);
        }

        public static void MapGenericPathRoute(this IEndpointRouteBuilder routeBuilder, string name, string pattern, object defaults)
        {
            MapGenericPathRoute(routeBuilder, name, pattern, defaults, (object)null /* constraints */);
        }

        public static void MapGenericPathRoute(this IEndpointRouteBuilder routeBuilder, string name, string pattern, object defaults, object constraints)
        {
            MapGenericPathRoute(routeBuilder, name, pattern, defaults, constraints, null /* namespaces */);
        }

        public static void MapGenericPathRoute(this IEndpointRouteBuilder routeBuilder, string name, string pattern, string[] namespaces)
        {
            MapGenericPathRoute(routeBuilder, name, pattern, null /* defaults */, null /* constraints */, namespaces);
        }

        public static void MapGenericPathRoute(this IEndpointRouteBuilder routeBuilder, string name, string pattern, object defaults, string[] namespaces)
        {
            MapGenericPathRoute(routeBuilder, name, pattern, defaults, null /* constraints */, namespaces);
        }

        public static void MapGenericPathRoute(this IEndpointRouteBuilder routeBuilder, string name, string pattern, object defaults, object constraints, string[] namespaces)
        {
            if (routeBuilder == null)
                throw new ArgumentNullException("routeBuilder");
            if (pattern == null)
                throw new ArgumentNullException("pattern");

            //"name", "defaults", "constraints" and "namespaces" are ignored - see the class remarks.
            routeBuilder.MapDynamicControllerRoute<SlugRouteTransformer>(pattern);
        }
    }
}
