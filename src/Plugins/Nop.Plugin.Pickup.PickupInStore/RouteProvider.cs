using Nop.Web.Framework.Mvc.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nop.Plugin.Pickup.PickupInStore
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Pickup.PickupInStore.Create",
                 "Plugins/PickupInStore/Create",
                 new { controller = "PickupInStore", action = "Create" }
            );

            endpointRouteBuilder.MapControllerRoute("Plugin.Pickup.PickupInStore.Edit",
                 "Plugins/PickupInStore/Edit",
                 new { controller = "PickupInStore", action = "Edit" }
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
