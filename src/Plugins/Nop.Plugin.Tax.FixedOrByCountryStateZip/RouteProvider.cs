using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Tax.FixedOrByCountryStateZip.AddRateByCountryStateZip",
                pattern: "Plugins/FixedOrByCountryStateZip/AddRateByCountryStateZip",
                defaults: new { controller = "FixedOrByCountryStateZip", action = "AddRateByCountryStateZip" }
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
