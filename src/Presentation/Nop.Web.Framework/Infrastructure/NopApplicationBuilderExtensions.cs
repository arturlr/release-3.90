using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routes;
using Nop.Web.Framework.Seo;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// The request pipeline nopCommerce needs, in the order it needs it. This is the
    /// <c>IHttpModule</c>/<c>IHttpHandler</c> → middleware port of task 6.4, expressed so that
    /// task 7.2's <c>Program.cs</c> is one call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Middleware inventory and where each behaviour came from.</b>
    /// <list type="table">
    /// <item><term><c>UseForwardedHeaders</c></term><description>framework, not nopCommerce.
    /// Required so <c>IWebHelper.IsCurrentConnectionSecured()</c> is correct behind a
    /// TLS-terminating proxy, otherwise <c>ForceSslForAllPages</c> loops (deferral 11.29).</description></item>
    /// <item><term><c>UseStaticFiles</c></term><description>replaces <c>system.webServer</c>'s
    /// static handler pipeline and 3.90's <c>Application_BeginRequest</c> "ignore static
    /// resources" early exit. Also replaces the
    /// <c>&lt;add name="DenyAccessToPluginDLLs" path="*.dll" type="HttpForbiddenHandler"/&gt;</c>
    /// handler: <c>.dll</c> is not in the default content-type map, so the static-file
    /// middleware refuses to serve it and the request 404s.</description></item>
    /// <item><term><see cref="InstallUrlMiddleware"/></term><description>the install-mode
    /// redirect from <c>Application_BeginRequest</c>.</description></item>
    /// <item><term><see cref="SeoFriendlyUrlsMiddleware"/></term><description>the inbound half
    /// of <c>LocalizedRoute</c> — strips <c>/en</c> into <c>PathBase</c>. MUST be before
    /// <c>UseRouting</c>.</description></item>
    /// <item><term><c>UseRouting</c></term><description>replaces
    /// <c>System.Web.Routing.UrlRoutingModule</c>, including the three
    /// <c>system.webServer/handlers</c> entries (<c>SitemapXml</c>, <c>RobotsTxt</c>,
    /// <c>MiniProfiler</c>) whose only job was to force those extensionless paths through
    /// <c>UrlRoutingModule</c>. In ASP.NET Core every path goes through routing, so all three
    /// disappear with nothing to replace them.</description></item>
    /// <item><term><see cref="SlugRedirectMiddleware"/></term><description>executes the 301/302
    /// that <c>GenericPathRoute</c> used to write with <c>Response.End()</c>. MUST be
    /// immediately after <c>UseRouting</c>.</description></item>
    /// <item><term><c>UseSession</c></term><description>deferral 7.15.</description></item>
    /// <item><term><c>UseAuthentication</c></term><description>replaces
    /// <c>&lt;authentication mode="Forms"&gt;</c> and <c>FormsAuthenticationModule</c>
    /// (deferral 7.13). Also what makes <c>ChallengeResult</c> work instead of throwing
    /// (deferral 11.25).</description></item>
    /// <item><term><see cref="WorkingCultureMiddleware"/></term><description>3.90's
    /// <c>Application_AuthenticateRequest</c> → <c>SetWorkingCulture()</c>. MUST be after
    /// <c>UseAuthentication</c> — the original comment says so explicitly.</description></item>
    /// <item><term><c>UseAuthorization</c></term><description>framework requirement between
    /// routing and endpoints.</description></item>
    /// <item><term><see cref="UseNopEndpoints"/></term><description><c>UseEndpoints</c> +
    /// <c>IRoutePublisher</c>, replacing <c>Global.asax</c>'s
    /// <c>RegisterRoutes(RouteTable.Routes)</c> and <c>AreaRegistration.RegisterAllAreas()</c>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>NOT ported here, and owned by task 7.2</b> (each was a <c>Global.asax</c> event, not
    /// a module):
    /// <list type="bullet">
    /// <item><c>Application_Error</c> — exception logging via <c>ILogger</c> plus the
    /// re-execution of <c>CommonController.PageNotFound</c> for 404s. Use
    /// <c>UseExceptionHandler</c> + <c>UseNopStatusCodePages()</c> (see
    /// <see cref="NopStatusCodePagesExtensions"/> — the stock
    /// <c>UseStatusCodePagesWithReExecute</c> also swallows bodiless 400/403/405 responses,
    /// deferral 7.7-1), and keep the <c>CommonSettings.Log404Errors</c> check.</item>
    /// <item><c>Application_BeginRequest</c>/<c>EndRequest</c> MiniProfiler start/stop, and the
    /// <c>GlobalFilters.Filters.Add(new ProfilingActionFilter())</c> in
    /// <c>Application_Start</c>. MiniProfiler 3.x / <c>StackExchange.Profiling.Mvc</c> is
    /// MVC5-only and is not a <c>PackageReference</c> of any migrated project, so profiling is
    /// dropped. <c>StoreInformationSettings.DisplayMiniProfilerInPublicStore</c> becomes
    /// inert.</item>
    /// <item><c>ServicePointManager.SecurityProtocol = Tls12</c> and
    /// <c>MvcHandler.DisableMvcResponseHeader = true</c> from <c>Application_Start</c>. TLS 1.2+
    /// is the .NET default now; the <c>X-AspNetMvc-Version</c> header no longer exists.</item>
    /// <item><c>routes.IgnoreRoute("favicon.ico")</c> and
    /// <c>routes.IgnoreRoute("{resource}.axd/{*pathInfo}")</c> — <c>.axd</c> handlers do not
    /// exist, and <c>UseStaticFiles</c> serves favicon.ico before routing.</item>
    /// <item><c>TaskManager.Instance.Initialize()/Start()</c> and
    /// <c>PluginManager.Initialize()</c> (deferral 1.1), <c>CommonHelper.BaseDirectory</c>
    /// (deferral 1.5), <c>NopConfigurationManager.Configuration</c> (deferral 1.4),
    /// <c>SqlServerDataProvider.DatabaseInitializer?.InitializeDatabase(...)</c>
    /// (deferral 4.8).</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class NopApplicationBuilderExtensions
    {
        /// <summary>
        /// The whole nopCommerce request pipeline in the correct order. Call this from
        /// <c>Program.cs</c> after <c>UseForwardedHeaders()</c>, the error handler and
        /// <c>UseStaticFiles()</c>:
        /// <code>
        /// app.UseForwardedHeaders();
        /// app.UseExceptionHandler("/error");                 // task 7.2
        /// app.UseNopStatusCodePages();                       // 404 -> /page-not-found, 404 ONLY
        /// app.UseStaticFiles();
        /// app.UseNopPipeline();                              // this method
        /// </code>
        /// </summary>
        public static IApplicationBuilder UseNopPipeline(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException("app");

            app.UseNopInstallUrl();
            app.UseNopSeoFriendlyUrls();

            app.UseRouting();

            app.UseNopSlugRedirect();

            app.UseSession();
            app.UseAuthentication();
            app.UseNopWorkingCulture();
            app.UseAuthorization();

            app.UseNopEndpoints();

            return app;
        }

        /// <summary>
        /// Install-mode redirect. Register BEFORE <c>UseRouting()</c>.
        /// </summary>
        public static IApplicationBuilder UseNopInstallUrl(this IApplicationBuilder app)
        {
            return app.UseMiddleware<InstallUrlMiddleware>();
        }

        /// <summary>
        /// Strips the language SEO code out of the path and into <c>PathBase</c>.
        /// MUST be registered BEFORE <c>UseRouting()</c>.
        /// </summary>
        public static IApplicationBuilder UseNopSeoFriendlyUrls(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SeoFriendlyUrlsMiddleware>();
        }

        /// <summary>
        /// Executes the SEO slug 301/302 decided during route matching.
        /// MUST be registered IMMEDIATELY AFTER <c>UseRouting()</c>.
        /// </summary>
        public static IApplicationBuilder UseNopSlugRedirect(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SlugRedirectMiddleware>();
        }

        /// <summary>
        /// Sets the request culture from <c>IWorkContext.WorkingLanguage</c> (or the fixed
        /// Telerik culture in the admin area). MUST be registered AFTER
        /// <c>UseAuthentication()</c>.
        /// </summary>
        public static IApplicationBuilder UseNopWorkingCulture(this IApplicationBuilder app)
        {
            return app.UseMiddleware<WorkingCultureMiddleware>();
        }

        /// <summary>
        /// Registers every route contributed by an <see cref="IRouteProvider"/> —
        /// <c>Nop.Web</c>, <c>Nop.Admin</c>'s <c>Admin</c> area, and every installed plugin.
        /// This replaces <c>Global.asax</c>'s <c>RegisterRoutes(RouteTable.Routes)</c> and
        /// <c>AreaRegistration.RegisterAllAreas()</c>.
        /// </summary>
        /// <remarks>
        /// The catch-all default route that <c>Global.asax.RegisterRoutes</c> appended after
        /// the publisher —
        /// <c>"{controller}/{action}/{id}"</c> with <c>Home/Index</c> defaults — is added here
        /// so no provider has to own it. It is registered LAST, after every provider, exactly
        /// as in 3.90.
        /// </remarks>
        public static IApplicationBuilder UseNopEndpoints(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException("app");

            return app.UseEndpoints(endpoints =>
            {
                var routePublisher = EngineContext.Current.Resolve<IRoutePublisher>();
                routePublisher.RegisterRoutes(endpoints);

                //Global.asax.cs RegisterRoutes(), after the route publisher:
                //  routes.MapRoute("Default", "{controller}/{action}/{id}",
                //      new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                //      new[] { "Nop.Web.Controllers" });
                //UrlParameter.Optional becomes "{id?}" in the pattern; the namespace filter has
                //no counterpart. WithOrder keeps it losing against every provider-registered
                //route, reproducing "registered last" under endpoint routing (which resolves
                //equal-precedence candidates by order, not by registration sequence).
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}")
                    .WithOrder(int.MaxValue);
            });
        }
    }
}
