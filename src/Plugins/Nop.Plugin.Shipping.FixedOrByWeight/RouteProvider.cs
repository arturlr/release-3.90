using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Shipping.FixedOrByWeight
{
    /// <summary>
    /// Registers this plugin's three admin routes.
    /// </summary>
    /// <remarks>
    /// Task 14.4. The port is the mechanical edit runtime-deferrals.md §17.4a / deferral 83.6
    /// describe, applied three times: <c>RouteCollection</c> → <c>IEndpointRouteBuilder</c>,
    /// <c>MapRoute</c> → <c>MapControllerRoute</c>, and the <c>string[] namespaces</c> argument
    /// dropped (no ASP.NET Core counterpart — controller discovery is application-part based).
    /// Route names, URL patterns and defaults are 3.90's, unchanged.
    ///
    /// The <c>Configure</c> route name is load-bearing: <c>_ByWeight.cshtml</c> generates URLs
    /// with <c>Url.RouteUrl("Plugin.Shipping.FixedOrByWeight.Configure")</c> and the popup views'
    /// grid buttons call <c>Url.RouteUrl("Plugin.Shipping.FixedOrByWeight.EditRateByWeighPopup")</c>
    /// / <c>...AddRateByWeighPopup</c>, so renaming any would break URL generation at runtime with
    /// no compile error. All three patterns are fully literal, so none can tie on precedence and
    /// §17.4a gotcha 3's <c>AmbiguousMatchException</c> cannot arise. <c>Priority</c> stays 3.90's 0.
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.Shipping.FixedOrByWeight.Configure",
                 "Plugins/FixedOrByWeight/Configure",
                 new { controller = "FixedOrByWeight", action = "Configure" }
            );

            routeBuilder.MapControllerRoute("Plugin.Shipping.FixedOrByWeight.AddRateByWeighPopup",
                 "Plugins/FixedOrByWeight/AddRateByWeighPopup",
                 new { controller = "FixedOrByWeight", action = "AddRateByWeighPopup" }
            );

            routeBuilder.MapControllerRoute("Plugin.Shipping.FixedOrByWeight.EditRateByWeighPopup",
                 "Plugins/FixedOrByWeight/EditRateByWeighPopup",
                 new { controller = "FixedOrByWeight", action = "EditRateByWeighPopup" }
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
