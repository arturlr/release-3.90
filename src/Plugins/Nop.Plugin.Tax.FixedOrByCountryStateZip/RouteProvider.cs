using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip
{
    /// <summary>
    /// Registers this plugin's one admin route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 15.1. The mechanical port runtime-deferrals.md §17.4a describes:
    /// <c>RouteCollection</c> → <see cref="IEndpointRouteBuilder"/>, <c>MapRoute</c> →
    /// <c>MapControllerRoute</c>, and the <c>string[] namespaces</c> argument dropped (no ASP.NET
    /// Core counterpart — controller discovery is application-part based). Route name, URL pattern
    /// and defaults are 3.90's, unchanged.
    /// </para>
    /// <para>
    /// <b>The route name is load-bearing:</b> <c>Views/_CountryStateZip.cshtml</c> resolves it
    /// with <c>Url.RouteUrl("Plugin.Tax.FixedOrByCountryStateZip.AddRateByCountryStateZip")</c> to
    /// build the "Add tax rate" AJAX target, so a rename breaks that button with no compile error.
    /// The pattern is fully literal, so it cannot tie on precedence with another route and
    /// §17.4a gotcha 3's <c>AmbiguousMatchException</c> cannot arise. <c>Priority</c> stays
    /// 3.90's 0.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.Tax.FixedOrByCountryStateZip.AddRateByCountryStateZip",
                 "Plugins/FixedOrByCountryStateZip/AddRateByCountryStateZip",
                 new { controller = "FixedOrByCountryStateZip", action = "AddRateByCountryStateZip" }
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
