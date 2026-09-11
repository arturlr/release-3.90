using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.DiscountRules.CustomerRoles
{
    /// <summary>
    /// Registers this plugin's admin configuration route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 10.1 — the ASP.NET Core port of a plugin <c>IRouteProvider</c>, and the worked
    /// example for the other seven plugins that ship one (runtime-deferrals.md §17.4a lists
    /// them). The contract changed from
    /// <c>RegisterRoutes(System.Web.Routing.RouteCollection)</c> to
    /// <c>RegisterRoutes(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)</c>.
    /// </para>
    /// <para>
    /// <b>3.90, verbatim:</b>
    /// <code>
    /// routes.MapRoute("Plugin.DiscountRules.CustomerRoles.Configure",
    ///      "Plugins/DiscountRulesCustomerRoles/Configure",
    ///      new { controller = "DiscountRulesCustomerRoles", action = "Configure" },
    ///      new[] { "Nop.Plugin.DiscountRules.CustomerRoles.Controllers" });
    /// </code>
    /// The route <b>name</b>, the URL pattern and the two defaults are unchanged. Only the
    /// <c>string[] namespaces</c> argument is gone: it became <c>DataTokens["Namespaces"]</c>
    /// in MVC 5 and has no ASP.NET Core counterpart, because controller discovery is
    /// application-part based (§17.4a gotcha 1). It is simply dropped, as
    /// <c>IRouteProvider</c>'s own remarks instruct.
    /// </para>
    /// <para>
    /// The other three gotchas do not arise here and that was checked rather than assumed:
    /// there is no <c>UrlParameter.Optional</c> in this provider (the pattern has no optional
    /// segment — <c>discountId</c> and <c>discountRequirementId</c> travel in the query
    /// string, see <c>CustomerRoleDiscountRequirementRule.GetConfigurationUrl</c>); the
    /// pattern is fully literal so it cannot tie on precedence with anything and no
    /// <c>AmbiguousMatchException</c> is possible; and the route name is non-empty.
    /// </para>
    /// <para>
    /// <b><c>Priority</c> stays 0, which is 3.90's value.</b> Task 8.2 set <c>Nop.Admin</c>'s
    /// provider to <c>int.MaxValue</c> because the admin route had to be tried first;
    /// a plugin has no such requirement, and 0 is what every 3.90 plugin provider returned.
    /// Endpoint routing compares <c>Endpoint.Order</c> before pattern precedence (§28.2) and
    /// <c>MapControllerRoute</c> auto-increments the order per call, so providers sharing
    /// priority 0 are ordered by whatever sequence <c>ITypeFinder</c> yields — which is
    /// immaterial here, since a fully-literal pattern can only be matched by its own URL.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.DiscountRules.CustomerRoles.Configure",
                 "Plugins/DiscountRulesCustomerRoles/Configure",
                 new { controller = "DiscountRulesCustomerRoles", action = "Configure" }
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
