using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
    /// A model with one string and one int, used only by
    /// <c>SmokeProbeMiddleware.WriteChildActionSelectionProbe</c>.
    /// </summary>
    /// <remarks>
    /// Task 11.1. It stands in for a plugin's <c>ConfigurationModel</c> without dragging a plugin
    /// assembly into this project's compile-time references — the plugin references here are
    /// build-order only, deliberately (see <c>Nop.Web.SmokeTests.csproj</c>). Two properties of
    /// different types so a bind that produced a default-constructed instance is distinguishable
    /// from one that actually read the form.
    /// </remarks>
    public class ChildActionBindProbeModel
    {
        public string ProbeText { get; set; }
        public int ProbeNumber { get; set; }
    }

    /// <summary>
    /// A controller that exists solely so the probe can drive the bridge's own
    /// <c>CreateController</c> and <c>ExecuteAction</c> against a real
    /// <see cref="ControllerActionDescriptor"/> whose action takes a COMPLEX parameter — i.e. the
    /// shape of a plugin's <c>[HttpPost] Configure(TSettingsModel)</c>.
    /// </summary>
    /// <remarks>
    /// Task 11.1. It is never routed: it is not part of any application part (this is a test
    /// assembly, not a plugin) and the probe hands it to the bridge directly. <see cref="Received"/>
    /// is what makes the binding observable — <c>ExecuteAction</c> returns only the
    /// <c>IActionResult</c>, so the bound argument has to be recorded by the action itself.
    /// </remarks>
    public class ChildActionBindProbeController : Controller
    {
        /// <summary>
        /// What the bridge's own parameter binding handed the action, or null.
        /// </summary>
        public ChildActionBindProbeModel Received { get; private set; }

        public IActionResult BindProbe(ChildActionBindProbeModel model)
        {
            Received = model;
            return null;
        }
    }

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
                    case "plugins":
                        WritePluginsProbe(context, sb);
                        break;
                    //Tasks 12.1-12.5. Deliberately a separate probe in its own file
                    //(PaymentPluginProbe.cs) - see the remarks there: the five Payments plugins
                    //have facts to report that no other plugin group has, and keeping them out
                    //of WritePluginsProbe keeps this file's change to one line while group 11
                    //edits the same method.
                    case "payments":
                        PaymentPluginProbe.Write(context, sb);
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
        /// Task 10.1–10.3 — the first three migrated plugins, reported from inside a live request.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Task 8.8's <c>PluginDiscoveryTests</c> proved the plugin <b>load</b> path with
        /// <c>Nop.Plugin.SmokeProbe</c>, which has no controller, no route and no view. This probe
        /// covers the three things a real plugin adds, and it is deliberately shaped as a text
        /// report for the same reason <c>WriteAdminAreaProbe</c> is: the interesting facts
        /// (application parts, compiled Razor identifiers, view-engine lookups, endpoint metadata)
        /// are only reachable from inside the running host.
        /// </para>
        /// <para>
        /// <b>What each section is evidence for.</b>
        /// (1) discovery and compatibility — <c>Description.txt</c>'s
        /// <c>SupportedVersions</c> really does satisfy <c>PluginManager</c>, asserted rather than
        /// eyeballed. (2) application parts — deferral <b>1.2</b>: a view-bearing plugin must
        /// contribute a <c>CompiledRazorAssemblyPart</c> as well as an <c>AssemblyPart</c>; the
        /// probe plugin could only ever show the latter. (3) compiled identifiers — the view
        /// resolution decision: the <c>Content</c>/<c>Link</c> metadata puts them back at 3.90's
        /// <c>/Plugins/&lt;ShortName&gt;/Views/…</c>. (4) view-engine lookups — the identifiers are
        /// not merely present, the real <c>IRazorViewEngine</c> finds them at exactly the strings
        /// the ported controllers pass, including the two CROSS-ASSEMBLY paths into
        /// <c>Nop.Admin.dll</c> that deferral <b>8.2-3</b> is about. (5) <c>_ViewStart</c>
        /// isolation, with its counterfactual measured in the same breath. (6) endpoints — the
        /// ported <c>IRouteProvider</c>s registered 3.90's names and patterns, once each.
        /// </para>
        /// </remarks>
        private static void WritePluginsProbe(HttpContext context, StringBuilder sb)
        {
            var shortNames = new[]
            {
                "DiscountRules.CustomerRoles",
                "DiscountRules.HasOneProduct",
                "ExchangeRate.EcbExchange",
                //task 11.1 / 11.2
                "ExternalAuth.Facebook",
                "Feed.GoogleShopping"
            };
            var assemblyNames = shortNames.Select(n => "Nop.Plugin." + n).ToArray();

            //--- (1) discovery, compatibility and load context --------------------------------
            var descriptors = Nop.Core.Plugins.PluginManager.ReferencedPlugins;
            sb.AppendLine("referencedPluginsIsNull=" + (descriptors == null));
            var incompatible = Nop.Core.Plugins.PluginManager.IncompatiblePlugins;
            sb.AppendLine("incompatibleCount=" + (incompatible == null ? -1 : incompatible.Count()));
            if (incompatible != null)
                foreach (var name in incompatible)
                    sb.AppendLine("incompatible=" + name);
            sb.AppendLine("nopVersion=" + NopVersion.CurrentVersion);

            if (descriptors != null)
            {
                foreach (var assemblyName in assemblyNames)
                {
                    var d = descriptors.FirstOrDefault(x => x.ReferencedAssembly != null &&
                        x.ReferencedAssembly.GetName().Name == assemblyName);
                    sb.AppendLine("plugin:" + assemblyName + ".discovered=" + (d != null));
                    if (d == null)
                        continue;

                    sb.AppendLine("plugin:" + assemblyName + ".systemName=" + d.SystemName);
                    sb.AppendLine("plugin:" + assemblyName + ".supportedVersions=" +
                        string.Join("|", d.SupportedVersions));
                    sb.AppendLine("plugin:" + assemblyName + ".supportsCurrentVersion=" +
                        d.SupportedVersions.Contains(NopVersion.CurrentVersion,
                            StringComparer.InvariantCultureIgnoreCase));
                    sb.AppendLine("plugin:" + assemblyName + ".pluginType=" +
                        (d.PluginType == null ? "<null>" : d.PluginType.FullName));
                    sb.AppendLine("plugin:" + assemblyName + ".assignableToIPlugin=" +
                        (d.PluginType != null &&
                         typeof(Nop.Core.Plugins.IPlugin).IsAssignableFrom(d.PluginType)));
                    sb.AppendLine("plugin:" + assemblyName + ".loadContextIsDefault=" +
                        ReferenceEquals(
                            System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(d.ReferencedAssembly),
                            System.Runtime.Loader.AssemblyLoadContext.Default));
                    sb.AppendLine("plugin:" + assemblyName + ".assemblyVersion=" +
                        d.ReferencedAssembly.GetName().Version);
                    sb.AppendLine("plugin:" + assemblyName + ".loadedFrom=" +
                        d.ReferencedAssembly.Location);

                    //TASK 11.2 - Feed.GoogleShopping's taxonomy list is an EMBEDDED RESOURCE that
                    //GoogleService reads BY MANIFEST NAME. If the <EmbeddedResource> item is lost the
                    //stream is null, GetTaxonomyList() returns an empty list, the "Default Google
                    //category" dropdown is silently EMPTY and every feed generation throws
                    //NopException("Default Google category is not set"). Read from the loaded
                    //assembly rather than from disk, and without needing a database.
                    if (assemblyName == "Nop.Plugin.Feed.GoogleShopping")
                    {
                        const string resourceName = "Nop.Plugin.Feed.GoogleShopping.Files.taxonomy.txt";
                        using (var stream = d.ReferencedAssembly.GetManifestResourceStream(resourceName))
                        {
                            sb.AppendLine("plugin:" + assemblyName + ".taxonomyResourcePresent=" +
                                (stream != null));
                            if (stream != null)
                                using (var reader = new System.IO.StreamReader(stream))
                                {
                                    //the exact split GoogleService.GetTaxonomyList performs
                                    var categories = reader.ReadToEnd()
                                        .Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    sb.AppendLine("plugin:" + assemblyName + ".taxonomyCategoryCount=" +
                                        categories.Length);
                                }
                        }
                    }

                    //deployment shape - the directory PluginManager actually scanned
                    if (d.OriginalAssemblyFile != null && d.OriginalAssemblyFile.Directory != null)
                    {                        var dir = d.OriginalAssemblyFile.Directory;
                        sb.AppendLine("plugin:" + assemblyName + ".deployDir=" + dir.Name);
                        sb.AppendLine("plugin:" + assemblyName + ".deployDirParent=" +
                            (dir.Parent == null ? "<null>" : dir.Parent.Name));
                        //Private="false" + NopPluginDoNotDeployHostAssemblies: no OTHER Nop.* dll
                        //may sit next to a plugin, or PluginManager shadow-copies and loads it as
                        //the process's copy of that assembly.
                        var strayNopDlls = dir.GetFiles("Nop.*.dll", System.IO.SearchOption.AllDirectories)
                            .Where(f => !string.Equals(f.Name, assemblyName + ".dll",
                                StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name)
                            .ToList();
                        sb.AppendLine("plugin:" + assemblyName + ".strayNopDlls=" +
                            (strayNopDlls.Count == 0 ? "<none>" : string.Join("|", strayNopDlls)));
                        //3.90 deployed the .cshtml files because System.Web compiled them at
                        //runtime; they are inside the dll now and a deployed copy is dead weight.
                        sb.AppendLine("plugin:" + assemblyName + ".deployedCshtmlCount=" +
                            dir.GetFiles("*.cshtml", System.IO.SearchOption.AllDirectories).Length);
                        sb.AppendLine("plugin:" + assemblyName + ".deployedConfigCount=" +
                            dir.GetFiles("*.config", System.IO.SearchOption.AllDirectories).Length);
                        sb.AppendLine("plugin:" + assemblyName + ".descriptionTxtDeployed=" +
                            System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Description.txt")));
                        sb.AppendLine("plugin:" + assemblyName + ".logoDeployed=" +
                            System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "logo.jpg")));
                    }
                }
            }

            //--- (2) MVC application parts ----------------------------------------------------
            var partManager = context.RequestServices.GetRequiredService<ApplicationPartManager>();
            foreach (var assemblyName in assemblyNames)
                foreach (var part in partManager.ApplicationParts.Where(p => p.Name == assemblyName))
                    sb.AppendLine("part:" + assemblyName + "=" + part.GetType().Name);

            //--- (3) compiled Razor identifiers ------------------------------------------------
            var views = new ViewsFeature();
            partManager.PopulateFeature(views);
            var allViewPaths = views.ViewDescriptors
                .Select(v => v.RelativePath ?? string.Empty)
                .ToList();
            foreach (var shortName in shortNames)
            {
                var prefix = "/Plugins/" + shortName + "/";
                var owned = allViewPaths
                    .Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                sb.AppendLine("identifiers:" + shortName + ".count=" + owned.Count);
                foreach (var p in owned)
                    sb.AppendLine("identifier=" + p);
            }
            //the counterfactual: had the views been left to compile at their project-relative
            //path they would be under /Views/, in the host's own identifier namespace
            //the counterfactual: had the views been left to compile at their project-relative
            //path they would be under /Views/, in the host's own identifier namespace.
            //NOTE the prefixes are the CONTROLLER-named directories the relocation alternative
            //would have used, spelled in full - "/Views/ExternalAuth" would also match Nop.Web's
            //own /Views/ExternalAuthentication/ tree and make this vacuously non-zero.
            sb.AppendLine("pluginViewsUnderHostViewsPath=" + allViewPaths.Count(p =>
                p.StartsWith("/Views/DiscountRules", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("/Views/ExternalAuthFacebook/", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("/Views/FeedGoogleShopping/", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("/Views/Configure.cshtml", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("/Views/PublicInfo.cshtml", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("/Views/ProductAddPopup.cshtml", StringComparison.OrdinalIgnoreCase)));

            //--- (4) the real view engine finds them, at the exact strings the controllers pass --
            var viewEngine = context.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine>();
            foreach (var path in new[]
            {
                //ported controllers, verbatim
                "~/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml",
                "~/Plugins/DiscountRules.HasOneProduct/Views/Configure.cshtml",
                "~/Plugins/DiscountRules.HasOneProduct/Views/ProductAddPopup.cshtml",
                //task 11.1 / 11.2, verbatim from the ported controllers
                "~/Plugins/ExternalAuth.Facebook/Views/Configure.cshtml",
                "~/Plugins/ExternalAuth.Facebook/Views/PublicInfo.cshtml",
                "~/Plugins/Feed.GoogleShopping/Views/Configure.cshtml",
                //deferral 8.2-3: cross-assembly, compiled into Nop.Admin.dll
                "~/Areas/Admin/Views/Shared/_AdminPopupLayout.cshtml",
                "~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml",
                //the pre-8.2 paths the plugin views used to name - these MUST NOT resolve, or
                //the 8.2-3 rewrite would have been unnecessary and the assertion vacuous
                "~/Administration/Views/Shared/_AdminPopupLayout.cshtml",
                "~/Administration/Views/Shared/_GridPagerMessages.cshtml"
            })
            {
                var result = viewEngine.GetView(null, path, false);
                sb.AppendLine("getView:" + path + "=" + result.Success);
            }
            //An extra caller-supplied lookup, so HarnessCanaryTests can travel this exact code path
            //with a path that cannot exist and prove the getView assertions are able to fail.
            var extraPath = context.Request.Query["getView"].ToString();
            if (!string.IsNullOrEmpty(extraPath))
                sb.AppendLine("getView:" + extraPath + "=" +
                    viewEngine.GetView(null, extraPath, false).Success);

            //--- (5) _ViewStart isolation, and the counterfactual -----------------------------
            //RazorViewEngine resolves _ViewStart at RUNTIME by walking the identifier's ancestor
            //directories through the SAME compiler dictionary every application part shares. In
            //3.90 no _ViewStart applied to a plugin view, because they lived at ~/Plugins/... .
            //Had they been compiled under /Views/, Nop.Web's own Views/_ViewStart.cshtml
            //(Layout = "~/Views/Shared/_ColumnsOne.cshtml") WOULD have applied to every one of
            //them - and Pickup.PickupInStore's Configure.cshtml sets no Layout at all.
            foreach (var identifier in new[]
            {
                "/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml",
                "/Views/DiscountRulesCustomerRoles/Configure.cshtml"
            })
            {
                var hits = ViewStartAncestorsOf(identifier)
                    .Where(p => allViewPaths.Any(v => string.Equals(v, p, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                sb.AppendLine("viewStartsApplyingTo:" + identifier + "=" +
                    (hits.Count == 0 ? "<none>" : string.Join("|", hits)));
            }

            //--- (6) endpoints ----------------------------------------------------------------
            //TWO measurements, because they answer different questions.
            //
            //(6a) THE LIVE EndpointDataSource. Note that a plugin's routes are registered only
            //when the plugin is INSTALLED: RoutePublisher.RegisterRoutes skips any provider whose
            //assembly belongs to a descriptor with Installed == false, which is 3.90's behaviour
            //verbatim. So in install mode the correct, faithful result is ZERO plugin endpoints,
            //and the probe reports `installed` alongside so the zero is explained rather than
            //looking like a defect.
            //
            //(6b) THE PORTED PROVIDER ITSELF, driven directly against a scratch
            //IEndpointRouteBuilder over the real service provider. This is what proves the
            //IRouteProvider port - patterns, route names and controller/action targets - without
            //needing an installed store.
            if (descriptors != null)
                foreach (var assemblyName in assemblyNames)
                {
                    var d = descriptors.FirstOrDefault(x => x.ReferencedAssembly != null &&
                        x.ReferencedAssembly.GetName().Name == assemblyName);
                    sb.AppendLine("plugin:" + assemblyName + ".installed=" +
                        (d != null && d.Installed));
                }

            var endpointSource = context.RequestServices.GetRequiredService<EndpointDataSource>();
            var liveEndpoints = endpointSource.Endpoints.OfType<RouteEndpoint>().ToList();
            var providerEndpoints = RegisterPluginRoutesIntoScratchBuilder(context, assemblyNames, sb);

            foreach (var expected in new[]
            {
                "Plugins/DiscountRulesCustomerRoles/Configure",
                "Plugins/DiscountRulesHasOneProduct/Configure",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopup",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopupList",
                "Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames",
                //task 11.1 - ExternalAuth.Facebook's two storefront routes
                "Plugins/ExternalAuthFacebook/Login",
                "Plugins/ExternalAuthFacebook/LoginCallback"
            })
            {
                sb.AppendLine("liveEndpoint:" + expected + ".count=" + liveEndpoints
                    .Count(e => string.Equals(e.RoutePattern.RawText, expected,
                               StringComparison.OrdinalIgnoreCase) &&
                           e.Metadata.GetMetadata<ControllerActionDescriptor>() != null));
                sb.AppendLine("liveEndpoint:" + expected + ".actions=" + string.Join("|",
                    liveEndpoints
                        .Where(e => string.Equals(e.RoutePattern.RawText, expected,
                            StringComparison.OrdinalIgnoreCase))
                        .Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
                        .Where(d => d != null)
                        .Select(d => d.ControllerTypeInfo.FullName + "." + d.ActionName)
                        .Distinct()
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .DefaultIfEmpty("<none>")));

                var matching = providerEndpoints
                    .Where(e => string.Equals(e.RoutePattern.RawText, expected,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                //MVC materialises ONE endpoint per matching action descriptor for a conventional
                //route, plus one "inert" endpoint carrying no descriptor that exists purely for
                //link generation - so the raw count is not the interesting number. What matters is
                //WHICH actions the pattern reaches. Configure is two actions (the GET and the
                //[HttpPost] overload) sharing one name, so the distinct set has one entry.
                var actions = matching
                    .Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
                    .Where(d => d != null)
                    .Select(d => d.ControllerTypeInfo.FullName + "." + d.ActionName)
                    .Distinct()
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();
                sb.AppendLine("endpoint:" + expected + ".actions=" +
                    (actions.Count == 0 ? "<none>" : string.Join("|", actions)));

                var named = matching
                    .Select(e =>
                    {
                        var endpointName = e.Metadata.GetMetadata<IEndpointNameMetadata>();
                        if (endpointName != null && endpointName.EndpointName != null)
                            return endpointName.EndpointName;
                        var routeNameMetadata = e.Metadata.GetMetadata<IRouteNameMetadata>();
                        return routeNameMetadata == null ? null : routeNameMetadata.RouteName;
                    })
                    .Where(n => n != null)
                    .Distinct()
                    .ToList();
                sb.AppendLine("endpoint:" + expected + ".routeNames=" +
                    (named.Count == 0 ? "<none>" : string.Join("|", named)));
            }

            //URL generation by ROUTE NAME - three of HasOneProduct's four routes exist only so the
            //views can call Url.RouteUrl(name), so a renamed route breaks at runtime, silently.
            //This needs the LIVE endpoints, i.e. an installed plugin.
            var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();
            foreach (var routeName in new[]
            {
                "Plugin.DiscountRules.CustomerRoles.Configure",
                "Plugin.DiscountRules.HasOneProduct.Configure",
                "Plugin.DiscountRules.HasOneProduct.ProductAddPopup",
                "Plugin.DiscountRules.HasOneProduct.ProductAddPopupList",
                "Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames",
                //task 11.1 - PublicInfo.cshtml resolves the login button's href by THIS name
                "Plugin.ExternalAuth.Facebook.Login",
                "Plugin.ExternalAuth.Facebook.LoginCallback"
            })
            {
                var url = linkGenerator.GetPathByName(context, routeName, null);
                sb.AppendLine("routeUrl:" + routeName + "=" + (url ?? "<null>"));
            }

            //--- (7) TASK 11.1 - the Html.Action bridge's ACTION SELECTION and MODEL BINDING ----
            WriteChildActionSelectionProbe(context, sb);

            //--- (8) TASK 11.2 - the plugin's own DbContext, runtime deferral 4.10 --------------
            WritePluginDataContextProbe(sb);
        }

        /// <summary>
        /// Task 11.2 — <c>Feed.GoogleShopping</c>'s own <c>DbContext</c>, and runtime deferral
        /// <b>4.10</b>: EF Core's create script is <c>GO</c>-batched and a plugin's
        /// <c>Install()</c> does not pass through <c>Nop.Data</c>'s initializer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>NO DATABASE IS REQUIRED and that is the point.</b>
        /// <c>DbContext.Database.GenerateCreateScript()</c> is a MODEL operation — it needs a
        /// provider selected but never opens a connection — so the whole of deferral 4.10 is
        /// observable with a throwaway connection string. The alternative was to leave the fix
        /// asserted only by an installed-store test, i.e. skipped in this environment.
        /// </para>
        /// <para>
        /// The context type is reached <b>reflectively</b>, through the assembly
        /// <c>PluginManager</c> already shadow-copied and loaded, because
        /// <c>Nop.Web.SmokeTests.csproj</c> references the plugins for BUILD ORDER only — a
        /// compiling reference would put the plugin in this project's output directory, where
        /// <c>WebAppTypeFinder</c> would load it as an ordinary base-directory assembly and bypass
        /// the plugin path under test entirely.
        /// </para>
        /// <para>
        /// It also measures the <b>model scope</b>. <c>OnModelCreating</c> calls
        /// <c>ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())</c>; if that ever
        /// widened to <c>Nop.Data</c>'s assembly the plugin's model would gain all ~105 nopCommerce
        /// entities and <c>Install()</c> would emit a create script for the entire nopCommerce
        /// schema against a live store.
        /// </para>
        /// </remarks>
        private static void WritePluginDataContextProbe(StringBuilder sb)
        {
            const string typeName = "Nop.Plugin.Feed.GoogleShopping.Data.GoogleProductObjectContext";

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Nop.Plugin.Feed.GoogleShopping");
            if (assembly == null)
            {
                sb.AppendLine("pluginContext.assemblyLoaded=False");
                return;
            }
            sb.AppendLine("pluginContext.assemblyLoaded=True");

            var contextType = assembly.GetType(typeName, false);
            sb.AppendLine("pluginContext.typePresent=" + (contextType != null));
            if (contextType == null)
                return;

            //3.90's (string nameOrConnectionString) ctor must still exist: Nop.Web.Framework's
            //RegisterPluginDataContext constructs the type with Activator.CreateInstance and exactly
            //that signature, so losing it fails at RUNTIME with MissingMethodException and no
            //compile error anywhere.
            var stringCtor = contextType.GetConstructor(new[] { typeof(string) });
            sb.AppendLine("pluginContext.stringCtorPresent=" + (stringCtor != null));
            if (stringCtor == null)
                return;

            try
            {
                //A syntactically valid connection string to nowhere. GenerateCreateScript is a model
                //operation and never connects.
                using (var ctx = (Microsoft.EntityFrameworkCore.DbContext)stringCtor.Invoke(
                    new object[] { "Server=(localdb)\\nowhere;Database=nop_11_2_probe;Trusted_Connection=True;" }))
                {
                    var entityTypes = ctx.Model.GetEntityTypes()
                        .Select(e => e.ClrType == null ? e.Name : e.ClrType.Name)
                        .OrderBy(n => n, StringComparer.Ordinal)
                        .ToList();
                    sb.AppendLine("pluginContext.entityTypeCount=" + entityTypes.Count);
                    sb.AppendLine("pluginContext.entityTypes=" + string.Join("|", entityTypes));

                    var tableNames = ctx.Model.GetEntityTypes()
                        .Select(e => Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions.GetTableName(e))
                        .Where(t => t != null)
                        .OrderBy(t => t, StringComparer.Ordinal)
                        .ToList();
                    sb.AppendLine("pluginContext.tableNames=" + string.Join("|", tableNames));

                    var script = Microsoft.EntityFrameworkCore
                        .RelationalDatabaseFacadeExtensions.GenerateCreateScript(ctx.Database);
                    sb.AppendLine("pluginContext.scriptLength=" + script.Length);

                    //THE DEFERRAL: the script really does contain a bare GO line, so 3.90's single
                    //Database.ExecuteSqlCommand(script) would throw "Incorrect syntax near 'GO'".
                    //MEASURED SHAPE, and it is narrower than deferral 4.10's wording suggests: for
                    //this one-table model EF Core emits ONE statement followed by a TRAILING GO, so
                    //the failure is the trailing directive rather than multiple batches. Either way
                    //the raw script is not executable as one command.
                    var goLines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                        .Count(l => string.Equals(l.Trim(), "GO", StringComparison.OrdinalIgnoreCase));
                    sb.AppendLine("pluginContext.scriptGoLineCount=" + goLines);
                    sb.AppendLine("pluginContext.rawScriptWouldBeRejected=" + (goLines > 0));

                    //...and the SHARED helper splits it into executable batches, none of which
                    //still contains a bare GO
                    var batches = Nop.Data.DbContextExtensions.SplitSqlIntoBatches(script).ToList();
                    sb.AppendLine("pluginContext.batchCount=" + batches.Count);
                    sb.AppendLine("pluginContext.batchesWithBareGo=" + batches.Count(b =>
                        b.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                            .Any(l => string.Equals(l.Trim(), "GO", StringComparison.OrdinalIgnoreCase))));
                    sb.AppendLine("pluginContext.batchesCreatingGoogleProduct=" + batches.Count(b =>
                        b.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        b.IndexOf("GoogleProduct", StringComparison.OrdinalIgnoreCase) >= 0));
                }
            }
            catch (Exception exc)
            {
                sb.AppendLine("pluginContext.EXCEPTION=" +
                    (exc.InnerException ?? exc).GetType().FullName + ": " +
                    (exc.InnerException ?? exc).Message);
            }

            //And the helper's own contract, on inputs the generator does not produce, so a future
            //"improvement" to the splitter cannot quietly change what a batch is.
            var crafted = "CREATE TABLE [A] ([Id] int);\nGO\n\nGO\nSELECT 'GO';\nGO";
            var craftedBatches = Nop.Data.DbContextExtensions.SplitSqlIntoBatches(crafted).ToList();
            sb.AppendLine("splitSql.craftedBatchCount=" + craftedBatches.Count);
            sb.AppendLine("splitSql.craftedBatchesKeepStringLiteralGo=" +
                craftedBatches.Count(b => b.Contains("SELECT 'GO'")));
            sb.AppendLine("splitSql.emptyInputBatchCount=" +
                Nop.Data.DbContextExtensions.SplitSqlIntoBatches("   ").Count());
        }

        /// <summary>
        /// Task 11.1 — drives the <c>Html.Action</c> bridge's action-selection and complex-parameter
        /// binding against the <b>real</b> action descriptors and the <b>real</b> request.
        /// Runtime deferral <b>11.x-1</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>WHAT WENT WRONG.</b> A plugin's admin configuration page is rendered through the
        /// bridge, and its <c>Html.BeginForm()</c> posts back to the ADMIN url — so the POST arrives
        /// as another child-action render. The pre-11.1 bridge matched on action + controller NAME
        /// and broke the tie by "fewest parameters", ignoring <c>[HttpPost]</c> and
        /// <c>[FormValueRequired]</c>, and it handed every COMPLEX parameter <c>null</c>. So a
        /// plugin settings form silently re-rendered the GET with the old values and discarded the
        /// administrator's input. Every plugin with a settings form is affected: 11.1, 11.2, all
        /// five of 12.x, 13.1, 14.4, 15.1, 15.2 and 15.3.
        /// </para>
        /// <para>
        /// <b>WHY THIS IS A PROBE AND NOT A UNIT TEST.</b> The two mechanisms are private statics
        /// inside <c>ChildActionExtensions</c>, and the inputs that make them meaningful — the
        /// application's real <c>ControllerActionDescriptor</c> set, with its
        /// <c>HttpMethodActionConstraint</c> and <c>FormValueRequiredAttribute</c> entries, plus a
        /// live <c>HttpContext</c> whose method and form body are the thing being reacted to — only
        /// exist inside the running host. Reflection is used deliberately rather than widening the
        /// public surface of a shared framework file for a test's benefit. The code executed is the
        /// production code, not a copy.
        /// </para>
        /// <para>
        /// The <b>selection</b> half is reported for whatever request the test makes, so
        /// <c>PluginViewRenderTests</c> can call <c>/__smoke/plugins</c> as a GET and as two
        /// different POSTs and compare — which is what proves the mechanism reacts to the request
        /// rather than returning a constant. The <b>binding</b> half reports the values it recovered
        /// from the form, and reports the value-provider-factory count that
        /// <c>PopulateValueProviderFactories</c> is responsible for: a hand-built
        /// <c>ControllerContext</c> starts with ZERO, and with zero every bind silently succeeds
        /// having read nothing.
        /// </para>
        /// </remarks>
        private static void WriteChildActionSelectionProbe(HttpContext context, StringBuilder sb)
        {
            var bridge = typeof(Nop.Web.Framework.ChildActionExtensions);

            //FindAction is the REAL entry point: the area preference, the constraint filter and the
            //fewest-parameters tie-break in the order InvokeAction applies them. The probe drives
            //THIS rather than ApplyActionConstraints directly, because a private helper can be
            //present and correct while its call site is missing - which is exactly the shape of
            //vacuous assertion task 8.8 §77.5 found.
            var findAction = bridge.GetMethod("FindAction",
                BindingFlags.NonPublic | BindingFlags.Static);
            var applyConstraints = bridge.GetMethod("ApplyActionConstraints",
                BindingFlags.NonPublic | BindingFlags.Static);
            sb.AppendLine("childAction.findActionPresent=" + (findAction != null));
            sb.AppendLine("childAction.applyActionConstraintsPresent=" + (applyConstraints != null));

            var provider = context.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            sb.AppendLine("childAction.requestMethod=" + context.Request.Method);
            sb.AppendLine("childAction.hasFormContentType=" + context.Request.HasFormContentType);

            //Report the raw constraint metadata first. If MVC ever stopped materialising these the
            //selection fix would silently become a no-op, and the assertion above would still pass.
            foreach (var pair in new[]
            {
                new[] { "ExternalAuthFacebook", "Configure" },
                new[] { "FeedGoogleShopping", "Configure" }
            })
            {
                var candidates = provider.ActionDescriptors.Items
                    .OfType<ControllerActionDescriptor>()
                    .Where(d => d.ControllerName == pair[0] && d.ActionName == pair[1])
                    .OrderBy(d => d.MethodInfo.Name, StringComparer.Ordinal)
                    .ThenBy(d => d.Parameters.Count)
                    .ToList();

                var key = "childAction:" + pair[0] + "." + pair[1];
                sb.AppendLine(key + ".candidateCount=" + candidates.Count);
                foreach (var d in candidates)
                {
                    var constraints = d.ActionConstraints == null
                        ? new List<string>()
                        : d.ActionConstraints.Select(c => c.GetType().Name).OrderBy(x => x, StringComparer.Ordinal).ToList();
                    sb.AppendLine(key + ".candidate=" + d.MethodInfo.Name + "(" +
                        string.Join(",", d.Parameters.Select(p => p.ParameterType.Name)) + ")" +
                        " constraints=" + (constraints.Count == 0 ? "<none>" : string.Join("|", constraints)));
                }

                if (findAction == null)
                    continue;

                //Drive the real selection with the real request. areaName = "" because a plugin
                //controller is not in the Admin area, which is what the plugin contracts'
                //{ "area", null } route value expresses.
                try
                {
                    var selected = (ControllerActionDescriptor)findAction.Invoke(null,
                        new object[] { context, pair[1], pair[0], string.Empty });
                    sb.AppendLine(key + ".selected=" + (selected == null
                        ? "<none>"
                        : selected.MethodInfo.Name + "/" + selected.Parameters.Count));
                }
                catch (Exception exc)
                {
                    sb.AppendLine(key + ".selected=<threw:" +
                        (exc.InnerException ?? exc).GetType().Name + ">");
                }
            }

            //--- the binding half -------------------------------------------------------------
            //DRIVEN THROUGH THE PRODUCTION CALL PATH, not through the helpers directly. CreateController
            //is what installs the value provider factories (a hand-built ControllerContext has ZERO,
            //and with zero every bind silently succeeds having read nothing) and ExecuteAction is what
            //reaches BindParameter -> BindComplexParameter. Invoking the helpers directly would leave
            //their CALL SITES unexercised, which is the vacuous-assertion trap task 8.8 §77.5 found.
            var createController = bridge.GetMethod("CreateController",
                BindingFlags.NonPublic | BindingFlags.Static);
            var executeAction = bridge.GetMethod("ExecuteAction",
                BindingFlags.NonPublic | BindingFlags.Static);
            var bindParameter = bridge.GetMethod("BindParameter",
                BindingFlags.NonPublic | BindingFlags.Static);
            sb.AppendLine("childAction.populateValueProviderFactoriesPresent=" +
                (bridge.GetMethod("PopulateValueProviderFactories",
                    BindingFlags.NonPublic | BindingFlags.Static) != null));
            sb.AppendLine("childAction.bindParameterArity=" +
                (bindParameter == null ? -1 : bindParameter.GetParameters().Length));

            if (createController == null || executeAction == null)
                return;

            //A hand-made descriptor for the probe controller, shaped exactly as MVC shapes one.
            var probeMethod = typeof(ChildActionBindProbeController).GetMethod("BindProbe");
            var probeDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(ChildActionBindProbeController).GetTypeInfo(),
                MethodInfo = probeMethod,
                ControllerName = "ChildActionBindProbe",
                ActionName = "BindProbe",
                Parameters = new List<Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor>
                {
                    new ControllerParameterDescriptor
                    {
                        Name = probeMethod.GetParameters()[0].Name,
                        ParameterType = typeof(ChildActionBindProbeModel),
                        ParameterInfo = probeMethod.GetParameters()[0]
                    }
                },
                RouteValues = new Dictionary<string, string>()
            };

            try
            {
                var actionContext = new ActionContext(context, new RouteData(), probeDescriptor);
                var controller = (ChildActionBindProbeController)createController
                    .Invoke(null, new object[] { context.RequestServices, probeDescriptor, actionContext });

                sb.AppendLine("childAction.valueProviderFactories=" +
                    controller.ControllerContext.ValueProviderFactories.Count);

                //the real ExecuteAction, i.e. the real BindParameter for a COMPLEX parameter
                executeAction.Invoke(null,
                    new object[] { controller, probeDescriptor, new RouteValueDictionary() });

                sb.AppendLine("childAction.bindParameterResult=" +
                    (controller.Received == null ? "<null>" : controller.Received.GetType().Name));
                sb.AppendLine("childAction.boundText=" +
                    (controller.Received == null ? "<null>" : (controller.Received.ProbeText ?? "<null>")));
                sb.AppendLine("childAction.boundNumber=" +
                    (controller.Received == null ? -1 : controller.Received.ProbeNumber));
            }
            catch (Exception exc)
            {
                sb.AppendLine("childAction.bindParameterResult=<threw:" +
                    (exc.InnerException ?? exc).GetType().Name + ": " +
                    (exc.InnerException ?? exc).Message + ">");
            }
        }

        /// <summary>
        /// Instantiates each plugin's own <c>IRouteProvider</c> and drives it against a scratch
        /// <see cref="IEndpointRouteBuilder"/> built over the real service provider, returning the
        /// endpoints it produced.
        /// </summary>
        /// <remarks>
        /// This exists so the <c>IRouteProvider</c> port can be asserted <b>without an installed
        /// store</b>. <c>RoutePublisher</c> deliberately skips providers belonging to uninstalled
        /// plugins (3.90's behaviour), so the live <c>EndpointDataSource</c> carries no plugin route
        /// in install mode — which is correct, and would leave the port itself unexercised. Driving
        /// the provider directly measures the thing task 10.x changed: the pattern, the route name,
        /// the defaults, and that the endpoint resolves to the plugin's controller action.
        /// The scratch builder is discarded; nothing is added to the running application.
        /// </remarks>
        private static List<RouteEndpoint> RegisterPluginRoutesIntoScratchBuilder(
            HttpContext context, IEnumerable<string> assemblyNames, StringBuilder sb)
        {
            var result = new List<RouteEndpoint>();
            var providerInterface = typeof(Nop.Web.Framework.Mvc.Routes.IRouteProvider);

            foreach (var assemblyName in assemblyNames)
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);
                if (assembly == null)
                    continue;

                var providerTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && providerInterface.IsAssignableFrom(t))
                    .ToList();
                sb.AppendLine("routeProviders:" + assemblyName + ".count=" + providerTypes.Count);

                foreach (var providerType in providerTypes)
                {
                    var provider = (Nop.Web.Framework.Mvc.Routes.IRouteProvider)
                        Activator.CreateInstance(providerType);
                    sb.AppendLine("routeProviders:" + assemblyName + ".priority=" + provider.Priority);

                    var scratch = new ScratchEndpointRouteBuilder(context.RequestServices);
                    provider.RegisterRoutes(scratch);
                    foreach (var dataSource in scratch.DataSources)
                        result.AddRange(dataSource.Endpoints.OfType<RouteEndpoint>());
                }
            }

            return result;
        }

        /// <summary>
        /// A throwaway <see cref="IEndpointRouteBuilder"/> over the host's real service provider.
        /// </summary>
        private class ScratchEndpointRouteBuilder : IEndpointRouteBuilder
        {
            public ScratchEndpointRouteBuilder(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; }
            public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
            public IApplicationBuilder CreateApplicationBuilder()
            {
                return new ApplicationBuilder(ServiceProvider);
            }
        }

        /// <summary>
        /// The <c>_ViewStart.cshtml</c> paths ASP.NET Core would consider for a view identifier,
        /// from the view's own directory upwards to the application root — the same walk
        /// <c>RazorViewEngine.GetViewStartPages</c> performs.
        /// </summary>
        private static IEnumerable<string> ViewStartAncestorsOf(string viewIdentifier)
        {
            var dir = viewIdentifier;
            while (true)
            {
                var slash = dir.LastIndexOf('/');
                if (slash < 0)
                    yield break;
                dir = dir.Substring(0, slash);
                yield return dir + "/_ViewStart.cshtml";
                if (dir.Length == 0)
                    yield break;
            }
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
