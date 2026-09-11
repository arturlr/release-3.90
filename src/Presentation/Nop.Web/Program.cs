using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Infrastructure;

namespace Nop.Web
{
    /// <summary>
    /// Application entry point — the ASP.NET Core replacement for 3.90's
    /// <c>Global.asax</c>/<c>Global.asax.cs</c> (task 7.2, Requirements 4.1, 4.5, 4.6).
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //--------------------------------------------------------------------------
            // Autofac (Requirement 4.5)
            //--------------------------------------------------------------------------
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            //--------------------------------------------------------------------------
            // Pre-container static seams — runtime deferrals 1.5, 1.4, 1.1, in that order
            //--------------------------------------------------------------------------
            builder.Environment.UseNopHostingEnvironment(builder.Configuration);

            //--------------------------------------------------------------------------
            // Engine
            //--------------------------------------------------------------------------
            var engine = new NopHostedEngine();
            EngineContext.Replace(engine);

            var nopConfig = NopConfigurationManager.GetNopConfig();

            //--------------------------------------------------------------------------
            // Services
            //--------------------------------------------------------------------------
            //AddNopFramework closes runtime deferrals 1.2, 7.13, 7.14, 7.15, 11.20, 11.21,
            //11.22, 11.23, 11.26, 11.29, 14.30 and 14.31 - see NopServiceCollectionExtensions.
            //The IConfiguration overload is used deliberately: it binds NopAuthenticationConfig
            //from the "Authentication" section instead of hardcoding requireSsl, which deferral
            //7.13 flags as a security hazard.
            builder.Services.AddNopFramework(builder.Configuration,
                configureMvc: options => options.ModelMetadataDetailsProviders.Add(
                    new SuppressImplicitRequiredValueTypeMetadataProvider()));

            builder.Host.ConfigureContainer<ContainerBuilder>(
                containerBuilder => engine.RegisterInto(containerBuilder, nopConfig));

            //--------------------------------------------------------------------------
            // Build
            //--------------------------------------------------------------------------
            var app = builder.Build();

            //--------------------------------------------------------------------------
            // The rest of Application_Start, now that the container exists
            //--------------------------------------------------------------------------
            engine.RunStartupTasks(nopConfig);

            InitializeDatabaseSchema();
            StartScheduledTasks();
            LogApplicationStart();

            //--------------------------------------------------------------------------
            // Request pipeline. THE ORDER IS A HARD CONSTRAINT - see
            // NopApplicationBuilderExtensions and runtime-deferrals.md section 17.7.
            //--------------------------------------------------------------------------

            //runtime deferral 11.29 - IWebHelper.IsCurrentConnectionSecured() reads
            //HttpRequest.IsHttps, which is false behind a TLS-terminating proxy. Without this,
            //SeoSettings.ForceSslForAllPages produces a redirect LOOP. Options were configured
            //by AddNopFramework().
            app.UseForwardedHeaders();

            //Application_Error, part 1 of 3: presentation of unhandled exceptions.
            //3.90: <customErrors defaultRedirect="errorpage.htm" mode="RemoteOnly"/>.
            //"RemoteOnly" == detailed errors locally, the static page remotely.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "text/html";

