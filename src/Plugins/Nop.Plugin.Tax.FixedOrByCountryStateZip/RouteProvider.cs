using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Tax.FixedOrByCountryStateZip.AddRateByCountryStateZip",
                 "Plugins/FixedOrByCountryStateZip/AddRateByCountryStateZip",
                 new { controller = "FixedOrByCountryStateZip", action = "AddRateByCountryStateZip" });
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