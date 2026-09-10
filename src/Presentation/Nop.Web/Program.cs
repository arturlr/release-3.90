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
            app.UseStatusCodePagesWithReExecute("/page-not-found");

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
        /// Runtime deferral 4.8 — creates the nopCommerce schema when the target database is
        /// empty.
        /// </summary>
        /// <remarks>
        /// <para>
        /// EF6 registered a <b>global, lazily fired</b> hook via
        /// <c>Database.SetInitializer(initializer)</c>: the first time any
        /// <c>NopObjectContext</c> was used, EF6 ran
        /// <c>CreateTablesIfNotExist.InitializeDatabase(context)</c>. EF Core deleted the
        /// initializer concept entirely, so <c>SqlServerDataProvider.SetDatabaseInitializer()</c>
        /// now only <i>publishes</i> the configured initializer on the static
        /// <c>SqlServerDataProvider.DatabaseInitializer</c> and <b>nothing calls it</b>. Left
        /// unfixed, installing onto an empty database silently creates no schema and the first
        /// real query fails with "Invalid object name".
        /// </para>
        /// <para>
        /// <b>This must run after <see cref="NopHostedEngine.RunStartupTasks(Nop.Core.Configuration.NopConfig)"/></b>:
        /// <c>Nop.Data</c>'s <c>EfStartUpTask</c> (Order -1000) is what calls
        /// <c>SetDatabaseInitializer()</c>, so the property is null until startup tasks have run.
        /// </para>
        /// <para>
        /// The initializer is idempotent and cheap on an already-installed store: it probes
        /// <c>INFORMATION_SCHEMA.TABLES</c> for <c>Customer</c>/<c>Discount</c>/<c>Order</c>/
        /// <c>Product</c>/<c>ShoppingCartItem</c> and returns immediately when any of them exists.
        /// </para>
        /// <para>
        /// <b>NEW behaviour, recorded as deferral 7.2-1:</b> exceptions are deliberately NOT
        /// swallowed. EF6 deferred the failure to the first query; here an unreachable or
        /// misconfigured database fails the host at startup. That is the honest outcome — a store
        /// with no schema cannot serve anything — but it does mean a supervised process will
        /// restart-loop instead of serving an error page.
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

        /// <summary>
        /// 3.90's <c>Application_Start</c>: <c>TaskManager.Instance.Initialize()</c> +
        /// <c>Start()</c>, guarded by the same "database installed" check.
        /// </summary>
        /// <remarks>
        /// Kept as-is rather than converted to an <c>IHostedService</c>. <c>TaskManager</c> owns
        /// its own <c>TaskThread</c>/<c>System.Threading.Timer</c> machinery and lives in
        /// <c>Nop.Services</c>, which has passed its clean-compile gate; rebasing it onto the
        /// generic host's background-service model is a behavioural change beyond this task.
        /// Recorded as deferral 7.2-3: nothing calls <c>TaskManager.Instance.Stop()</c> on
        /// shutdown, exactly as in 3.90, where <c>Application_End</c> did not call it either.
        /// </remarks>
        private static void StartScheduledTasks()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            TaskManager.Instance.Initialize();
            TaskManager.Instance.Start();
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
