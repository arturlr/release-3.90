using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Shipping.FixedOrByWeight
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Shipping.FixedOrByWeight.Configure",
                 "Plugins/FixedOrByWeight/Configure",
                 new { controller = "FixedOrByWeight", action = "Configure", });

            endpointRouteBuilder.MapControllerRoute("Plugin.Shipping.FixedOrByWeight.AddRateByWeighPopup",
                 "Plugins/FixedOrByWeight/AddRateByWeighPopup",
                 new { controller = "FixedOrByWeight", action = "AddRateByWeighPopup" });

            endpointRouteBuilder.MapControllerRoute("Plugin.Shipping.FixedOrByWeight.EditRateByWeighPopup",
                 "Plugins/FixedOrByWeight/EditRateByWeighPopup",
                 new { controller = "FixedOrByWeight", action = "EditRateByWeighPopup" });
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
