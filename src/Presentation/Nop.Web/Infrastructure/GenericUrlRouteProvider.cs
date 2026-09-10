using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routes;
using Nop.Web.Framework.Seo;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// Registers the SEO-slug route and the seven "name only" routes that exist purely so
    /// views can generate slug URLs by route name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Task 7.3 — the endpoint-ambiguity question, measured rather than assumed.</b>
    /// <c>GenericPathRouteExtensions</c>'s remarks warned that the eight single-segment
    /// patterns registered here (<c>{generic_se_name}</c> plus seven <c>{SeName}</c>) are of
    /// identical precedence and would therefore raise
    /// <c>AmbiguousMatchException</c>, and recommended <c>.WithOrder(1000)</c> on the seven.
    /// A live probe against net10.0 showed that recommendation is <b>backwards</b>:
    /// </para>
    /// <list type="number">
    /// <item><c>MapControllerRoute</c> does <b>not</b> leave <c>Endpoint.Order</c> at 0. MVC's
    /// <c>ControllerActionEndpointConventionBuilder</c> assigns an <b>auto-incrementing order
    /// per call</b> (1, 2, 3, …), and <c>EndpointComparer</c> compares <c>Order</c>
    /// <i>before</i> precedence. Registration sequence therefore already decides the winner —
    /// which is a faithful reproduction of MVC 5's "<c>RouteCollection</c> stops at the first
    /// match", and is why the eight patterns here do not collide by default.</item>
    /// <item>Applying <c>.WithOrder(1000)</c> to all seven <b>creates</b> the ambiguity it was
    /// meant to prevent: it collapses seven distinct auto-assigned orders into one, and the
    /// probe then reproduced <c>AmbiguousMatchException: The request matched multiple
    /// endpoints</c> for a single-segment path. So the literal advice is not applied.</item>
    /// </list>
    /// <para>
    /// <b>What is applied instead:</b> each of the seven carries
    /// <see cref="SuppressMatchingMetadata"/>, which removes it from <i>inbound matching</i>
    /// while leaving it fully usable for <i>link generation</i>. That is the exact semantic
    /// these routes have always had — in 3.90 they were never matched, because
    /// <c>GenericPathRoute</c> either resolved the slug itself or aborted the request with
    /// <c>Response.End()</c>, so <c>RouteCollection</c> never reached them. Relying on the
    /// auto-assigned order instead would work today only by coincidence, and would break
    /// silently if a plugin ever registered another <c>{SeName}</c>-shaped route or the
    /// provider ordering changed.
    /// </para>
    /// <para>
    /// <b>Verified live, not reasoned about:</b> with <c>SuppressMatchingMetadata</c> applied,
    /// <c>Url.RouteUrl("Product", new { SeName = "my-slug" })</c> — the call shape the views
    /// use — still returns <c>/my-slug</c> for all seven names.
    /// <c>IUrlHelper.RouteUrl</c> resolves through <c>RouteValuesAddressScheme</c>, which
    /// skips only <c>ISuppressLinkGenerationMetadata</c>, not
    /// <c>ISuppressMatchingMetadata</c>.
    /// </para>
    /// <para>
    /// <b>Ordering still matters for the generic route itself</b> and is preserved by
    /// <see cref="Priority"/>: this provider stays at <c>-1000000</c> so
    /// <c>IRoutePublisher</c> registers it last, which gives <c>{generic_se_name}</c> a worse
    /// <c>Order</c> than every route registered by <c>RouteProvider</c>. Without that,
    /// <c>{generic_se_name}</c> would swallow every single-segment path — the probe confirmed
    /// that too: registering the slug route first made <c>/cart</c> resolve to
    /// <c>Common/GenericUrl</c> instead of the <c>"cart/"</c> route, even though the literal
    /// pattern has strictly better precedence.
    /// </para>
    /// </remarks>
    public partial class GenericUrlRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routes)
        {
            //generic URLs
            routes.MapGenericPathRoute("GenericUrl",
                                       "{generic_se_name}",
                                       new {controller = "Common", action = "GenericUrl"});

            //define this routes to use in UI views (in case if you want to customize some of them later)
            //NOTE (task 7.3): these are URL-GENERATION ONLY - see the class remarks. Each is
            //suppressed from inbound matching, which is what 3.90 achieved implicitly.
            routes.MapLocalizedRoute("Product",
                                     "{SeName}",
                                     new { controller = "Product", action = "ProductDetails" })
                  .WithMetadata(new SuppressMatchingMetadata());

            routes.MapLocalizedRoute("Category",
                            "{SeName}",
                            new { controller = "Catalog", action = "Category" })
                  .WithMetadata(new SuppressMatchingMetadata());

            routes.MapLocalizedRoute("Manufacturer",
                            "{SeName}",
                            new { controller = "Catalog", action = "Manufacturer" })
                  .WithMetadata(new SuppressMatchingMetadata());

            routes.MapLocalizedRoute("Vendor",
                            "{SeName}",
                            new { controller = "Catalog", action = "Vendor" })
                  .WithMetadata(new SuppressMatchingMetadata());

            routes.MapLocalizedRoute("NewsItem",
                            "{SeName}",
                            new { controller = "News", action = "NewsItem" })
                  .WithMetadata(new SuppressMatchingMetadata());

            routes.MapLocalizedRoute("BlogPost",
                            "{SeName}",
                            new { controller = "Blog", action = "BlogPost" })
                  .WithMetadata(new SuppressMatchingMetadata());

            routes.MapLocalizedRoute("Topic",
                            "{SeName}",
                            new { controller = "Topic", action = "TopicDetails" })
                  .WithMetadata(new SuppressMatchingMetadata());



            //the last route. it's used when none of registered routes could be used for the current request
            //but in this case we cannot process non-registered routes (/controller/action)
            //routes.MapLocalizedRoute(
            //    "PageNotFound-Wildchar",
            //    "{*url}",
            //    new { controller = "Common", action = "PageNotFound" });
        }

        public int Priority
        {
            get
            {
                //it should be the last route
                //we do not set it to -int.MaxValue so it could be overridden (if required)
                return -1000000;
            }
        }
    }
}
