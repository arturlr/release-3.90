using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Pickup.PickupInStore
{
    /// <summary>
    /// Registers this plugin's admin Create/Edit routes.
    /// </summary>
    /// <remarks>
    /// Task 13.1 — the ASP.NET Core port of a plugin <c>IRouteProvider</c>, following the group-10
    /// worked example (<c>Nop.Plugin.DiscountRules.CustomerRoles/RouteProvider.cs</c>). The contract
    /// changed from <c>RegisterRoutes(System.Web.Routing.RouteCollection)</c> to
    /// <c>RegisterRoutes(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)</c>.
    ///
    /// <para>
    /// Both route <b>names</b> ("Plugin.Pickup.PickupInStore.Create" / ".Edit"), both URL patterns
    /// and both sets of defaults are UNCHANGED — the names are load-bearing here:
    /// <c>Views/Configure.cshtml</c> resolves these URLs by name through
    /// <c>Url.RouteUrl("Plugin.Pickup.PickupInStore.Create")</c> /
    /// <c>Url.RouteUrl("Plugin.Pickup.PickupInStore.Edit")</c>. Only the
    /// <c>new[] { "Nop.Plugin.Pickup.PickupInStore.Controllers" }</c> namespaces argument is dropped:
    /// it became <c>DataTokens["Namespaces"]</c> in MVC 5 and has no ASP.NET Core counterpart, since
    /// controller discovery is application-part based.
    /// </para>
    /// <para>
    /// <c>Priority</c> stays 0 (3.90's value for every plugin provider). Both patterns are fully
    /// literal so they can only be matched by their own URLs; no <c>AmbiguousMatchException</c> is
    /// possible.
    /// </para>
    /// <para>
    /// A plugin's routes only exist while it is INSTALLED — <c>RoutePublisher</c> skips an
    /// uninstalled plugin's provider (3.90's filter), so a missing endpoint before install is not a
    /// porting defect.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.Pickup.PickupInStore.Create",
                 "Plugins/PickupInStore/Create",
                 new { controller = "PickupInStore", action = "Create" }
            );

            routeBuilder.MapControllerRoute("Plugin.Pickup.PickupInStore.Edit",
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
