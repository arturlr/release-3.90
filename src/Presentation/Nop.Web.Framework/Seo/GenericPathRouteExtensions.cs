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
    /// <b>ENDPOINT AMBIGUITY — the original guidance here was WRONG, and task 7.3 corrected it.</b>
    /// <c>GenericUrlRouteProvider</c> registers this route with the pattern
    /// <c>"{generic_se_name}"</c> and then registers seven <c>MapLocalizedRoute</c> routes
    /// (<c>Product</c>, <c>Category</c>, <c>Manufacturer</c>, <c>Vendor</c>, <c>NewsItem</c>,
    /// <c>BlogPost</c>, <c>Topic</c>) that all share the pattern <c>"{SeName}"</c>. Those exist
    /// purely so views can generate URLs <i>by route name</i>; in MVC 5 they were never matched
    /// because <c>RouteCollection</c> matching stopped at the first candidate.
    /// </para>
    /// <para>
    /// This comment previously advised giving the seven a losing order with
    /// <c>.WithOrder(1000)</c>. <b>Do not do that.</b> A live probe against net10.0 (task 7.3,
    /// runtime-deferrals.md §28.1) established two facts:
    /// <list type="number">
    /// <item><c>MapControllerRoute</c> does <b>not</b> leave <c>Endpoint.Order</c> at 0 — MVC's
    /// <c>ControllerActionEndpointConventionBuilder</c> assigns an <b>auto-incrementing order per
    /// call</b>, and <c>EndpointComparer</c> compares <c>Order</c> <i>before</i> precedence. So
    /// registration sequence already decides the winner, faithfully reproducing MVC 5, and these
    /// eight patterns do not collide by default.</item>
    /// <item>Applying <c>.WithOrder(1000)</c> to all seven <b>creates</b> the ambiguity it was
    /// meant to prevent: it collapses seven distinct orders into one, and the probe then
    /// reproduced <c>AmbiguousMatchException: The request matched multiple endpoints</c>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>What task 7.3 applied instead</b>, and what a plugin registering a similarly-shaped
    /// route should copy: attach <see cref="Microsoft.AspNetCore.Routing.SuppressMatchingMetadata"/>,
    /// which removes the endpoint from <i>inbound matching</i> while leaving it usable for
    /// <i>link generation</i> — the exact semantic a "URL generation only" route has:
    /// <code>
    /// routeBuilder.MapLocalizedRoute("Product", "{SeName}", new { controller = "Product", action = "ProductDetails" })
    ///             .WithMetadata(new SuppressMatchingMetadata());
    /// </code>
    /// Verified live: <c>Url.RouteUrl("Product", new { SeName = "my-slug" })</c> still returns
    /// <c>/my-slug</c> with matching suppressed, because <c>IUrlHelper.RouteUrl</c> resolves
    /// through <c>RouteValuesAddressScheme</c>, which skips only
    /// <c>ISuppressLinkGenerationMetadata</c>.
    /// </para>
    /// <para>
    /// <b>Also load-bearing:</b> because <c>Order</c> beats precedence, this provider's
    /// <c>Priority</c> of <c>-1000000</c> (so <c>IRoutePublisher</c> registers it last) is what
    /// stops <c>{generic_se_name}</c> swallowing every single-segment path. The probe confirmed
    /// that registering the slug route first makes <c>/cart</c> resolve to
    /// <c>Common/GenericUrl</c> instead of the <c>"cart/"</c> route, even though the literal
    /// pattern has strictly better precedence. Do not reorder the providers.
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
