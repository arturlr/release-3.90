using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.PayPalStandard
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PDT
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Payments.PayPalStandard.PDTHandler",
                pattern: "Plugins/PaymentPayPalStandard/PDTHandler",
                defaults: new { controller = "PaymentPayPalStandard", action = "PDTHandler" }
            );
            //IPN
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Payments.PayPalStandard.IPNHandler",
                pattern: "Plugins/PaymentPayPalStandard/IPNHandler",
                defaults: new { controller = "PaymentPayPalStandard", action = "IPNHandler" }
            );
            //Cancel
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Payments.PayPalStandard.CancelOrder",
                pattern: "Plugins/PaymentPayPalStandard/CancelOrder",
                defaults: new { controller = "PaymentPayPalStandard", action = "CancelOrder" }
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
