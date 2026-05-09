using Nop.Web.Framework.Mvc.Routes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;


namespace Nop.Plugin.Payments.PayPalDirect
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Payments.PayPalDirect.Webhook",
                pattern: "Plugins/PaymentPayPalDirect/Webhook",
                defaults: new { controller = "PaymentPayPalDirect", action = "WebhookEventsHandler" }
            );
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
