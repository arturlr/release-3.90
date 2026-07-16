using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.PayPalDirect
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayPalDirect.Webhook",
                 "Plugins/PaymentPayPalDirect/Webhook",
                 new { controller = "PaymentPayPalDirect", action = "WebhookEventsHandler" });
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
