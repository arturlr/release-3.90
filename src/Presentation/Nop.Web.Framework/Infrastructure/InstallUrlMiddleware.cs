using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// Redirects every request to <c>~/install</c> until the database is installed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 6.4: this is the install-mode branch of 3.90's
    /// <c>Nop.Web/Global.asax.cs</c> <c>Application_BeginRequest</c>. <c>HttpApplication</c>
    /// events have no ASP.NET Core counterpart; the pipeline stage that corresponds to
    /// <c>BeginRequest</c> is middleware placed early, before routing.
    /// </para>
    /// <para>
    /// <b>Both early-exit guards from 3.90 are preserved</b>: static resources are skipped
    /// (via <see cref="IWebHelper.IsStaticResource"/>, and in practice they never reach here
    /// because <c>UseStaticFiles()</c> is registered earlier), and the keep-alive URL is
    /// skipped so it does not create guest customer records.
    /// </para>
    /// <para>
    /// <b>Not ported:</b> the MiniProfiler start/stop that shared
    /// <c>Application_BeginRequest</c>/<c>Application_EndRequest</c>. <c>MiniProfiler</c> /
    /// <c>StackExchange.Profiling.Mvc</c> is MVC5-only and was not carried into the migration
    /// (it is not a <c>PackageReference</c> of any migrated project). See the report for
    /// task 7.2.
    /// </para>
    /// </remarks>
    public class InstallUrlMiddleware
    {
        private readonly RequestDelegate _next;

        public InstallUrlMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                await _next(context);
                return;
            }

            var webHelper = EngineContext.Current.Resolve<IWebHelper>();

            //ignore static resources
            if (webHelper.IsStaticResource(context.Request))
            {
                await _next(context);
                return;
            }

            //keep alive page requested (we ignore it to prevent creating a guest customer records)
            var keepAliveUrl = string.Format("{0}keepalive/index", webHelper.GetStoreLocation());
            var thisPageUrl = webHelper.GetThisPageUrl(false);
            if (thisPageUrl.StartsWith(keepAliveUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                await _next(context);
                return;
            }

            var installUrl = string.Format("{0}install", webHelper.GetStoreLocation());
            if (!thisPageUrl.StartsWith(installUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Redirect(installUrl);
                    return;
                }
            }

            await _next(context);
        }
    }
}
