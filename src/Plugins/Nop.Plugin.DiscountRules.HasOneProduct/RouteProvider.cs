using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.DiscountRules.HasOneProduct
{
    /// <summary>
    /// Registers this plugin's four admin routes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 10.2. The port is the mechanical edit runtime-deferrals.md §17.4a describes, applied
    /// four times: <c>RouteCollection</c> → <c>IEndpointRouteBuilder</c>,
    /// <c>MapRoute</c> → <c>MapControllerRoute</c>, and the <c>string[] namespaces</c> argument
    /// dropped (no ASP.NET Core counterpart — controller discovery is application-part based).
    /// Route names, URL patterns and defaults are 3.90's, unchanged.
    /// </para>
    /// <para>
    /// <b>The route names matter here, unlike in 10.1.</b> Three of these four are used for URL
    /// GENERATION from the views: <c>Configure.cshtml</c> calls
    /// <c>Url.RouteUrl("Plugin.DiscountRules.HasOneProduct.ProductAddPopup", …)</c> and
    /// <c>Url.RouteUrl("Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames")</c>, and
    /// <c>ProductAddPopup.cshtml</c> calls
    /// <c>Url.RouteUrl("Plugin.DiscountRules.HasOneProduct.ProductAddPopupList")</c>. Endpoint
    /// route names must be unique and are what <c>LinkGenerator</c> resolves, so renaming any
    /// of them would break URL generation at runtime with no compile error.
    /// </para>
    /// <para>
    /// All four patterns are fully literal, so none can tie on precedence with another and
    /// §17.4a gotcha 3's <c>AmbiguousMatchException</c> cannot arise. <c>Priority</c> stays
    /// 3.90's 0 — see the note in the 10.1 provider.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Configure",
                 "Plugins/DiscountRulesHasOneProduct/Configure",
                 new { controller = "DiscountRulesHasOneProduct", action = "Configure" }
            );
            routeBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopup",
                 "Plugins/DiscountRulesHasOneProduct/ProductAddPopup",
                 new { controller = "DiscountRulesHasOneProduct", action = "ProductAddPopup" }
            );
            routeBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopupList",
                 "Plugins/DiscountRulesHasOneProduct/ProductAddPopupList",
                 new { controller = "DiscountRulesHasOneProduct", action = "ProductAddPopupList" }
            );
            routeBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames",
                 "Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames",
                 new { controller = "DiscountRulesHasOneProduct", action = "LoadProductFriendlyNames" }
            );
        }

        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
