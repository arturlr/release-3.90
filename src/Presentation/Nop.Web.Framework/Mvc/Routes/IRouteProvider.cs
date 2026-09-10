using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Mvc.Routes
{
    /// <summary>
    /// nopCommerce's route-registration extensibility point. Every assembly that wants to
    /// contribute routes (Nop.Web, Nop.Admin, and any plugin) implements this and is
    /// discovered reflectively by <see cref="IRoutePublisher"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BREAKING CONTRACT CHANGE — task 6.4. In 3.90 this was
    /// <c>void RegisterRoutes(System.Web.Routing.RouteCollection routes)</c>. There is no
    /// <c>RouteCollection</c> and no <c>RouteTable</c> in ASP.NET Core: routes are endpoints
    /// registered on an <see cref="IEndpointRouteBuilder"/> inside <c>UseEndpoints(...)</c>.
    /// The parameter type is therefore <see cref="IEndpointRouteBuilder"/>.
    /// </para>
    /// <para>
    /// <b>What implementers must change</b> (tasks 7.3, 8.2 and every plugin task 10.x–15.x):
    /// <list type="number">
    /// <item>replace <c>using System.Web.Routing;</c> / <c>using System.Web.Mvc;</c> with
    /// <c>using Microsoft.AspNetCore.Routing;</c> (plus
    /// <c>using Microsoft.AspNetCore.Builder;</c> when <c>MapControllerRoute</c> is called
    /// directly — it is an extension method in that namespace);</item>
    /// <item>change the parameter type to <see cref="IEndpointRouteBuilder"/>;</item>
    /// <item><c>routes.MapRoute(name, url, defaults, namespaces)</c> →
    /// <c>routeBuilder.MapControllerRoute(name, pattern, defaults)</c>. The
    /// <c>string[] namespaces</c> argument has NO ASP.NET Core counterpart and is dropped —
    /// controller discovery is application-part based, not namespace based;</item>
    /// <item><c>routes.MapLocalizedRoute(...)</c> and <c>routes.MapGenericPathRoute(...)</c>
    /// keep their names and argument lists (including the now-ignored <c>namespaces</c>
    /// overloads) — see <see cref="Nop.Web.Framework.Localization.LocalizedRouteExtensions"/>
    /// and <see cref="Nop.Web.Framework.Seo.GenericPathRouteExtensions"/>;</item>
    /// <item><c>System.Web.Mvc.UrlParameter.Optional</c> in a <c>defaults</c> object has no
    /// counterpart: express the optional segment in the pattern instead
    /// (<c>"p/{productId}/{SeName}"</c> + <c>SeName = UrlParameter.Optional</c> becomes
    /// <c>"p/{productId}/{SeName?}"</c>);</item>
    /// <item>an <c>AreaRegistration</c> becomes
    /// <c>routeBuilder.MapAreaControllerRoute(name, areaName, pattern)</c> registered from
    /// an <see cref="IRouteProvider"/> (task 8.2).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Ordering.</b> <see cref="Priority"/> is unchanged and still orders providers
    /// descending, so <c>GenericUrlRouteProvider</c> (-1000000) still registers last. Note
    /// that endpoint routing does NOT resolve two equally-specific patterns by registration
    /// order — it throws <c>AmbiguousMatchException</c>. Use
    /// <c>Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithOrder(int)</c>
    /// on the value returned by the <c>Map*</c> call to break such ties. This matters for
    /// <c>Nop.Web</c>'s seven name-only <c>"{SeName}"</c> routes, which collide with the
    /// generic-path <c>"{generic_se_name}"</c> route — see the remarks on
    /// <see cref="Nop.Web.Framework.Seo.GenericPathRouteExtensions"/>.
    /// </para>
    /// </remarks>
    public interface IRouteProvider
    {
        /// <summary>
        /// Register routes as endpoints
        /// </summary>
        /// <param name="routeBuilder">Endpoint route builder</param>
        void RegisterRoutes(IEndpointRouteBuilder routeBuilder);

        /// <summary>
        /// Registration order. Providers are invoked in DESCENDING priority order.
        /// </summary>
        int Priority { get; }
    }
}
