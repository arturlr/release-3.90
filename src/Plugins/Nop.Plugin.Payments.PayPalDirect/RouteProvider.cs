using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.PayPalDirect
{
    /// <summary>
    /// Registers this plugin's single webhook route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 12.3. The mechanical edit runtime-deferrals.md §17.4a describes:
    /// <c>RouteCollection</c> → <see cref="IEndpointRouteBuilder"/>, <c>MapRoute</c> →
    /// <c>MapControllerRoute</c>, and the <c>string[] namespaces</c> argument dropped (no
    /// ASP.NET Core counterpart — controller discovery is application-part based). The route
    /// name, URL pattern and defaults are 3.90's, unchanged; <c>Priority</c> stays 0.
    /// </para>
    /// <para>
    /// <b>The PATTERN is part of the plugin's external contract and must not drift.</b>
    /// <c>Plugins/PaymentPayPalDirect/Webhook</c> is the URL registered AT PAYPAL:
    /// <c>PaymentPayPalDirectController.CreateWebHook</c> composes it as
    /// <c>{storeLocation}Plugins/PaymentPayPalDirect/Webhook</c> and POSTs it to PayPal's
    /// webhook API, and the admin Configure view's <c>Instructions</c> resource tells a store
    /// owner to type the same path in by hand. Changing it would silently stop recurring
    /// payments from ever being marked paid.
    /// </para>
    /// <para>
    /// Note the pattern targets <c>WebhookEventsHandler</c>, not an action called
    /// <c>Webhook</c> — that asymmetry is 3.90's and is preserved.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.Payments.PayPalDirect.Webhook",
                 "Plugins/PaymentPayPalDirect/Webhook",
                 new { controller = "PaymentPayPalDirect", action = "WebhookEventsHandler" }
            );
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
