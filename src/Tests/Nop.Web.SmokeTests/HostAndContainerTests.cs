using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Plugins;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Themes;
using Nop.Web.Framework.UI;
using Nop.Web.Infrastructure;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 7.7, group A — the host, the Autofac container, the pre-container static seams and
    /// configuration binding. None of these needs a database.
    /// </summary>
    /// <remarks>
    /// Every assertion here is <b>verified by execution of the real host</b>, not by inspection.
    /// The fixture-level <see cref="OneTimeSetUp"/> is itself the first assertion: if
    /// <c>Program.Main</c> throws, or the Autofac container fails to build, or a startup task
    /// fails, no test in this fixture can run.
    /// </remarks>
    [TestFixture]
    public class HostAndContainerTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            //Forces the host to be built and Program.Main to run to app.Run().
            _client = _factory.CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.1 / 4.6 — the host starts
        // -----------------------------------------------------------------------------------

        [Test]
        public void Host_starts_and_serves_a_request()
        {
            //Reaching this line at all means Program.Main ran through app.Run(). Prove the server
            //is live rather than merely constructed.
            var response = _client.GetAsync(SmokeProbeMiddleware.Prefix + "ok").Result;
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            StringAssert.Contains("probe=ok", response.Content.ReadAsStringAsync().Result);
        }

        [Test]
        public void Content_root_is_the_real_Nop_Web_directory()
        {
            var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
            Assert.AreEqual(
                Path.GetFullPath(NopWebApplicationFactory.ResolveNopWebContentRoot()),
                Path.GetFullPath(env.ContentRootPath),
                "A wrong content root would make most of this suite pass vacuously - see NopWebApplicationFactory.");
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 1.5 — CommonHelper.MapPath must resolve against the content root,
        // not bin/. Verified by execution: the value is read back from the running host.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_1_5_CommonHelper_BaseDirectory_is_the_content_root()
        {
            var contentRoot = Path.GetFullPath(NopWebApplicationFactory.ResolveNopWebContentRoot());

            Assert.IsTrue(NopHostingExtensions.ContentRootConfigured,
                "UseNopHostingEnvironment did not run - deferral 1.5 is NOT closed.");
            Assert.AreEqual(contentRoot, Path.GetFullPath(CommonHelper.BaseDirectory));

            var mapped = Path.GetFullPath(CommonHelper.MapPath("~/App_Data/"));
            Assert.IsTrue(mapped.StartsWith(contentRoot, StringComparison.Ordinal),
                "MapPath(\"~/App_Data/\") resolved to " + mapped + ", outside the content root.");
            Assert.IsTrue(Directory.Exists(mapped), "App_Data does not exist at " + mapped);
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.5 / runtime deferral 1.3 and 3 — ONE Autofac container
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_3_engine_and_host_share_exactly_one_Autofac_container()
        {
            var engine = EngineContext.Current;
            Assert.IsInstanceOf<NopHostedEngine>(engine,
                "Program.cs must publish NopHostedEngine, not the base NopEngine, or two containers exist.");

            var hostRoot = _factory.Services.GetAutofacRoot();
            Assert.IsNotNull(engine.ContainerManager, "NopHostedEngine's build callback did not fire.");
            Assert.AreSame(hostRoot, engine.ContainerManager.Container,
                "The engine's container is NOT the host's container - every SingleInstance registration " +
                "would exist twice and EngineContext.Resolve would resolve from the wrong graph.");
        }

        [Test]
        public void Deferral_1_3_CurrentScopeProvider_is_assigned()
        {
            Assert.IsNotNull(ContainerManager.CurrentScopeProvider,
                "DependencyRegistrar's RegisterBuildCallback did not assign CurrentScopeProvider; " +
                "ContainerManager.Scope() would open a fresh scope per call.");

            //Outside a request there is no HttpContext, so the provider must return null rather
            //than throw - that is the documented contract and the fallback path tests rely on.
            Assert.IsNull(ContainerManager.CurrentScopeProvider(),
                "Outside a request the scope provider should yield null.");
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.5 — core services resolve through the nopCommerce container
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Services whose whole construction graph is free of <c>IDataProvider</c> and of
        /// database-backed settings, i.e. the set that must resolve whether or not a store is
        /// installed.
        /// </summary>
        private static readonly Type[] DatabaseFreeServices =
        {
            typeof(IWebHelper),          //IHttpContextAccessor - the task 2.4 seam
            typeof(IUserAgentHelper),    //NopConfig + IHttpContextAccessor
            typeof(ICacheManager),       //IMemoryCache - the System.Runtime.Caching replacement
            typeof(IDbContext),          //EF Core NopObjectContext
            typeof(IRepository<Product>),//EfRepository over it
            typeof(NopConfig),
            typeof(ITypeFinder)
        };

        /// <summary>
        /// Services that reach <c>IDataProvider</c> or a database-backed <c>ISettings</c>, so they
        /// can only be constructed once a store is installed.
        /// </summary>
        private static readonly Type[] DatabaseBackedServices =
        {
            typeof(IWorkContext), typeof(IStoreContext), typeof(ISettingService),
            typeof(ILocalizationService), typeof(IPermissionService), typeof(IProductService),
            typeof(ICustomerService), typeof(ICategoryService), typeof(IPictureService),
            typeof(ILogger), typeof(ICurrencyService), typeof(ITaxService),
            typeof(IShoppingCartService), typeof(IUrlRecordService),
            typeof(IPageHeadBuilder), typeof(IThemeContext), typeof(IThemeProvider)
        };

        [Test]
        public void Core_services_resolve_from_the_nopCommerce_container()
        {
            //Chosen so that every constructor seam the migration changed is exercised:
            //IHttpContextAccessor (WebHelper, UserAgentHelper), IMemoryCache (MemoryCacheManager),
            //EF Core (IDbContext, EfRepository<T>) and the bound configuration.
            var mandatory = Resolve(DatabaseFreeServices);
            Assert.IsEmpty(mandatory, "Services that failed to resolve WITHOUT a database:" +
                Environment.NewLine + string.Join(Environment.NewLine, mandatory));

            var dbBacked = Resolve(DatabaseBackedServices);
            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                Assert.IsEmpty(dbBacked, "Services that failed to resolve WITH a database installed:" +
                    Environment.NewLine + string.Join(Environment.NewLine, dbBacked));
            }
            else
            {
                //NOT a defect, and NOT hidden: see the dedicated test below.
                TestContext.WriteLine("No database installed; " + dbBacked.Count + " of " +
                    DatabaseBackedServices.Length + " database-backed services could not be constructed:");
                foreach (var f in dbBacked)
                    TestContext.WriteLine("  " + f);
                Assert.Ignore("Database-backed service resolution requires an installed store. " +
                    DatabaseFreeServices.Length + " database-free services resolved successfully.");
            }
        }

        [Test]
        public void Uninstalled_store_fails_service_resolution_loudly_not_silently()
        {
            //Recorded because it is the single biggest constraint on what task 7.7 could verify,
            //and because a reader could otherwise mistake it for breakage. With no
            //App_Data/Settings.txt, EfDataProviderManager has no provider name and SettingsSource
            //has no database to read, so ANY Nop.Services graph throws
            //Autofac.Core.DependencyResolutionException on IDataProvider or on an ISettings
            //parameter. This is 3.90 parity - in install mode 3.90 could not construct these
            //either - and it is a LOUD failure, which is the desired direction.
            if (DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore("A database is installed, so this state cannot be observed.");

            var containerManager = EngineContext.Current.ContainerManager;
            using (var scope = containerManager.Container.BeginLifetimeScope())
            {
                var ex = Assert.Throws<Autofac.Core.DependencyResolutionException>(
                    () => containerManager.Resolve<ILogger>(scope: scope));
                TestContext.WriteLine(ex.Message.Split('\n')[0]);
                StringAssert.Contains("IDataProvider", ex.Message);
            }
        }

        private static List<string> Resolve(IEnumerable<Type> types)
        {
            var failures = new List<string>();
            var containerManager = EngineContext.Current.ContainerManager;
            using (var scope = containerManager.Container.BeginLifetimeScope())
            {
                foreach (var t in types)
                {
                    try
                    {
                        var instance = containerManager.Resolve(t, scope);
                        if (instance == null)
                            failures.Add(t.Name + " -> null");
                    }
                    catch (Exception ex)
                    {
                        failures.Add(t.Name + " -> " + ex.GetType().Name + ": " + ex.Message.Split('\n')[0]);
                    }
                }
            }
            return failures;
        }

        [Test]
        public void Deferral_1_6_IHostApplicationLifetime_resolves_from_the_nop_container()
        {
            //WebHelper.RestartAppDomain resolves this through EngineContext and throws
            //NopException when it is absent, so every restart path (plugin install/uninstall,
            //settings that require a restart) depends on it.
            var lifetime = EngineContext.Current.Resolve<IHostApplicationLifetime>();
            Assert.IsNotNull(lifetime);
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 1.1 — plugin discovery must actually run
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_1_1_plugin_discovery_ran()
        {
            Assert.IsNotNull(PluginManager.ReferencedPlugins,
                "PluginManager.Initialize() never ran - ReferencedPlugins is null and the whole " +
                "plugin subsystem is dead (deferral 1.1).");

            var found = PluginManager.ReferencedPlugins.ToList();
            TestContext.WriteLine("PluginManager.ReferencedPlugins count = " + found.Count);
            foreach (var p in found)
                TestContext.WriteLine("  plugin: " + p.SystemName + " (" + p.PluginFileName + ")");

            //A count of zero is the CORRECT answer at this point in the migration: no plugin
            //project has been converted yet (groups 10-15), so ~/Plugins contains no
            //Description.txt for PluginManager to find. What matters is that the scan RAN, which
            //the non-null assertion above proves, and that it created its directories.
            var pluginsDir = CommonHelper.MapPath("~/Plugins");
            var pluginsBinDir = CommonHelper.MapPath("~/Plugins/bin");
            Assert.IsTrue(Directory.Exists(pluginsDir),
                "PluginManager.Initialize() should have created " + pluginsDir);
            Assert.IsTrue(Directory.Exists(pluginsBinDir),
                "PluginManager.Initialize() should have created " + pluginsBinDir);
        }

        // -----------------------------------------------------------------------------------
        // Requirement 1.4 / runtime deferral 4 and 7.2-4 — appsettings.json really binds
        // -----------------------------------------------------------------------------------

        [Test]
        public void Requirement_1_4_NopConfig_binds_from_appsettings_and_reaches_the_Autofac_singleton()
        {
            //Not "the file parses" - the actual values, read off the singleton instance that
            //RedisConnectionWrapper, AzurePictureService and UserAgentHelper are injected with.
            var config = EngineContext.Current.Resolve<NopConfig>();
            Assert.IsNotNull(config);

            Assert.AreEqual("~/App_Data/browscap.xml", config.UserAgentStringsPath,
                "NopConfig did not bind - deferral 7.20 (IsSearchEngine always false) would still be open.");
            Assert.AreEqual("~/App_Data/browscap.crawlersonly.xml", config.CrawlerOnlyUserAgentStringsPath);
            Assert.AreEqual("localhost", config.RedisCachingConnectionString);
            Assert.IsFalse(config.RedisCachingEnabled);
            Assert.IsTrue(config.SupportPreviousNopcommerceVersions,
                "appsettings.json sets this to true; a false here means the section did not bind.");
            Assert.IsFalse(config.MultipleInstancesEnabled);
            Assert.IsFalse(config.RunOnAzureWebApps);
            Assert.IsFalse(config.IgnoreStartupTasks);
            Assert.AreEqual(string.Empty, config.AzureBlobStorageConnectionString);
            Assert.IsFalse(config.DisableSampleDataDuringInstallation);
            Assert.IsFalse(config.UseFastInstallationService);

            //The Autofac singleton and a freshly bound instance must agree. (GetNopConfig()
            //deliberately returns a NEW object each call, so this is value equality, not
            //reference equality - the point is that the container was handed the BOUND config and
            //not an all-default one.)
            var rebound = NopConfigurationManager.GetNopConfig();
            Assert.AreEqual(rebound.UserAgentStringsPath, config.UserAgentStringsPath);
            Assert.AreEqual(rebound.RedisCachingConnectionString, config.RedisCachingConnectionString);
            Assert.AreEqual(rebound.SupportPreviousNopcommerceVersions, config.SupportPreviousNopcommerceVersions);
        }

        [Test]
        public void Requirement_1_4_legacy_appSettings_behave_exactly_as_in_3_90()
        {
            //The one live key is authored; the three load-balancer keys were COMMENTED OUT in
            //3.90's Web.config and must stay unset, or the port silently changes behaviour.
            Assert.AreEqual("false",
                NopConfigurationManager.GetAppSetting("ClearPluginsShadowDirectoryOnStartup"));
            Assert.IsNull(NopConfigurationManager.GetAppSetting("Use_HTTP_CLUSTER_HTTPS"));
            Assert.IsNull(NopConfigurationManager.GetAppSetting("Use_HTTP_X_FORWARDED_PROTO"));
            Assert.IsNull(NopConfigurationManager.GetAppSetting("ForwardedHTTPheader"));
            //Dropped System.Web/OWIN artifacts must not have been carried over.
            Assert.IsNull(NopConfigurationManager.GetAppSetting("webpages:Enabled"));
            Assert.IsNull(NopConfigurationManager.GetAppSetting("owin:AutomaticAppStartup"));
        }

        [Test]
        public void Deferral_7_13_forms_authentication_values_bind_and_reach_the_cookie_handler()
        {
            var authConfig = _factory.Services.GetRequiredService<IOptions<NopAuthenticationConfig>>().Value;
            Assert.AreEqual("NOPCOMMERCE.AUTH", authConfig.CookieName);
            Assert.AreEqual("/login", authConfig.LoginPath);
            Assert.AreEqual(43200, authConfig.TimeoutMinutes);
            Assert.AreEqual("/", authConfig.CookiePath);
            Assert.IsTrue(authConfig.SlidingExpiration);
            Assert.IsFalse(authConfig.RequireSsl, "3.90 shipped requireSSL=\"false\"; see deferral 7.13.");

            //The binding is only useful if the handler actually received it - deferral 7.13's
            //specific warning was a silent fall back to a 30-MINUTE default.
            var cookieOptions = _factory.Services
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme);
            Assert.AreEqual("NOPCOMMERCE.AUTH", cookieOptions.Cookie.Name);
            Assert.AreEqual(TimeSpan.FromMinutes(43200), cookieOptions.ExpireTimeSpan);
            Assert.IsTrue(cookieOptions.SlidingExpiration);
            Assert.AreEqual("/login", cookieOptions.LoginPath.Value);
            Assert.AreEqual(Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest,
                cookieOptions.Cookie.SecurePolicy);
        }

        [Test]
        public void Deferral_16_EU_VAT_endpoint_is_configuration_and_defaults_to_https()
        {
            var url = NopConfigurationManager.Configuration["Tax:EuropaCheckVatServiceUrl"];
            Assert.IsNotNull(url, "Tax:EuropaCheckVatServiceUrl is not configured (deferral 16).");
            Assert.IsTrue(url.StartsWith("https://", StringComparison.Ordinal),
                "Task 7.4 deliberately moved this endpoint from http to https; got " + url);
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.3 / 4.4 — the MVC option graph the migration hand-wired
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_11_20_FluentValidation_provider_is_registered_SECURITY()
        {
            //Without this, ModelState.IsValid returns TRUE for input nopCommerce's validators
            //would reject, across login, registration, change-password, checkout and every admin
            //form. InstallModeTests proves it actually rejects; this proves the wiring.
            var mvc = _factory.Services.GetRequiredService<IOptions<MvcOptions>>().Value;
            Assert.IsTrue(mvc.ModelValidatorProviders.Any(p => p is NopFluentValidationModelValidatorProvider),
                "NopFluentValidationModelValidatorProvider is absent from MvcOptions.ModelValidatorProviders.");
        }

        [Test]
        public void Deferrals_11_21_11_22_metadata_and_model_binder_providers_are_registered()
        {
            var mvc = _factory.Services.GetRequiredService<IOptions<MvcOptions>>().Value;

            Assert.IsTrue(mvc.ModelMetadataDetailsProviders.Any(p => p is NopMetadataProvider),
                "NopMetadataProvider is absent - ModelMetadata.AdditionalValues would stay empty (11.21).");

            Assert.IsInstanceOf<NopModelBinderProvider>(mvc.ModelBinderProviders.FirstOrDefault(),
                "NopModelBinderProvider must be at index 0 or submitted strings are not trimmed (11.22).");

            //Task 7.2's parity item: the implicit-required suppressor must run AFTER the
            //framework's DataAnnotationsMetadataProvider or a blank int is rejected where 3.90
            //accepted it.
            var names = mvc.ModelMetadataDetailsProviders.Select(p => p.GetType().Name).ToList();
            var dataAnnotations = names.FindIndex(n => n == "DataAnnotationsMetadataProvider");
            var suppressor = names.FindIndex(n => n == nameof(SuppressImplicitRequiredValueTypeMetadataProvider));
            Assert.Greater(dataAnnotations, -1, "DataAnnotationsMetadataProvider not found: " + string.Join(", ", names));
            Assert.Greater(suppressor, dataAnnotations,
                "SuppressImplicitRequiredValueTypeMetadataProvider must come after DataAnnotationsMetadataProvider. Order: "
                + string.Join(", ", names));
        }

        [Test]
        public void Deferral_11_23_JsonResult_keeps_PascalCase()
        {
            var json = _factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
            Assert.IsNull(json.JsonSerializerOptions.PropertyNamingPolicy,
                "PropertyNamingPolicy must be null; camelCase would break every Kendo grid read action.");
        }

        [Test]
        public void Deferral_14_30_theming_view_location_expander_is_first()
        {
            var razor = _factory.Services.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
            Assert.IsNotEmpty(razor.ViewLocationExpanders);
            Assert.IsInstanceOf<ThemeableViewLocationExpander>(razor.ViewLocationExpanders.First(),
                "Without the expander at index 0 every theme is ignored, silently (14.30).");
        }

        #region Task 8.2 / deferral 8.1-4 — admin view locations

        /// <summary>
        /// Builds a populated <see cref="ViewLocationExpanderContext"/> and returns what the
        /// <b>configured</b> expander from the real host emits for it.
        /// </summary>
        private IList<string> ExpandLocations(string areaName)
        {
            var razor = _factory.Services.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
            var expander = razor.ViewLocationExpanders.First();

            var actionContext = new ActionContext(
                new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = _factory.Services },
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            var context = new ViewLocationExpanderContext(actionContext, "SomeView", "SomeController",
                areaName, null, false);
            context.Values = new Dictionary<string, string>();
            expander.PopulateValues(context);

            return expander
                .ExpandViewLocations(context, new[] { "/FRAMEWORK/DEFAULT/{0}.cshtml" })
                .ToList();
        }

        [Test]
        public void Task_8_2_the_Admin_area_searches_Shared_BEFORE_the_controller_folder()
        {
            //3.90's ThemeableVirtualPathProviderViewEngine.GetPath() did two Insert(0, ...) calls,
            //so ~/Administration/Views/Shared/{0}.cshtml ended up ahead of
            //~/Administration/Views/{1}/{0}.cshtml and a same-named Shared view SHADOWED the
            //controller-specific one. Task 6.3 preserved that quirk verbatim (runtime-deferrals.md
            //section 16.1); task 8.2 moved the paths to /Areas/Admin/Views/ and had to preserve it
            //again, because ASP.NET Core's own area formats order these the OTHER way round.
            //Removing the quirk compiles, renders, and silently changes which view wins.
            var locations = ExpandLocations("Admin");

            Assert.AreEqual("/Areas/{2}/Views/Shared/{0}.cshtml", locations[0],
                "The Shared entry must come first for the Admin area (3.90 quirk, section 16.1).");
            Assert.AreEqual("/Areas/{2}/Views/{1}/{0}.cshtml", locations[1],
                "The controller-folder entry must come second for the Admin area.");
        }

        [Test]
        public void Task_8_2_the_expander_emits_no_Administration_location_deferral_8_1_4()
        {
            //Deferral 8.1-4: those two formats could never match anything, because the admin views
            //are compiled into Nop.Admin.dll and the Razor source generator names them relative to
            //NOP.ADMIN's project root. Keeping them would have been two wasted probes per lookup
            //and, more importantly, a false signal that admin view resolution was handled.
            foreach (var areaName in new[] { "Admin", "SomeOtherArea", null })
            {
                var locations = ExpandLocations(areaName);
                Assert.IsFalse(
                    locations.Any(l => l.StartsWith("/Administration/", StringComparison.OrdinalIgnoreCase)),
                    "area=" + (areaName ?? "<none>") + ": " + string.Join(" | ", locations));
            }
        }

        [Test]
        public void Task_8_2_the_storefront_view_locations_are_unchanged()
        {
            //The expander lives in the gated Nop.Web.Framework and is shared with the storefront,
            //so the admin change must not touch non-area lookups.
            var locations = ExpandLocations(null);

            var i = locations.IndexOf("/Views/{1}/{0}.cshtml");
            Assert.GreaterOrEqual(i, 0, "storefront default location missing: " + string.Join(" | ", locations));
            Assert.AreEqual("/Views/Shared/{0}.cshtml", locations[i + 1],
                "storefront Shared fallback must follow the controller-specific one");
            Assert.AreEqual("/FRAMEWORK/DEFAULT/{0}.cshtml", locations.Last(),
                "the framework's own locations must remain the final fallback");
            Assert.IsFalse(locations.Any(l => l.StartsWith("/Areas/", StringComparison.Ordinal)),
                "a non-area lookup must not search area locations: " + string.Join(" | ", locations));
        }

        [Test]
        public void Task_8_2_the_sibling_UI_application_part_mechanism_ran_in_the_real_host()
        {
            //Nop.Admin is not a compile-time reference of Nop.Web, so it is absent from
            //Nop.Web.deps.json and therefore absent from the application parts MVC seeds itself
            //with. AddNopFramework closes that with
            //NopApplicationPartExtensions.AddNopDiscoveredApplicationParts, which contributes the
            //assemblies WebAppTypeFinder loads. Nop.Admin.dll is not present in this test host's
            //base directory (it does not compile yet), so what is asserted here is that the
            //MECHANISM runs and contributes type-finder assemblies that MVC would not otherwise
            //have seen. Nop.Core is the witness: it references no MVC assembly, so nothing but this
            //call can have made it a part.
            var partManager = _factory.Services.GetRequiredService<ApplicationPartManager>();
            var names = partManager.ApplicationParts.Select(p => p.Name).ToList();

            Assert.Contains("Nop.Core", names,
                "AddNopDiscoveredApplicationParts did not run: " + string.Join(", ", names));
            Assert.Contains("Nop.Services", names, string.Join(", ", names));
        }

        [Test]
        public void Task_8_2_WebAppTypeFinder_loads_base_directory_assemblies_without_throwing()
        {
            //Task 8.2 changed AppDomainTypeFinder.LoadMatchingAssemblies from
            //AppDomain.Load(AssemblyName) to AssemblyLoadContext.Default.LoadFromAssemblyPath,
            //because the former cannot resolve a base-directory assembly that is absent from
            //deps.json and let a FileNotFoundException escape - which would take the host down at
            //startup from inside NopHostedEngine.RegisterInto.
            //
            //HONEST LIMIT OF THIS TEST: it only covers the non-regressing case. Every assembly in
            //this host's base directory IS in its deps.json, so reverting the fix would still make
            //this test pass. The behaviour itself was measured with a throwaway probe that placed
            //an assembly in the base directory and NOT in deps.json - see runtime-deferrals.md
            //section 50. Task 8.8, where Nop.Admin.dll really is such an assembly, is the first
            //point at which this can be asserted for real.
            IList<System.Reflection.Assembly> assemblies = null;
            Assert.DoesNotThrow(() => assemblies = new WebAppTypeFinder().GetAssemblies());
            Assert.IsNotNull(assemblies);
            Assert.IsTrue(assemblies.Any(a => a.GetName().Name == "Nop.Core"),
                string.Join(", ", assemblies.Select(a => a.GetName().Name)));
        }

        [Test]
        public void Task_8_2_the_Admin_area_route_is_absent_until_Nop_Admin_compiles_KNOWN_GAP()
        {
            //Task 8.2 wired the Admin area end to end: Nop.Admin/Infrastructure/RouteProvider.cs
            //registers MapAreaControllerRoute("Admin_default", "Admin",
            //"Admin/{controller=Home}/{action=Index}/{id?}") through IRouteProvider, which
            //RoutePublisher discovers reflectively. It cannot appear yet, for a reason that has
            //nothing to do with the wiring: Nop.Admin does not compile (task 8.3/8.4), so
            //Nop.Admin.dll does not exist and cannot be dropped into this host's base directory.
            //
            //TASK 8.8 MUST FLIP THIS TEST. Once Nop.Admin compiles and its post-build copy has
            //run, an endpoint with pattern "Admin/{controller=Home}/{action=Index}/{id?}" must be
            //present, and this assertion must be inverted. Leaving it as-is after 8.8 would mean
            //the suite is asserting the bug.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "endpoints?q=Admin/").Result;

            Assert.IsFalse(body.Contains("Admin/{controller=Home}"),
                "The Admin area route is now registered - INVERT THIS TEST (task 8.8). " + body);
        }

        #endregion

        [Test]
        public void Deferrals_11_26_14_31_11_28_framework_services_resolve()
        {
            Assert.IsNotNull(_factory.Services.GetService<IAntiforgery>(), "IAntiforgery (11.26)");
            Assert.IsNotNull(_factory.Services.GetService<IFileVersionProvider>(), "IFileVersionProvider (14.31)");
            Assert.IsNotNull(_factory.Services.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                "IHttpContextAccessor (7.14)");
            Assert.IsNotNull(_factory.Services.GetService<ITempDataProvider>(), "ITempDataProvider (11.28)");
            Assert.IsNotNull(_factory.Services.GetService<Microsoft.AspNetCore.Session.ISessionStore>(),
                "ISessionStore (7.15)");
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 1.3 / 3, the half that is only observable inside a request
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_3_per_request_scope_is_shared_within_one_request()
        {
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "scope").Result;
            TestContext.WriteLine(body);

            StringAssert.DoesNotContain("EXCEPTION=", body);
            StringAssert.Contains("requestServicesIsLifetimeScope=True", body);
            StringAssert.Contains("currentScopeProviderAssigned=True", body);
            StringAssert.Contains("providerReturnsNonNull=True", body);
            StringAssert.Contains("providerScopeStable=True", body);
            StringAssert.Contains("providerScopeIsRequestServices=True", body);
            StringAssert.Contains("containerManagerScopeStable=True", body);
            StringAssert.Contains("perRequestServiceShared=True", body);
            StringAssert.Contains("sharedWithRequestScope=True", body);
        }

        // -----------------------------------------------------------------------------------
        // runtime-deferrals.md §28.1 — the seven SuppressMatchingMetadata routes must still
        // generate URLs. Task 7.3 verified this against a SYNTHETIC probe app only.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Section_28_1_the_seven_name_only_routes_still_generate_URLs()
        {
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "routeurl").Result;
            TestContext.WriteLine(body);

            StringAssert.DoesNotContain("EXCEPTION=", body);
            StringAssert.Contains("route:Product=/smoke-product-slug", body);
            StringAssert.Contains("route:Category=/smoke-category-slug", body);
            StringAssert.Contains("route:Manufacturer=/smoke-manufacturer-slug", body);
            StringAssert.Contains("route:Vendor=/smoke-vendor-slug", body);
            StringAssert.Contains("route:NewsItem=/smoke-news-slug", body);
            StringAssert.Contains("route:BlogPost=/smoke-blog-slug", body);
            StringAssert.Contains("route:Topic=/smoke-topic-slug", body);
            //A literal route must still be generable too.
            StringAssert.Contains("route:ShoppingCart=/cart", body);
        }

        [Test]
        public void Section_28_1_the_seven_name_only_routes_carry_SuppressMatchingMetadata()
        {
            //The other half: they must be OUT of inbound matching, or endpoint routing raises
            //AmbiguousMatchException against {generic_se_name}. Task 7.3 corrected an upstream
            //instruction here - the recommended .WithOrder(1000) provably CREATES the ambiguity -
            //so this asserts the mechanism actually chosen, read off the live endpoint table.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "endpoints?q={SeName}").Result;
            TestContext.WriteLine(body);

            var lines = body.Split('\n')
                            .Where(l => l.StartsWith("endpoint ") && l.Contains("{SeName}"))
                            .ToList();
            Assert.IsNotEmpty(lines, "No {SeName} endpoints found - check the probe.");
            var unsuppressed = lines.Where(l => l.Contains("suppressMatching=False")).ToList();
            Assert.IsEmpty(unsuppressed,
                "These single-segment {SeName} endpoints are still matchable and will collide with " +
                "{generic_se_name}:" + Environment.NewLine + string.Join(Environment.NewLine, unsuppressed));
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 7.3-4 — former [ChildActionOnly] actions must not be URL-reachable.
        //
        // Fixed ahead of its assigned task (8.3) so Nop.Admin ports onto the finished mechanism:
        // Nop.Web.Framework.Mvc.NopChildActionOnlyAttribute selects the actions and
        // NopChildActionOnlyConvention adds SuppressMatchingMetadata to their endpoints. These
        // assertions read the LIVE endpoint table and the LIVE descriptor collection, so they hold
        // with or without a database — which matters, because the HTTP-level symptom is only
        // observable on an installed store (InstallUrlMiddleware redirects everything otherwise).
        // -----------------------------------------------------------------------------------

        [Test]
        [TestCase("Common", "Footer")]
        [TestCase("Common", "Logo")]
        [TestCase("Catalog", "TopMenu")]
        [TestCase("ShoppingCart", "OrderSummary")]
        [TestCase("Product", "RelatedProducts")]
        [TestCase("Profile", "Info")]
        [TestCase("Widget", "WidgetsByZone")]
        public void Deferral_7_3_4_a_marked_child_action_has_no_matchable_endpoint(
            string controller, string action)
        {
            //Widget/WidgetsByZone is the interesting case: RouteProvider registers an EXPLICIT
            //"widgetsbyzone/" route for it as well as the Default route, so this proves the
            //suppression is scoped to the ACTION rather than to one route - which is what 3.90's
            //[ChildActionOnly] did (that URL answered 500 there).
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix +
                "action?controller=" + controller + "&action=" + action).Result;
            TestContext.WriteLine(body);

            StringAssert.DoesNotContain("EXCEPTION=", body);
            //sanity: the action must actually exist, or "0 matchable endpoints" would be vacuous
            Assert.IsFalse(body.Contains("endpointCount=0"),
                "No endpoint at all for " + controller + "." + action +
                " - the probe found nothing, so the suppression assertion would be vacuous.");
            StringAssert.Contains("matchableEndpointCount=0", body,
                controller + "." + action + " is still reachable by URL. Either the " +
                "[NopChildActionOnly] marker is missing or NopChildActionOnlyConvention is not " +
                "registered in AddNopFramework.");
        }

        [Test]
        [TestCase("Common", "Footer")]
        [TestCase("Catalog", "TopMenu")]
        [TestCase("ShoppingCart", "FlyoutShoppingCart")]
        [TestCase("Widget", "WidgetsByZone")]
        public void Deferral_7_3_4_a_marked_child_action_is_STILL_visible_to_the_Html_Action_bridge(
            string controller, string action)
        {
            //THE CRITICAL CONSTRAINT. Task 7.3's ChildActionExtensions bridge resolves these
            //actions through IActionDescriptorCollectionProvider and invokes them by reflection; it
            //never touches the matcher. So suppressing MATCHING must leave the descriptor in place.
            //If this ever fails, the home page's ~15 child actions stop rendering and the fix has
            //traded a minor information exposure for a broken storefront - so it is asserted
            //directly rather than reasoned about, and asserted here (no database required) rather
            //than only via a rendered page.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix +
                "action?controller=" + controller + "&action=" + action).Result;
            TestContext.WriteLine(body);

            StringAssert.Contains("visibleToChildActionBridge=True", body,
                controller + "." + action + " has vanished from IActionDescriptorCollectionProvider. " +
                "@Html.Action would now throw \"could not find an action\" - see " +
                "Nop.Web.Framework/ChildActionExtensions.cs.");
        }

        [Test]
        [TestCase("Home", "Index")]
        [TestCase("Customer", "Info")]
        [TestCase("Catalog", "Search")]
        [TestCase("Common", "ContactUs")]
        public void Deferral_7_3_4_an_UNMARKED_action_is_still_matchable(string controller, string action)
        {
            //The other side of the ledger. The convention must only remove the marked 48; marking
            //an action that was never [ChildActionOnly] would delete a legitimate URL endpoint.
            //Customer/Info is deliberately included: ProfileController.Info IS marked and both are
            //called "Info", so this catches a name-based rather than method-based application of
            //the marker.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix +
                "action?controller=" + controller + "&action=" + action).Result;
            TestContext.WriteLine(body);

            StringAssert.DoesNotContain("EXCEPTION=", body);
            Assert.IsFalse(body.Contains("matchableEndpointCount=0"),
                controller + "." + action + " has NO matchable endpoint but was never " +
                "[ChildActionOnly] in 3.90 - the marker has been applied too widely.");
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 7.7-1 — a bodiless non-404 must keep its own status code
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_7_7_1_a_bodiless_non_404_keeps_its_status_code()
        {
            //InstallController.RestartInstall is [HttpPost]-only, so a GET matches the route
            //pattern but no HTTP method - routing produces a BODILESS 405. That is the same shape
            //as PublicAntiForgeryAttribute's BadRequestResult, and unlike the antiforgery path it
            //is reachable with no database at all (InstallUrlMiddleware lets /install/* through).
            //
            //Before the fix, UseStatusCodePagesWithReExecute swallowed the 405 and re-executed
            ///page-not-found; measured, that produced a 302 to /install in install mode and a 404
            //"Page not found" on an installed store. Either way the 405 was lost.
            var response = _client.GetAsync("/install/restartinstall").Result;
            TestContext.WriteLine("GET /install/restartinstall -> " + (int)response.StatusCode);

            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode,
                "A bodiless non-404 was rewritten by the status-code-pages middleware. " +
                "Program.cs must call UseNopStatusCodePages(), not UseStatusCodePagesWithReExecute().");
        }
    }
}