                    //~/ErrorPage.htm is the file <customErrors defaultRedirect> named. It is
                    //still at its 3.90 location in the content root (deferral 7.1-5), so it is
                    //sent directly rather than resolved through the static-file middleware.
                    var errorPagePath = CommonHelper.MapPath("~/ErrorPage.htm");
                    if (File.Exists(errorPagePath))
                        await context.Response.SendFileAsync(errorPagePath);
                    else
                        await context.Response.WriteAsync("<html><body>An error occurred while processing your request.</body></html>");
                }));
            }

            //Application_Error, part 2 of 3: logging, including the CommonSettings.Log404Errors
            //rule that only makes sense once a 404 is a status code rather than an exception.
            app.UseNopErrorLogging();

            //Application_Error, part 3 of 3: 404 -> CommonController.PageNotFound. Replaces the
            //Response.Clear() / Server.ClearError() / errorController.Execute(routeData) block.
            //"/page-not-found" is the route RouteProvider registers for Common/PageNotFound.
            //
            //deferral 7.7-1: this was a bare UseStatusCodePagesWithReExecute("/page-not-found"),
            //which fires for ANY bodiless 400-599 - so every antiforgery refusal (a bodiless 400
            //from PublicAntiForgeryAttribute) was rewritten to "404 Page not found". The
            //replacement re-executes for a 404 and ONLY for a 404; see NopStatusCodePagesExtensions.
            app.UseNopStatusCodePages();

            //<system.webServer> static handling, 3.90's "ignore static resources" early exit in
            //Application_BeginRequest, and the DenyAccessToPluginDLLs HttpForbiddenHandler (.dll
            //is not in the default content-type map, so static files refuses it and it 404s).
            //
            //task 7.4 replaced the bare UseStaticFiles() with this call, at the SAME pipeline
            //position. It does three things a bare call cannot (see NopStaticFilesExtensions):
            //  * runtime deferral 40/7.1-5 - repoints IWebHostEnvironment.WebRootFileProvider at
            //    an ALLOW-LISTED provider over the content root, because 3.90's static assets
            //    (~/Content, ~/Scripts, ~/Themes/<theme>/Content, ~/favicon.ico) are still at
            //    their 3.90 locations and there is no wwwroot - so until now NOTHING served.
            //    The allow-list is what keeps App_Data/Settings.txt (the database connection
            //    string), *.cshtml, Web.config and Administration/db_backups/*.bak out of it -
            //    System.Web blocked App_Data implicitly and ASP.NET Core does not (deferral 39).
            //  * runtime deferral 33/14.33 - because it is the SAME provider instance on
            //    WebRootFileProvider, IFileVersionProvider can see every asset it serves, so
            //    PageHeadBuilder's "?v=<hash>" cache busting actually fires instead of silently
            //    returning unversioned URLs.
            //  * reproduces <staticContent>: the 7-day <clientCache> max-age and the <mimeMap>
            //    additions. NOT the .bak mapping - see the note in CreateNopStaticFileOptions.
            app.UseNopStaticFiles();

            //install redirect, SEO-friendly URLs, routing, slug redirect, session,
            //authentication, working culture, authorization, endpoints - in that exact order.
            app.UseNopPipeline();

            app.Run();
        }

        /// <summary>
        /// Runtime deferral 4.8 — safety net that creates the nopCommerce schema when
        /// <c>App_Data/Settings.txt</c> names a database that has none.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TASK 7.7 CORRECTION — READ THIS FIRST.</b> This method was recorded as the fix for
        /// runtime deferral 4.8 ("the schema initializer is never invoked, so a fresh install
        /// creates no tables"). <b>It is not, and never was.</b> It opens with
        /// <c>if (!DataSettingsHelper.DatabaseIsInstalled()) return;</c> and runs exactly once, at
        /// host startup — but the case deferral 4.8 describes is <i>installing onto an empty
        /// database</i>, and at startup a not-yet-installed store has no data-settings file, so the
        /// method early-returns. By the time the installer has written <c>Settings.txt</c>, host
        /// startup is long past. On every later start the store IS installed and the initializer
        /// short-circuits on its own table probe. The call was inert in both directions.
        /// </para>
        /// <para>
        /// Task 7.7 measured this by POSTing the real installer form at a real SQL Server: it
        /// failed with <c>Entity: Store State: Added … Invalid object name 'Store'.</c> — deferral
        /// 4.8's predicted symptom, verbatim. The real fix is in
        /// <c>Nop.Data.SqlServerDataProvider.InitDatabase()</c>, which is what the installer calls
        /// and what EF6's <c>Database.SetInitializer</c> hook used to fire from; see the remarks
        /// there.
        /// </para>
        /// <para>
        /// This method is <b>kept</b> as a safety net for one residual case the installer cannot
        /// cover: a hand-written or copied <c>Settings.txt</c> pointing at an empty database. The
        /// initializer is idempotent and cheap on a provisioned store (it probes
        /// <c>INFORMATION_SCHEMA.TABLES</c> for <c>Customer</c>/<c>Discount</c>/<c>Order</c>/
        /// <c>Product</c>/<c>ShoppingCartItem</c> and returns immediately when any exists).
        /// </para>
        /// <para>
        /// <b>Deferral 7.2-1 RESOLVED here, and the decision is a reversal.</b> Task 7.2 chose
        /// deliberately not to swallow exceptions, so an unreachable database took the host down at
        /// startup, and left the accept-or-defer decision to task 7.7. Task 7.7 verified the
        /// behaviour — a <c>Settings.txt</c> naming a non-existent host produced
        /// <c>Unhandled exception. Nop.Core.NopException: No database instance</c> and process exit
        /// code 134 — and chose to make it non-fatal, because the argument for failing fast no
        /// longer holds:
        /// <list type="bullet">
        /// <item>Provisioning is now owned by the installation path, so this call is a net rather
        /// than the mechanism, and a net must not be more dangerous than what it guards.</item>
        /// <item>The throw does not distinguish "empty database" from "database briefly
        /// unreachable". The second is a transient operational condition, and under ANCM, systemd
        /// or an orchestrator it produces a restart loop with the real cause buried in a crash
        /// log.</item>
        /// <item>3.90 served an error page in this situation; it did not fail to boot. A genuinely
        /// broken database still surfaces loudly on the first request, through
        /// <see cref="Nop.Web.Infrastructure.NopErrorLoggingMiddleware"/> and the configured error
        /// page.</item>
        /// </list>
        /// </para>
        /// </remarks>
        private static void InitializeDatabaseSchema()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var initializer = SqlServerDataProvider.DatabaseInitializer;
            if (initializer == null)
                return;

            var containerManager = EngineContext.Current.ContainerManager;

            try
            {
                //an explicit, disposed scope: IDbContext is InstancePerLifetimeScope and there is no
                //ambient request scope at startup, so ContainerManager.Scope() would open one that
                //nothing ever disposes.
                using (var scope = containerManager.Container.BeginLifetimeScope())
                {
                    var context = containerManager.Resolve<IDbContext>(scope: scope) as NopObjectContext;
                    if (context == null)
                        return;

                    initializer.InitializeDatabase(context);
                }
            }
            catch (Exception)
            {
                //deferral 7.2-1, resolved as described above: a database that is unreachable at
                //startup must not prevent the host from starting. The condition is reported on the
                //first request instead.
            }
        }

        /// <summary>
        /// 3.90's <c>Application_Start</c>: <c>TaskManager.Instance.Initialize()</c> +
        /// <c>Start()</c>, guarded by the same "database installed" check.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Kept as-is rather than converted to an <c>IHostedService</c>. <c>TaskManager</c> owns
        /// its own <c>TaskThread</c>/<c>System.Threading.Timer</c> machinery and lives in
        /// <c>Nop.Services</c>, which has passed its clean-compile gate; rebasing it onto the
        /// generic host's background-service model is a behavioural change beyond this task.
        /// Recorded as deferral 7.2-3: nothing calls <c>TaskManager.Instance.Stop()</c> on
        /// shutdown, exactly as in 3.90, where <c>Application_End</c> did not call it either.
        /// </para>
        /// <para>
        /// <b>TASK 7.7 — the try/catch is new, and it is the SECOND half of deferral 7.2-1.</b>
        /// 7.2-1 attributed "startup now fails fast on an unreachable database" solely to
        /// <see cref="InitializeDatabaseSchema"/>. Task 7.7 made that call non-fatal and measured
        /// again: the host still died, now here —
        /// <c>Program.StartScheduledTasks → TaskManager.Initialize → ScheduleTaskService.GetAllTasks</c>
        /// → <c>InvalidOperationException</c> (EF Core's transient-failure wrapper), process exit
        /// code 134. <c>TaskManager.Initialize()</c> reads the <c>ScheduleTask</c> table, so it
        /// cannot succeed while the database is unreachable.
        /// </para>
        /// <para>
        /// This is <b>not</b> 3.90 parity, which is why it is guarded. In System.Web a throw from
        /// <c>Application_Start</c> failed the request that triggered it and ASP.NET re-ran
        /// <c>Application_Start</c> on the next one — the worker process survived and the site
        /// recovered by itself once the database came back. An exception out of
        /// <c>Program.Main</c> terminates the process, so under ANCM/systemd/an orchestrator the
        /// same transient outage becomes a restart loop.
        /// </para>
        /// <para>
        /// The cost of swallowing is bounded and visible: scheduled tasks do not start for the
        /// lifetime of this process, and an operator must restart it once the database is healthy.
        /// That is strictly better than not serving at all, and it matches what 3.90 did on the
        /// first request after a failed <c>Application_Start</c>.
        /// </para>
        /// </remarks>
        private static void StartScheduledTasks()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            try
            {
                TaskManager.Instance.Initialize();
                TaskManager.Instance.Start();
            }
            catch (Exception)
            {
                //deferral 7.2-1: an unreachable database must not stop the host from starting.
            }
        }

        /// <summary>
        /// 3.90's <c>Application_Start</c> "log application start", including its
        /// swallow-everything guard.
        /// </summary>
        private static void LogApplicationStart()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            try
            {
                var logger = EngineContext.Current.Resolve<Nop.Services.Logging.ILogger>();
                logger.Information("Application started", null, null);
            }
            catch (Exception)
            {
                //don't throw new exception if occurs
            }
        }
    }
}
