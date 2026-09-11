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
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
        /// <para>Query string: <c>?controller=Common&amp;action=Footer</c>.</para>
        /// </remarks>
        private static void WriteActionProbe(HttpContext context, StringBuilder sb)
        {
            var controllerName = context.Request.Query["controller"].ToString();
            var actionName = context.Request.Query["action"].ToString();
            sb.AppendLine("query=" + controllerName + "." + actionName);

            //(a) the endpoint table - every endpoint built for this action, from the Default
            //conventional route and from any explicit MapControllerRoute that targets it.
            var sources = context.RequestServices.GetServices<EndpointDataSource>();
            var endpoints = sources.SelectMany(s => s.Endpoints)
                .Where(e =>
                {
                    var d = e.Metadata.GetMetadata<ControllerActionDescriptor>();
                    return d != null &&
                           string.Equals(d.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(d.ActionName, actionName, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            sb.AppendLine("endpointCount=" + endpoints.Count);
            var matchable = 0;
            foreach (var endpoint in endpoints)
            {
                var suppressed = endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>();
                var isSuppressed = suppressed != null && suppressed.SuppressMatching;
                if (!isSuppressed)
                    matchable++;

                var route = endpoint as RouteEndpoint;
                sb.AppendLine("endpoint pattern=" + (route == null ? "<not-a-route>" : route.RoutePattern.RawText)
                    + " suppressMatching=" + isSuppressed);
            }
            sb.AppendLine("matchableEndpointCount=" + matchable);

            //(b) the descriptor collection Html.Action's bridge queries - MUST still contain it.
            var provider = context.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            var descriptors = provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(d => string.Equals(d.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.ActionName, actionName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            sb.AppendLine("actionDescriptorCount=" + descriptors.Count);
            sb.AppendLine("visibleToChildActionBridge=" + (descriptors.Count > 0));
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
