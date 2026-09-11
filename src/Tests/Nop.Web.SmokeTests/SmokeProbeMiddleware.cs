using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Services.Helpers;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Registers <see cref="SmokeProbeMiddleware"/> at the very front of the real pipeline.
    /// </summary>
    /// <remarks>
    /// <c>IStartupFilter</c> is used rather than an override of the pipeline, because the pipeline
    /// order in <c>Program.cs</c> is itself under test (runtime-deferrals.md §17.7 calls it "a hard
    /// constraint") and must not be rewritten by the harness. A startup filter wraps the existing
    /// configuration instead of replacing it, and <c>WebApplication</c> honours filters registered
    /// in the service collection.
    /// </remarks>
    public class SmokeProbeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<SmokeProbeMiddleware>();
                next(app);
            };
        }
    }

    /// <summary>
    /// A read-only diagnostic endpoint under <c>/__smoke/</c>, and a strict pass-through for
    /// everything else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Several things task 7.7 must verify are only observable from <b>inside a live request</b>,
    /// and no nopCommerce controller exposes them:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Per-request Autofac scope sharing</b> (runtime deferral 1.3/3). Outside a request
    /// <c>ContainerManager.CurrentScopeProvider</c> returns null by design, so the only way to
    /// prove the seam actually shares a scope — rather than silently handing out a fresh one per
    /// resolve, which is the exact regression the deferral describes — is to resolve twice within
    /// one request and compare instances.</item>
    /// <item><b>Outbound route generation for the seven <c>SuppressMatchingMetadata</c> routes</b>
    /// (runtime-deferrals.md §28.1). Task 7.3 verified this against a synthetic probe app, never
    /// against nopCommerce's real endpoint set.</item>
    /// <item><b><c>IUserAgentHelper.IsSearchEngine()</c></b>, newly live as of task 7.4 (deferral
    /// 7.20). It needs an ambient <c>HttpContext</c> carrying a User-Agent, and its first call
    /// parses the 46 MB <c>App_Data/browscap.xml</c>, so both the result and the elapsed time
    /// matter.</item>
    /// </list>
    /// <para>
    /// <b>It cannot perturb the application.</b> A request whose path does not start with
    /// <c>/__smoke/</c> is passed to <c>next</c> untouched before anything else happens, so the
    /// storefront, the install redirect and the static-file allow-list all see exactly the
    /// pipeline <c>Program.cs</c> built. The probe paths themselves are handled and terminated
    /// here, ahead of <c>InstallUrlMiddleware</c>, so they remain reachable in install mode.
    /// </para>
    /// </remarks>
    public class SmokeProbeMiddleware
    {
        public const string Prefix = "/__smoke/";

        private readonly RequestDelegate _next;

        public SmokeProbeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.HasValue ? context.Request.Path.Value : string.Empty;
            if (!path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var probe = path.Substring(Prefix.Length).Trim('/').ToLowerInvariant();
            var sb = new StringBuilder();

            try
            {
                switch (probe)
                {
                    case "ok":
                        sb.AppendLine("probe=ok");
                        break;
                    case "scope":
                        WriteScopeProbe(context, sb);
                        break;
                    case "routeurl":
                        WriteRouteUrlProbe(context, sb);
                        break;
                    case "searchengine":
                        WriteSearchEngineProbe(sb);
                        break;
                    case "endpoints":
                        WriteEndpointsProbe(context, sb);
                        break;
                    case "action":
                        WriteActionProbe(context, sb);
                        break;
                    case "adminarea":
                        WriteAdminAreaProbe(context, sb);
                        break;
                    default:
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        sb.AppendLine("unknown probe: " + probe);
                        break;
                }
            }
            catch (Exception ex)
            {
                //Reported as text with a 200 rather than rethrown: the assertions live in the
                //test, and an exception surfacing here would be swallowed into a generic 500 by
                //the app's own UseExceptionHandler, losing the type and message.
                sb.AppendLine("EXCEPTION=" + ex.GetType().FullName + ": " + ex.Message);
            }

            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(sb.ToString());
        }

        /// <summary>
        /// Runtime deferral 1.3 / 3 — is the per-request lifetime scope really shared?
        /// </summary>
        private static void WriteScopeProbe(HttpContext context, StringBuilder sb)
        {
            var requestScope = context.RequestServices.GetService<ILifetimeScope>();
            sb.AppendLine("requestServicesIsLifetimeScope=" + (requestScope != null));

            var provider = ContainerManager.CurrentScopeProvider;
            sb.AppendLine("currentScopeProviderAssigned=" + (provider != null));

            if (provider != null)
            {
                var a = provider();
                var b = provider();
                sb.AppendLine("providerReturnsNonNull=" + (a != null));
                sb.AppendLine("providerScopeStable=" + ReferenceEquals(a, b));
                sb.AppendLine("providerScopeIsRequestServices=" + ReferenceEquals(a, requestScope));
            }

            //IWebHelper is InstancePerLifetimeScope and its whole graph is IHttpContextAccessor,
            //so it is constructible with no database - which matters, because every
            //Nop.Services graph is not (see Uninstalled_store_fails_service_resolution_loudly).
            //Two resolves through the nop engine within one request MUST return the same object;
            //if CurrentScopeProvider were unset, ContainerManager.Scope() would open a fresh scope
            //per call and these would differ - the exact regression deferral 1.3 describes.
            var engine = EngineContext.Current;
            var h1 = engine.Resolve<IWebHelper>();
            var h2 = engine.Resolve<IWebHelper>();
            sb.AppendLine("perRequestServiceShared=" + ReferenceEquals(h1, h2));
            sb.AppendLine("perRequestServiceType=" + h1.GetType().FullName);

            //And it must be the SAME instance the request scope itself yields, i.e. the nop seam
            //and ASP.NET Core's own request scope are one scope, not two.
            if (requestScope != null)
            {
                var h3 = requestScope.Resolve<IWebHelper>();
                sb.AppendLine("sharedWithRequestScope=" + ReferenceEquals(h1, h3));
            }

            var s1 = engine.ContainerManager.Scope();
            var s2 = engine.ContainerManager.Scope();
            sb.AppendLine("containerManagerScopeStable=" + ReferenceEquals(s1, s2));

            //And the scope must belong to the ONE container the engine owns, not a second one.
            sb.AppendLine("containerTagRoot=" + (engine.ContainerManager.Container != null));
        }

        /// <summary>
        /// runtime-deferrals.md §28.1 — the seven name-only routes carry
        /// <c>SuppressMatchingMetadata</c>, which must remove them from inbound matching while
        /// leaving them usable for link generation. This is what every
        /// <c>Url.RouteUrl("Product", new { SeName = … })</c> call site in the views depends on.
        /// </summary>
        private static void WriteRouteUrlProbe(HttpContext context, StringBuilder sb)
        {
            var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();

            var byName = new Dictionary<string, object>
            {
                { "Product", new { SeName = "smoke-product-slug" } },
                { "Category", new { SeName = "smoke-category-slug" } },
                { "Manufacturer", new { SeName = "smoke-manufacturer-slug" } },
                { "Vendor", new { SeName = "smoke-vendor-slug" } },
                { "NewsItem", new { SeName = "smoke-news-slug" } },
                { "BlogPost", new { SeName = "smoke-blog-slug" } },
                { "Topic", new { SeName = "smoke-topic-slug" } },
                { "ShoppingCart", new { } },
                { "HomePage", new { } }
            };

            foreach (var pair in byName)
            {
                string generated;
                try
                {
                    generated = linkGenerator.GetPathByRouteValues(context, pair.Key, pair.Value) ?? "<null>";
                }
                catch (Exception ex)
                {
                    generated = "<" + ex.GetType().Name + ">";
                }
                sb.AppendLine("route:" + pair.Key + "=" + generated);
            }
        }

        /// <summary>
        /// Lists the registered endpoints whose route pattern contains <c>?q=</c>, with the HTTP
        /// methods each accepts. Requirement 4.6 — the <c>RouteCollection</c> →
        /// <c>IEndpointRouteBuilder</c> port is only observable through the endpoint table.
        /// </summary>
        private static void WriteEndpointsProbe(HttpContext context, StringBuilder sb)
        {
            var q = context.Request.Query["q"].ToString();
            var sources = context.RequestServices.GetServices<EndpointDataSource>();

            foreach (var endpoint in sources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>())
            {
                var pattern = endpoint.RoutePattern.RawText ?? string.Empty;
                if (!string.IsNullOrEmpty(q) &&
                    pattern.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
                var suppressed = endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>() != null;
                sb.AppendLine(string.Format(
                    "endpoint pattern={0} order={1} methods={2} suppressMatching={3} name={4}",
                    pattern,
                    endpoint.Order,
                    methods == null || methods.HttpMethods.Count == 0
                        ? "<any>" : string.Join("|", methods.HttpMethods),
                    suppressed,
                    endpoint.DisplayName));
            }
        }

        /// <summary>
        /// Runtime deferral 7.3-4 — reports, for one <c>Controller.Action</c>, (a) every registered
        /// endpoint MVC built for it and whether that endpoint is suppressed from inbound matching,
        /// and (b) whether its <c>ControllerActionDescriptor</c> is still present in the collection
        /// <c>Html.Action</c>'s bridge queries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Both halves are needed, and (b) is the one that matters most. The fix for 7.3-4 adds
        /// <c>ISuppressMatchingMetadata</c> to the action's endpoints. Task 7.3's
        /// <c>ChildActionExtensions</c> bridge does not use the matcher at all — it resolves the
        /// action through <c>IActionDescriptorCollectionProvider</c> and invokes the method by
        /// reflection — so suppression should be invisible to it. "Should" is not evidence, and if
        /// it were wrong the home page's ~15 child actions would silently stop rendering, which
        /// would be a far worse outcome than the exposure being fixed. So the descriptor lookup is
        /// asserted directly, and it is asserted in <b>install mode too</b>, where no page can be
        /// rendered to notice the breakage.
        /// </para>
        /// <para>Query string: <c>?controller=Common&amp;action=Footer</c>, optionally
        /// <c>&amp;area=Admin</c>.</para>
        /// <para>
        /// <b>The <c>area</c> filter is not optional decoration — task 8.8 added it because two
        /// controller+action pairs exist in BOTH areas:</b> <c>Common.LanguageSelector</c> and
        /// <c>Widget.WidgetsByZone</c>. Without the filter, an assertion about the admin action
        /// could be satisfied by the storefront's endpoint and vice versa, which is precisely the
        /// "assertion that passes for the wrong reason" this suite exists to avoid. Omit the
        /// parameter to match any area (the pre-8.8 behaviour); pass <c>area=Admin</c> to require
        /// the admin one; pass <c>area=</c> (empty) to require an action with NO area.
        /// </para>
        /// </remarks>
        private static void WriteActionProbe(HttpContext context, StringBuilder sb)
        {
            var controllerName = context.Request.Query["controller"].ToString();
            var actionName = context.Request.Query["action"].ToString();
            var hasAreaFilter = context.Request.Query.ContainsKey("area");
            var areaName = context.Request.Query["area"].ToString();
            sb.AppendLine("query=" + controllerName + "." + actionName +
                (hasAreaFilter ? " area=[" + areaName + "]" : " area=<any>"));

            Func<ControllerActionDescriptor, bool> matches = d =>
            {
                if (d == null)
                    return false;
                if (!string.Equals(d.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(d.ActionName, actionName, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (!hasAreaFilter)
                    return true;

                string actual;
                if (!d.RouteValues.TryGetValue("area", out actual))
                    actual = null;
                return string.Equals(actual ?? string.Empty, areaName, StringComparison.OrdinalIgnoreCase);
            };

            //(a) the endpoint table - every endpoint built for this action, from the Default
            //conventional route and from any explicit MapControllerRoute that targets it.
            var sources = context.RequestServices.GetServices<EndpointDataSource>();
            var endpoints = sources.SelectMany(s => s.Endpoints)
                .Where(e => matches(e.Metadata.GetMetadata<ControllerActionDescriptor>()))
                .ToList();

            sb.AppendLine("endpointCount=" + endpoints.Count);
            var matchable = 0;
            foreach (var endpoint in endpoints)
            {
                var suppressed = endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>();
                var isSuppressed = suppressed != null && suppressed.SuppressMatching;
                if (!isSuppressed)
                    matchable++;

                var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                var route = endpoint as RouteEndpoint;
                sb.AppendLine("endpoint pattern=" + (route == null ? "<not-a-route>" : route.RoutePattern.RawText)
                    + " suppressMatching=" + isSuppressed
                    + " declaringType=" + descriptor.ControllerTypeInfo.FullName
                    + " signature=" + Signature(descriptor));
            }
            sb.AppendLine("matchableEndpointCount=" + matchable);

            //(b) the descriptor collection Html.Action's bridge queries - MUST still contain it.
            var provider = context.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            var descriptors = provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(d => matches(d))
                .ToList();
            sb.AppendLine("actionDescriptorCount=" + descriptors.Count);
            sb.AppendLine("visibleToChildActionBridge=" + (descriptors.Count > 0));
            foreach (var descriptor in descriptors)
                sb.AppendLine("descriptorSignature=" + Signature(descriptor));
        }

        /// <summary>
        /// A stable, readable identity for one action METHOD, so overloads can be told apart.
        /// </summary>
        /// <remarks>
        /// Needed because <c>[NopChildActionOnly]</c> is applied per METHOD, not per action name,
        /// and exactly one place in the solution exercises the difference:
        /// <c>Nop.Admin.Controllers.CommonController.PopularSearchTermsReport</c> has a marked
        /// parameterless overload (the child action that renders the partial) and an <b>unmarked</b>
        /// <c>[HttpPost] (DataSourceRequest command)</c> overload (the Kendo grid data action, which
        /// must stay URL-reachable). 3.90 had the identical shape — verified against git history.
        /// </remarks>
        private static string Signature(ControllerActionDescriptor descriptor)
        {
            if (descriptor == null)
                return "<null>";
            var parameters = descriptor.Parameters == null || descriptor.Parameters.Count == 0
                ? string.Empty
                : string.Join(",", descriptor.Parameters.Select(p => p.ParameterType.Name));
            return descriptor.ActionName + "(" + parameters + ")";
        }

        /// <summary>
        /// Task 8.8 — everything about the <c>Admin</c> area that is only observable from inside
        /// the running host: how <c>Nop.Admin.dll</c> was discovered, which MVC application parts
        /// it contributed, its compiled Razor view identifiers, whether every one of its
        /// controllers inherited <c>[Area("Admin")]</c>, and the shape of the area route.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Task 8.2 could prove all of this only against a throwaway stand-in assembly, because
        /// <c>Nop.Admin</c> did not compile. Task 8.8's <c>ProjectReference</c>-plus-copy
        /// arrangement (runtime deferral 8.4-1) makes the real assembly discoverable, and
        /// deliberately reproduces production's shape: present in the base directory, <b>absent
        /// from <c>deps.json</c></b>, so the path-based load in
        /// <c>AppDomainTypeFinder.LoadMatchingAssemblies</c> is the mechanism actually exercised.
        /// </para>
        /// <para>
        /// Nothing here names a <c>Nop.Admin</c> type: the assembly is reached reflectively,
        /// which is how the host reaches it and what lets this project keep its
        /// <c>ReferenceOutputAssembly="false"</c> reference.
        /// </para>
        /// </remarks>
        private static void WriteAdminAreaProbe(HttpContext context, StringBuilder sb)
        {
            const string adminAssemblyName = "Nop.Admin";
            const string adminAreaName = "Admin";

            //--- (1) how was the assembly discovered? ------------------------------------------
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == adminAssemblyName);
            sb.AppendLine("assemblyLoaded=" + (loaded != null));
            if (loaded != null)
            {
                sb.AppendLine("assemblyLocation=" + loaded.Location);

                var loadedDir = string.IsNullOrEmpty(loaded.Location)
                    ? null
                    : System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(loaded.Location));
                var baseDir = System.IO.Path.GetFullPath(
                    AppContext.BaseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                sb.AppendLine("assemblyInBaseDirectory=" +
                    (loadedDir != null &&
                     string.Equals(loadedDir, baseDir, StringComparison.OrdinalIgnoreCase)));

                sb.AppendLine("assemblyLoadContextIsDefault=" +
                    ReferenceEquals(
                        System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(loaded),
                        System.Runtime.Loader.AssemblyLoadContext.Default));
            }

            //The property that makes this a faithful reproduction of production rather than a
            //different mechanism wearing the same name. If Nop.Admin ever appears in this test
            //project's deps.json, the assembly is resolved from the trusted-platform-assemblies
            //list and the base-directory probe is no longer under test at all.
            var depsPath = System.IO.Path.Combine(AppContext.BaseDirectory,
                "Nop.Web.SmokeTests.deps.json");
            sb.AppendLine("depsJsonExists=" + System.IO.File.Exists(depsPath));
            if (System.IO.File.Exists(depsPath))
            {
                sb.AppendLine("adminInDepsJson=" +
                    System.IO.File.ReadAllText(depsPath)
                        .Contains(adminAssemblyName, StringComparison.OrdinalIgnoreCase));
            }

            //--- (2) MVC application parts -----------------------------------------------------
            //Task 8.2 used ApplicationPartFactory rather than `new AssemblyPart(assembly)`
            //precisely so a Razor-SDK assembly contributes BOTH its controllers and its compiled
            //views. A bare AssemblyPart would register the controllers and silently leave every
            //view unresolvable - deferral 8.1-4's failure mode arriving by another route.
            var partManager = context.RequestServices.GetRequiredService<ApplicationPartManager>();
            foreach (var part in partManager.ApplicationParts.Where(p => p.Name == adminAssemblyName))
                sb.AppendLine("adminPart=" + part.GetType().Name);

            //--- (3) compiled Razor view identifiers -------------------------------------------
            var views = new ViewsFeature();
            partManager.PopulateFeature(views);
            var adminViews = views.ViewDescriptors
                .Select(v => v.RelativePath ?? string.Empty)
                .Where(p => p.StartsWith("/Areas/Admin/Views/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            sb.AppendLine("adminViewCount=" + adminViews.Count);
            sb.AppendLine("viewsUnderLegacyAdministrationPath=" + views.ViewDescriptors
                .Count(v => (v.RelativePath ?? string.Empty)
                    .StartsWith("/Administration/", StringComparison.OrdinalIgnoreCase)));
            foreach (var probe in new[]
            {
                "/Areas/Admin/Views/_ViewImports.cshtml",
                "/Areas/Admin/Views/_ViewStart.cshtml",
                "/Areas/Admin/Views/Shared/_AdminLayout.cshtml",
                "/Areas/Admin/Views/Shared/Menu.cshtml",
                "/Areas/Admin/Views/Home/Index.cshtml",
                "/Areas/Admin/Views/Product/List.cshtml"
            })
            {
                sb.AppendLine("view:" + probe + "=" + adminViews.Any(
                    p => string.Equals(p, probe, StringComparison.OrdinalIgnoreCase)));
            }

            //--- (4) [Area("Admin")] inherited by EVERY admin controller ----------------------
            //Declared once on the abstract BaseAdminController; AreaAttribute derives from
            //RouteValueAttribute, which is Inherited = true. Task 8.2 proved the MECHANISM on a
            //single probe controller and rested the COVERAGE claim on reading 54 class
            //declarations. This counts them off the live descriptor collection instead.
            var provider = context.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            var adminDescriptors = provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(d => d.ControllerTypeInfo.Assembly.GetName().Name == adminAssemblyName)
                .ToList();

            var byController = adminDescriptors
                .GroupBy(d => d.ControllerTypeInfo.FullName)
                .OrderBy(g => g.Key)
                .ToList();
            sb.AppendLine("adminControllerCount=" + byController.Count);
            sb.AppendLine("adminActionCount=" + adminDescriptors.Count);

            var wrongArea = byController
                .Where(g => !g.All(d =>
                {
                    string area;
                    return d.RouteValues.TryGetValue("area", out area) &&
                           string.Equals(area, adminAreaName, StringComparison.Ordinal);
                }))
                .Select(g => g.Key)
                .ToList();
            sb.AppendLine("adminControllersWithoutAreaCount=" + wrongArea.Count);
            foreach (var name in wrongArea)
                sb.AppendLine("controllerWithoutArea=" + name);

            //--- (5) the area route itself ----------------------------------------------------
            //3.90's AdminAreaRegistration.cs registered
            //  "Admin_default", "Admin/{controller}/{action}/{id}",
            //  new { controller = "Home", action = "Index", area = "Admin", id = "" }
            //Task 8.2 preserved the name, the prefix and both defaults; `id = ""` became {id?}.
            var routeEndpoints = context.RequestServices.GetServices<EndpointDataSource>()
                .SelectMany(s => s.Endpoints)
                .OfType<RouteEndpoint>()
                .Where(e => (e.RoutePattern.RawText ?? string.Empty)
                    .StartsWith("Admin/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            sb.AppendLine("adminRoutePatternCount=" +
                routeEndpoints.Select(e => e.RoutePattern.RawText).Distinct().Count());
            foreach (var pattern in routeEndpoints.Select(e => e.RoutePattern.RawText).Distinct())
                sb.AppendLine("adminRoutePattern=" + pattern);

            var homeIndex = routeEndpoints
                .Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
                .FirstOrDefault(d => d != null &&
                    d.ControllerTypeInfo.Assembly.GetName().Name == adminAssemblyName &&
                    d.ControllerName == "Home" && d.ActionName == "Index");
            sb.AppendLine("adminHomeIndexReachable=" + (homeIndex != null));
            if (homeIndex != null)
                sb.AppendLine("adminHomeIndexType=" + homeIndex.ControllerTypeInfo.FullName);
        }

        /// <summary>
        /// Runtime deferral 7.20 — <c>NopConfig.UserAgentStringsPath</c> is configured as of task
        /// 7.4, so <c>IsSearchEngine()</c> stops unconditionally returning false. The first call
        /// parses a 46 MB XML file and then WRITES <c>App_Data/browscap.crawlersonly.xml</c>.
        /// </summary>
        private static void WriteSearchEngineProbe(StringBuilder sb)
        {
            var helper = EngineContext.Current.Resolve<IUserAgentHelper>();
            var sw = Stopwatch.StartNew();
            var isSearchEngine = helper.IsSearchEngine();
            sw.Stop();
            sb.AppendLine("isSearchEngine=" + isSearchEngine);
            sb.AppendLine("elapsedMs=" + sw.ElapsedMilliseconds);
        }
    }
}
