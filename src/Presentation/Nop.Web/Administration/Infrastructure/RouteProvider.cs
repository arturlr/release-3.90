using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Admin.Infrastructure
{
    /// <summary>
    /// Registers the routes of the nopCommerce <c>Admin</c> area.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Task 8.2 — this replaces <c>AdminAreaRegistration.cs</c>, which was deleted.</b>
    /// <c>System.Web.Mvc.AreaRegistration</c>, <c>AreaRegistrationContext</c> and
    /// <c>AreaRegistration.RegisterAllAreas()</c> have no ASP.NET Core counterpart: an area is
    /// no longer a discovered registration object but a route value declared by
    /// <c>[Area]</c> on the controller plus an endpoint registered with
    /// <see cref="ControllerEndpointRouteBuilderExtensions.MapAreaControllerRoute"/>. Because
    /// there is no <c>RegisterAllAreas()</c> equivalent, the registration is contributed through
    /// nopCommerce's own <see cref="IRouteProvider"/> extensibility point, which
    /// <c>RoutePublisher</c> discovers reflectively through <c>ITypeFinder</c> — i.e. from
    /// <c>Nop.Admin</c> itself, with no declaration in the host and no <c>Global.asax</c>
    /// (deleted at task 7.2).
    /// </para>
    /// <para>
    /// <b>3.90's registration, verbatim, for comparison:</b>
    /// <code>
    /// public override string AreaName { get { return "Admin"; } }
    /// context.MapRoute(
    ///     "Admin_default",
    ///     "Admin/{controller}/{action}/{id}",
    ///     new { controller = "Home", action = "Index", area = "Admin", id = "" },
    ///     new[] { "Nop.Admin.Controllers" });
    /// </code>
    /// The URL prefix (<c>Admin/</c>), the route name, the default controller (<c>Home</c>) and
    /// the default action (<c>Index</c>) are all preserved. Two things could not be carried over
    /// literally:
    /// </para>
    /// <list type="bullet">
    /// <item><b><c>string[] namespaces</c> is gone.</b> It became
    /// <c>DataTokens["Namespaces"]</c> in MVC 5 and has no ASP.NET Core counterpart — controller
    /// discovery is application-part based, not namespace based. Dropped, as
    /// <see cref="IRouteProvider"/>'s own remarks instruct.</item>
    /// <item><b><c>id = ""</c> became <c>{id?}</c>.</b> MVC 5 could only make a segment optional
    /// by giving it a default, so a request to <c>/Admin/Product/Edit</c> matched with
    /// <c>RouteData.Values["id"] == ""</c>; with <c>{id?}</c> the value is simply absent.
    /// <b>Verified harmless:</b> no file under <c>Administration/</c> reads
    /// <c>RouteData.Values["id"]</c>, and every admin action that takes an id declares it as a
    /// method parameter (typically <c>int</c>), for which <c>""</c> and "absent" bind
    /// identically. Recorded because it is an observable difference, not because it has a known
    /// consequence.</item>
    /// </list>
    /// <para>
    /// <b>Why <see cref="Priority"/> is <c>int.MaxValue</c>.</b> In 3.90
    /// <c>Application_Start</c> called <c>AreaRegistration.RegisterAllAreas()</c> <i>before</i>
    /// <c>RegisterRoutes(RouteTable.Routes)</c>, and MVC 5's <c>RouteCollection</c> stopped at
    /// the first match — so the admin route was tried before every storefront route. Endpoint
    /// routing compares <c>Endpoint.Order</c> <i>before</i> pattern precedence (measured at task
    /// 7.3, runtime-deferrals.md §28.2) and MVC assigns an auto-incrementing order per
    /// <c>Map*</c> call, so registering first is what reproduces 3.90's outcome.
    /// <c>RoutePublisher</c> invokes providers in descending priority, hence the maximum.
    /// </para>
    /// <para>
    /// In practice the ordering is immaterial for correctness — no storefront pattern begins
    /// with the literal segment <c>admin</c>, so nothing can collide — but it is deterministic
    /// rather than dependent on the order <c>ITypeFinder</c> happens to return assemblies in,
    /// which two providers sharing priority 0 would be.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Name of the administration area. Must match the <c>[Area]</c> value on
        /// <c>Nop.Admin.Controllers.BaseAdminController</c> and the <c>{2}</c> substitution the
        /// Razor view-location expander performs — see
        /// <c>Nop.Web.Framework.Themes.ThemeableViewLocationExpander</c>.
        /// </summary>
        public const string AreaName = "Admin";

        /// <summary>
        /// Register the <c>Admin</c> area routes as endpoints.
        /// </summary>
        /// <param name="routeBuilder">Endpoint route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            //3.90: context.MapRoute("Admin_default", "Admin/{controller}/{action}/{id}",
            //          new { controller = "Home", action = "Index", area = "Admin", id = "" },
            //          new[] { "Nop.Admin.Controllers" });
            routeBuilder.MapAreaControllerRoute("Admin_default", AreaName,
                "Admin/{controller=Home}/{action=Index}/{id?}");
        }

        /// <summary>
        /// Registration order — see the remarks on the class.
        /// </summary>
        public int Priority
        {
            get
            {
                return int.MaxValue;
            }
        }
    }
}
