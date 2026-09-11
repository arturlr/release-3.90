using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.PayPalStandard
{
    /// <summary>
    /// Registers this plugin's three storefront-facing routes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 12.4. The port is the mechanical edit runtime-deferrals.md §17.4a describes, applied
    /// three times: <c>RouteCollection</c> → <see cref="IEndpointRouteBuilder"/>,
    /// <c>MapRoute</c> → <c>MapControllerRoute</c>, and the <c>string[] namespaces</c> argument
    /// dropped (no ASP.NET Core counterpart — controller discovery is application-part based).
    /// Route names, URL patterns and defaults are 3.90's, unchanged; <c>Priority</c> stays 0,
    /// as in every 3.90 plugin provider and in group 10's two.
    /// </para>
    /// <para>
    /// <b>These three URLs are not internal — they are configured at PayPal and typed into the
    /// admin Configure screen</b>, so the PATTERNS are part of the plugin's external contract
    /// and must not drift: <c>PDTHandler</c> is where PayPal returns the customer after payment,
    /// <c>IPNHandler</c> is where PayPal POSTs server-to-server notifications (the store owner
    /// pastes this into <c>IpnUrl</c>), and <c>CancelOrder</c> is the cancel-return URL.
    /// <c>PayPalStandardPaymentProcessor.GenerationRedirectionUrl</c> builds all three by
    /// calling <c>_webHelper.GetStoreLocation()</c> and appending these literal paths, so a
    /// changed pattern would silently send a customer to a 404 after paying.
    /// </para>
    /// <para>
    /// All three patterns are fully literal, so none can tie on precedence with another and
    /// §17.4a gotcha 3's <c>AmbiguousMatchException</c> cannot arise — checked, not assumed.
    /// The route NAMES are not used for URL generation anywhere in this plugin (the processor
    /// composes absolute URLs by hand, as 3.90 did), but they are kept verbatim because they
    /// are stable identifiers in the endpoint set and a third-party theme could name them.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            //PDT
            routeBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.PDTHandler",
                 "Plugins/PaymentPayPalStandard/PDTHandler",
                 new { controller = "PaymentPayPalStandard", action = "PDTHandler" }
            );
            //IPN
            routeBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.IPNHandler",
                 "Plugins/PaymentPayPalStandard/IPNHandler",
                 new { controller = "PaymentPayPalStandard", action = "IPNHandler" }
            );
            //Cancel
            routeBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.CancelOrder",
                 "Plugins/PaymentPayPalStandard/CancelOrder",
                 new { controller = "PaymentPayPalStandard", action = "CancelOrder" }
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
