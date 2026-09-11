using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Boots the real <c>Nop.Web</c> host in-process for task 7.7's smoke check.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This starts the REAL application.</b> <see cref="WebApplicationFactory{TEntryPoint}"/>
    /// resolves <c>Nop.Web.Program.Main</c> through
    /// <c>Microsoft.Extensions.Hosting.HostFactoryResolver</c> and invokes it on a background
    /// thread with <c>stopApplication: false</c>, so <c>Main</c> runs to completion of
    /// <c>app.Run()</c>: every line of the real <c>Program.cs</c> executes, in order — the
    /// pre-container static seams, <c>AddNopFramework</c>, <c>AutofacServiceProviderFactory</c>,
    /// <c>NopHostedEngine.RegisterInto</c>/<c>RunStartupTasks</c>, then
    /// <c>UseForwardedHeaders</c> → error handling → <c>UseNopStaticFiles</c> →
    /// <c>UseNopPipeline</c>. Only the <c>IServer</c> is substituted (Kestrel →
    /// <c>TestServer</c>), so no socket is opened and nothing survives disposal.
    /// </para>
    /// <para>
    /// <b>No change was required in <c>Program.cs</c> for this to work.</b> The brief allowed a
    /// minimal accommodation (e.g. a <c>partial</c> declaration); none was needed, because
    /// <c>Nop.Web.Program</c> is already a <c>public class</c> with a conventional
    /// <c>public static void Main(string[] args)</c>, which is exactly what the host factory
    /// resolver looks for. <b>Task 7.7 modified no production source file to enable testing.</b>
    /// </para>
    /// <para>
    /// <b>The content root is set explicitly and then asserted, not assumed.</b>
    /// <c>Microsoft.AspNetCore.Mvc.Testing</c> ships MSBuild targets that emit a
    /// <c>WebApplicationFactoryContentRootAttribute</c> for each referenced web project, and
    /// <see cref="WebApplicationFactory{TEntryPoint}"/> will use it — but nopCommerce's content
    /// root is load-bearing far beyond static files: <c>UseNopHostingEnvironment</c> assigns it
    /// to <c>CommonHelper.BaseDirectory</c> (runtime deferral 1.5), which is what
    /// <c>MapPath("~/App_Data/…")</c>, <c>DataSettingsManager</c> (the connection string),
    /// <c>PluginManager</c>'s shadow copy directory, <c>ThemeProvider</c> and
    /// <c>NopStaticFileProvider</c> all resolve against. A silently wrong content root would
    /// make most of this suite pass vacuously — plugin discovery would find nothing, the
    /// database would look uninstalled, and no static file would serve. So the value is pinned
    /// through <c>ASPNETCORE_CONTENTROOT</c> (which <c>WebApplication.CreateBuilder</c> reads
    /// from the environment before anything else) and
    /// <see cref="HostAndContainerTests"/> asserts the resolved
    /// <c>IWebHostEnvironment.ContentRootPath</c> equals it.
    /// </para>
    /// </remarks>
    public class NopWebApplicationFactory : WebApplicationFactory<Program>
    {
        private static readonly object EnvLock = new object();
        private static string _contentRoot;

        /// <summary>
        /// The absolute path of <c>src/Presentation/Nop.Web</c>, located by walking up from the
        /// test assembly rather than hard-coded, so the suite works both on a developer machine
        /// and inside the <c>mcr.microsoft.com/dotnet/sdk:10.0</c> container the migration builds
        /// in (where the repository is mounted at <c>/workspace</c>).
        /// </summary>
        public static string ResolveNopWebContentRoot()
        {
            if (_contentRoot != null)
                return _contentRoot;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Presentation", "Nop.Web", "Nop.Web.csproj");
                if (File.Exists(candidate))
                {
                    _contentRoot = Path.GetDirectoryName(candidate);
                    return _contentRoot;
                }
                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate src/Presentation/Nop.Web by walking up from " + AppContext.BaseDirectory +
                ". The smoke check needs the real Nop.Web content root - see the remarks on NopWebApplicationFactory.");
        }

        public NopWebApplicationFactory()
        {
            var contentRoot = ResolveNopWebContentRoot();

            //Set BEFORE the host is created. WebApplication.CreateBuilder(args) reads
            //ASPNETCORE_CONTENTROOT from the environment while computing IWebHostEnvironment,
            //which is the value Program.cs then hands to UseNopHostingEnvironment. Doing this
            //through IWebHostBuilder.UseContentRoot instead would be too late for the
            //pre-container seams.
            lock (EnvLock)
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_CONTENTROOT", contentRoot);

                //Production, not Development, on purpose: Development would install
                //UseDeveloperExceptionPage, which converts an unhandled exception into a 500
                //with a diagnostic body. That is convenient for debugging but it would let the
                //suite report "200/500 as expected" for pages that are in fact throwing. In
                //Production the app takes its real error path (ErrorPage.htm), and
                //AllowAutoRedirect=false plus explicit status assertions do the rest.
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(ResolveNopWebContentRoot());

            //The ONLY modification made to the application under test: one IStartupFilter that
            //prepends SmokeProbeMiddleware. It is a pure addition - the middleware handles the
            ///__smoke/* prefix and passes everything else straight through, so no other request
            //observes a different pipeline. It exists because several things task 7.7 must verify
            //(per-request Autofac scope sharing, IUserAgentHelper, LinkGenerator route
            //generation) are only observable from INSIDE a live request, and there is no
            //nopCommerce controller that exposes them.
            builder.ConfigureTestServices(services =>
                services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, SmokeProbeStartupFilter>());
        }
    }
}
