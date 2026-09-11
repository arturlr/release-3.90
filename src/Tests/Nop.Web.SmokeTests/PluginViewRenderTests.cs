using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core.Data;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Tasks 10.1–10.3 — the first three real migrated plugins: discovered, loaded, contributed as
    /// application parts, their compiled Razor views found at 3.90's paths, their routes
    /// registered, and (with a database) their views RENDERED over HTTP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this fixture exists and what it adds to task 8.8's.</b>
    /// <see cref="PluginDiscoveryTests"/> proved the plugin <b>load</b> path — deferral 8.2-1's
    /// <c>AssemblyLoadContext.Default.LoadFromAssemblyPath</c> fix — with
    /// <c>Nop.Plugin.SmokeProbe</c>, which has no controller, no route and, decisively, <b>no
    /// view</b>. Task 8.8 recorded that as an explicit limit: deferral <b>1.2</b> was only half
    /// exercised, because a bare <c>AssemblyPart</c> would have looked identical for a plugin with
    /// nothing to render. These are the first plugins that can close it.
    /// </para>
    /// <para>
    /// <b>Everything here is read out of the real host</b> through the <c>/__smoke/plugins</c>
    /// probe, which runs inside a live request and reports facts from the real
    /// <c>ApplicationPartManager</c>, the real <c>IRazorViewEngine</c>, the real
    /// <c>EndpointDataSource</c> and the real <c>LinkGenerator</c>. The plugins are not planted:
    /// each one's <c>OutputPath</c> is 3.90's
    /// <c>Presentation\Nop.Web\Plugins\&lt;ShortName&gt;\</c>, so the build puts them exactly
    /// where production puts them and <c>PluginManager</c> finds them with no help from the test.
    /// </para>
    /// <para>
    /// <b>What needs a database and what does not.</b> Group A needs none: discovery, parts,
    /// compiled identifiers, view-engine lookups, <c>_ViewStart</c> isolation, endpoints and URL
    /// generation are all observable in install mode. Group B — the actual HTTP render — needs an
    /// installed store and an authenticated administrator, because these are admin plugins:
    /// <c>[AdminAuthorize]</c> plus <c>IPermissionService.Authorize</c> plus
    /// <c>IDiscountService.GetDiscountById</c>, and <c>InstallUrlMiddleware</c> redirects every
    /// request to <c>/install</c> before routing runs anyway. Group B skips with the recipe rather
    /// than being weakened into something that passes without rendering anything.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class PluginViewRenderTests
    {
        private const string CustomerRolesAsm = "Nop.Plugin.DiscountRules.CustomerRoles";
        private const string HasOneProductAsm = "Nop.Plugin.DiscountRules.HasOneProduct";
        private const string EcbAsm = "Nop.Plugin.ExchangeRate.EcbExchange";

        private const string SkipNoDatabase =
            "NOT EXERCISED: no database is installed, so InstallUrlMiddleware redirects every " +
            "request to /install and no plugin view can be rendered over HTTP. The DB-free half " +
            "of this fixture (Group A) still runs. See build-environment.md for the SQL Server + " +
            "installer recipe.";

        private NopWebApplicationFactory _factory;
        private HttpClient _client;
        private string _report;
        private bool _signedIn;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            _report = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "plugins").Result;
            TestContext.WriteLine("---- /__smoke/plugins ----");
            TestContext.WriteLine(_report);

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var login = _client.PostAsync("/login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "Email", AdminUiRenderTests.AdminEmail },
                    { "Password", AdminUiRenderTests.AdminPassword }
                })).Result;

            //Same discriminator as AdminUiRenderTests, and for the same reason: "was the request
            //REFUSED", not "did it succeed". Keying off a 200 would make a broken plugin view a
            //SKIP instead of a failure, which is the trap that fixture documents.
            var probe = _client.GetAsync("/Admin/").Result;
            _signedIn = login.StatusCode == HttpStatusCode.Found &&
                        probe.StatusCode != HttpStatusCode.Found;
            TestContext.WriteLine("sign-in probe: POST /login -> " + (int)login.StatusCode +
                ", GET /Admin/ -> " + (int)probe.StatusCode + ", signedIn=" + _signedIn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        private void AssertReport(string expected, string why)
        {
            //The probe reports `key=value` lines; every assertion is an exact-line match rather
            //than a loose Contains, so a partial or differently-shaped value cannot satisfy one.
            var lines = (_report ?? string.Empty).Split('\n').Select(l => l.Trim());
            Assert.IsTrue(lines.Any(l => l == expected),
                why + Environment.NewLine + "expected line: " + expected +
                Environment.NewLine + "full report:" + Environment.NewLine + _report);
        }

        #region Group A — no database required

        [Test]
        public void Task_10_x_all_three_plugins_are_discovered_and_version_compatible()
        {
            //Description.txt's SupportedVersions must CONTAIN NopVersion.CurrentVersion or
            //PluginManager records the plugin in IncompatiblePlugins and skips it - silently, as
            //far as anything downstream can tell. The task brief said to assert this rather than
            //assume it, so it is asserted here from the live PluginManager state, not by reading
            //the files.
            AssertReport("nopVersion=3.90", "NopVersion.CurrentVersion changed; every plugin's " +
                "Description.txt SupportedVersions line must change with it.");
            AssertReport("referencedPluginsIsNull=False",
                "PluginManager.Initialize() did not run - deferral 1.1.");

            foreach (var asm in new[] { CustomerRolesAsm, HasOneProductAsm, EcbAsm })
            {
                AssertReport("plugin:" + asm + ".discovered=True",
                    asm + " was not discovered. Check that it built to " +
                    "src/Presentation/Nop.Web/Plugins/<ShortName>/ - if OutputPath or " +
                    "AppendTargetFrameworkToOutputPath=false is missing it deploys to a " +
                    "net10.0 subfolder, whose parent is not \"Plugins\", and " +
                    "PluginManager.IsPackagePluginFolder rejects it.");
                AssertReport("plugin:" + asm + ".supportsCurrentVersion=True",
                    asm + "'s Description.txt does not list " +
                    "NopVersion.CurrentVersion in SupportedVersions, so PluginManager treats it " +
                    "as incompatible and skips it.");
                AssertReport("plugin:" + asm + ".assignableToIPlugin=True",
                    asm + "'s plugin type is not assignable to Nop.Core.Plugins.IPlugin, which " +
                    "means it was loaded into a NON-DEFAULT AssemblyLoadContext (deferral 8.2-1) " +
                    "or a stray Nop.Core.dll next to it gave the process a second Nop.Core.");
                AssertReport("plugin:" + asm + ".loadContextIsDefault=True",
                    asm + " was not loaded into the default AssemblyLoadContext.");
            }

            //3.90's system names, which are what InstalledPlugins.txt and every
            //IDiscountRequirementRule/IExchangeRateProvider lookup key off. A renamed one would
            //orphan an installed store's configuration.
            AssertReport("plugin:" + CustomerRolesAsm +
                ".systemName=DiscountRequirement.MustBeAssignedToCustomerRole", "system name changed");
            AssertReport("plugin:" + HasOneProductAsm +
                ".systemName=DiscountRequirement.HasOneProduct", "system name changed");
            AssertReport("plugin:" + EcbAsm + ".systemName=CurrencyExchange.ECB", "system name changed");
        }

        [Test]
        public void Task_10_x_no_plugin_is_reported_incompatible()
        {
            //The complement of the assertion above, from the other side of the branch: a plugin
            //whose Description.txt no longer matches lands here instead of in ReferencedPlugins.
            var lines = (_report ?? string.Empty).Split('\n').Select(l => l.Trim()).ToList();
            var incompatible = lines
                .Where(l => l.StartsWith("incompatible=", StringComparison.Ordinal))
                .Select(l => l.Substring("incompatible=".Length))
                .ToList();
            CollectionAssert.IsEmpty(incompatible,
                "PluginManager recorded incompatible plugins: " + string.Join(", ", incompatible));
        }

        [Test]
        public void Deferral_1_2_a_view_bearing_plugin_contributes_a_CompiledRazorAssemblyPart()
        {
            //*** THIS CLOSES THE HALF OF DEFERRAL 1.2 TASK 8.8 COULD NOT. ***
            //Task 8.2 changed NopApplicationPartExtensions from `new AssemblyPart(assembly)` to
            //ApplicationPartFactory.GetApplicationPartFactory(assembly) precisely so a Razor-SDK
            //assembly contributes BOTH parts. With only the AssemblyPart the plugin's controllers
            //are routable and every one of its views is unresolvable - a failure with no
            //compile error and no startup error.
            //
            //It is also the assertion that pins AddRazorSupportForMvc=true in the plugin project
            //files: without that property the views still compile but the assembly carries no
            //[ProvideApplicationPartFactory], the DEFAULT factory is used, and only an
            //AssemblyPart appears. Measured on a probe before the recipe was chosen.
            foreach (var asm in new[] { CustomerRolesAsm, HasOneProductAsm })
            {
                AssertReport("part:" + asm + "=AssemblyPart",
                    asm + " did not become an MVC AssemblyPart, so its controllers are not routable.");
                AssertReport("part:" + asm + "=CompiledRazorAssemblyPart",
                    asm + " contributed NO Razor part, so none of its compiled views can be found. " +
                    "Check Sdk=\"Microsoft.NET.Sdk.Razor\" AND " +
                    "<AddRazorSupportForMvc>true</AddRazorSupportForMvc> in its project file, and " +
                    "that NopApplicationPartExtensions still uses ApplicationPartFactory rather " +
                    "than new AssemblyPart(...) - deferral 1.2.");
            }

            //And the viewless plugin contributes only the AssemblyPart, which is what makes the
            //assertion above discriminating rather than something every assembly satisfies.
            AssertReport("part:" + EcbAsm + "=AssemblyPart", EcbAsm + " is not an application part.");
            Assert.IsFalse((_report ?? string.Empty).Contains("part:" + EcbAsm + "=CompiledRazorAssemblyPart"),
                EcbAsm + " ships no .cshtml, so a Razor part here would mean the part type says " +
                "nothing about whether views were contributed.");
        }

        [Test]
        public void Task_10_x_compiled_view_identifiers_are_3_90s_Plugins_paths()
        {
            //THE VIEW-RESOLUTION DECISION, measured on the real assemblies.
            //On .NET there is no runtime view compilation: a view is looked up by the identifier
            //the Razor source generator gave it, which it derives from the file's path relative to
            //its OWN project root. Left alone, Views\Configure.cshtml would compile as
            ///Views/Configure.cshtml and 3.90's explicit
            //View("~/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml") would match
            //nothing. The Content/Link metadata in each plugin project sets the identifier back to
            //3.90's path, so not one of the 50 explicit ~/Plugins/... strings across the 20
            //plugins had to be edited.
            AssertReport("identifiers:DiscountRules.CustomerRoles.count=2",
                "expected Configure.cshtml + _ViewImports.cshtml under " +
                "/Plugins/DiscountRules.CustomerRoles/Views/.");
            AssertReport("identifier=/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml",
                "the compiled identifier is not 3.90's path. If the Link metadata is missing the " +
                "view compiles as /Views/Configure.cshtml; if the Link value repeats \"Views\\\" " +
                "it compiles as /Plugins/<ShortName>/Views/Views/Configure.cshtml, because " +
                "%(RecursiveDir) already contains it.");
            AssertReport("identifier=/Plugins/DiscountRules.CustomerRoles/Views/_ViewImports.cshtml",
                "the _ViewImports.cshtml must be linked with the SAME prefix as the views, or " +
                "Razor resolves imports from a different directory and they silently do not apply.");

            AssertReport("identifiers:DiscountRules.HasOneProduct.count=3", "expected 3 identifiers");
            AssertReport("identifier=/Plugins/DiscountRules.HasOneProduct/Views/Configure.cshtml", "");
            AssertReport("identifier=/Plugins/DiscountRules.HasOneProduct/Views/ProductAddPopup.cshtml", "");
            AssertReport("identifier=/Plugins/DiscountRules.HasOneProduct/Views/_ViewImports.cshtml", "");

            AssertReport("identifiers:ExchangeRate.EcbExchange.count=0",
                EcbAsm + " ships no views.");

            //and none of them landed in the host's own /Views/ identifier namespace, where
            ///Views/_ViewImports.cshtml and /Views/_ViewStart.cshtml already exist in Nop.Web.dll
            AssertReport("pluginViewsUnderHostViewsPath=0",
                "a plugin view compiled under /Views/, i.e. into the host's identifier namespace.");
        }

        [Test]
        public void Task_10_x_the_real_view_engine_finds_every_path_the_controllers_pass()
        {
            //Identifiers being present is not the same as the view engine resolving them. This
            //asks the real IRazorViewEngine for the exact strings the ported controllers pass -
            //copied from the source, not paraphrased.
            AssertReport("getView:~/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml=True", "");
            AssertReport("getView:~/Plugins/DiscountRules.HasOneProduct/Views/Configure.cshtml=True", "");
            AssertReport("getView:~/Plugins/DiscountRules.HasOneProduct/Views/ProductAddPopup.cshtml=True", "");
        }

        [Test]
        public void Deferral_8_2_3_the_cross_assembly_admin_layout_and_partial_resolve()
        {
            //*** DEFERRAL 8.2-3, AND THE DEEPER QUESTION IT RAISES. ***
            //ProductAddPopup.cshtml sets
            //   Layout = "~/Areas/Admin/Views/Shared/_AdminPopupLayout.cshtml"
            //and partials "~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml". Both name views
            //COMPILED INTO Nop.Admin.dll, from a plugin in a different assembly which is NOT in
            //the Admin area. That it resolves is not obvious from the string looking right: it
            //works only because task 8.2's NopApplicationPartExtensions contributes every
            //discovered assembly as an application part, which puts Nop.Admin's compiled
            //identifiers into the SAME view-compiler dictionary as the plugin's. Asserted here
            //rather than assumed.
            AssertReport("getView:~/Areas/Admin/Views/Shared/_AdminPopupLayout.cshtml=True",
                "the admin popup layout does not resolve, so ProductAddPopup.cshtml cannot render. " +
                "Nop.Admin.dll is probably not an application part in this host - see deferral " +
                "8.4-1 and the CopyNopAdminToSmokeTestOutput target.");
            AssertReport("getView:~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml=True",
                "the Kendo grid pager partial does not resolve.");

            //...and the PRE-8.2 paths must NOT resolve. Without this the assertion above would be
            //satisfied by a host in which both paths worked, and the 8.2-3 rewrite would have been
            //unnecessary - i.e. it proves the edit was load-bearing.
            AssertReport("getView:~/Administration/Views/Shared/_AdminPopupLayout.cshtml=False",
                "the PRE-8.2 admin view path still resolves, so this fixture cannot show that " +
                "rewriting the plugin views to ~/Areas/Admin/Views/ was necessary.");
            AssertReport("getView:~/Administration/Views/Shared/_GridPagerMessages.cshtml=False",
                "the PRE-8.2 admin view path still resolves.");
        }

        [Test]
        public void Task_10_x_no_ViewStart_applies_to_a_plugin_view_and_the_alternative_would_have_leaked()
        {
            //*** THE REASON THE VIEWS WERE NOT RELOCATED UNDER /Views/<Controller>/. ***
            //_ViewStart is resolved AT RUNTIME by walking a view identifier's ancestor directories
            //through the shared compiled-view dictionary. In 3.90 no _ViewStart applied to a plugin
            //view, because they lived at ~/Plugins/... whose ancestors have none. This asserts that
            //property still holds - AND, in the same report, that the obvious alternative would
            //have broken it, which is what makes the first half meaningful rather than incidental.
            AssertReport("viewStartsApplyingTo:/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml=<none>",
                "a _ViewStart.cshtml now applies to a plugin view. In 3.90 none did.");
            AssertReport("viewStartsApplyingTo:/Views/DiscountRulesCustomerRoles/Configure.cshtml=/Views/_ViewStart.cshtml",
                "PREMISE BROKEN: Nop.Web's /Views/_ViewStart.cshtml no longer applies to views " +
                "under /Views/. If that is a deliberate change, the reasoning recorded in the " +
                "plugin project files for NOT relocating plugin views under /Views/<Controller>/ " +
                "needs revisiting - it rests on this leak being real. (Nop.Web's " +
                "Views/_ViewStart.cshtml sets Layout = \"~/Views/Shared/_ColumnsOne.cshtml\", and " +
                "Pickup.PickupInStore's Configure.cshtml sets no Layout at all - task 13.1.)");
        }

        [Test]
        public void Task_10_x_the_ported_route_providers_registered_3_90s_routes()
        {
            //IRouteProvider is now void RegisterRoutes(IEndpointRouteBuilder). Patterns, route
            //names and defaults are 3.90's; only the string[] namespaces argument was dropped
            //(no ASP.NET Core counterpart - controller discovery is application-part based).
            //
            //These assertions drive each plugin's own provider against a scratch
            //IEndpointRouteBuilder over the real service provider, because the LIVE
            //EndpointDataSource legitimately carries no plugin route until the plugin is
            //INSTALLED - see Task_10_x_an_uninstalled_plugins_routes_are_deliberately_absent.
            AssertReport("routeProviders:" + CustomerRolesAsm + ".count=1", "expected one IRouteProvider");
            AssertReport("routeProviders:" + HasOneProductAsm + ".count=1", "expected one IRouteProvider");
            AssertReport("routeProviders:" + EcbAsm + ".count=0",
                EcbAsm + " has no controller and no route in 3.90 either.");
            //3.90's value, and the reason it is 0 and not int.MaxValue is recorded on the class
            AssertReport("routeProviders:" + CustomerRolesAsm + ".priority=0", "3.90's Priority was 0");
            AssertReport("routeProviders:" + HasOneProductAsm + ".priority=0", "3.90's Priority was 0");

            foreach (var pattern in new[]
            {
                "Plugins/DiscountRulesCustomerRoles/Configure",
                "Plugins/DiscountRulesHasOneProduct/Configure",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopup",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopupList",
                "Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames"
            })
                AssertReport("endpoint:" + pattern + ".routeNames=Plugin." +
                    pattern.Replace("Plugins/DiscountRulesCustomerRoles/", "DiscountRules.CustomerRoles.")
                           .Replace("Plugins/DiscountRulesHasOneProduct/", "DiscountRules.HasOneProduct."),
                    "3.90's route name for " + pattern + " is not the endpoint's name. Three of " +
                    "HasOneProduct's four routes are resolved BY NAME from its views, so a rename " +
                    "breaks URL generation at runtime with no compile error.");

            //WHICH ACTION each pattern reaches. Configure is two actions - the GET and the
            //[HttpPost] overload - sharing one name, so the distinct set has a single entry.
            AssertReport("endpoint:Plugins/DiscountRulesCustomerRoles/Configure.actions=" +
                "Nop.Plugin.DiscountRules.CustomerRoles.Controllers.DiscountRulesCustomerRolesController.Configure",
                "the route does not reach the plugin's controller action. The `new { controller = …, " +
                "action = … }` defaults are wrong, or the plugin's AssemblyPart is missing so no " +
                "action descriptor exists for it at all.");
            AssertReport("endpoint:Plugins/DiscountRulesHasOneProduct/Configure.actions=" +
                "Nop.Plugin.DiscountRules.HasOneProduct.Controllers.DiscountRulesHasOneProductController.Configure", "");
            AssertReport("endpoint:Plugins/DiscountRulesHasOneProduct/ProductAddPopup.actions=" +
                "Nop.Plugin.DiscountRules.HasOneProduct.Controllers.DiscountRulesHasOneProductController.ProductAddPopup", "");
            AssertReport("endpoint:Plugins/DiscountRulesHasOneProduct/ProductAddPopupList.actions=" +
                "Nop.Plugin.DiscountRules.HasOneProduct.Controllers.DiscountRulesHasOneProductController.ProductAddPopupList", "");
            AssertReport("endpoint:Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames.actions=" +
                "Nop.Plugin.DiscountRules.HasOneProduct.Controllers.DiscountRulesHasOneProductController.LoadProductFriendlyNames", "");
        }

        [Test]
        public void Task_10_x_an_uninstalled_plugins_routes_are_deliberately_absent()
        {
            //A FINDING, asserted so it cannot be mistaken for a defect later — and so that its
            //complement in Group B is meaningful.
            //
            //RoutePublisher.RegisterRoutes contains 3.90's filter verbatim:
            //    var plugin = FindPlugin(providerType);
            //    if (plugin != null && !plugin.Installed) continue;
            //so a plugin that is DISCOVERED but not INSTALLED contributes no endpoint. Installation
            //state comes from App_Data/InstalledPlugins.txt, written when an administrator installs
            //the plugin, so in install mode all three are uninstalled and the live
            //EndpointDataSource correctly holds none of their routes. The provider port itself is
            //asserted directly above; that the publisher WIRES it is asserted in Group B, where a
            //plugin can actually be installed.
            foreach (var asm in new[] { CustomerRolesAsm, HasOneProductAsm, EcbAsm })
                AssertReport("plugin:" + asm + ".installed=" +
                    (DataSettingsHelper.DatabaseIsInstalled() ? "True" : "False"),
                    "installation state is not what this environment implies. Without a database " +
                    "no plugin can be installed; with one, install the three plugins from the " +
                    "admin plugin list (or expect this assertion to describe the real state).");

            if (DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore("NOT EXERCISED as a negative: a database is installed, so plugin " +
                              "installation state is whatever the store has. The uninstalled-plugin " +
                              "filter is only assertable in install mode.");

            foreach (var pattern in new[]
            {
                "Plugins/DiscountRulesCustomerRoles/Configure",
                "Plugins/DiscountRulesHasOneProduct/Configure",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopup",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopupList",
                "Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames"
            })
                AssertReport("liveEndpoint:" + pattern + ".actions=<none>",
                    "an UNINSTALLED plugin contributed " + pattern + " to the live endpoint set. " +
                    "RoutePublisher's `if (plugin != null && !plugin.Installed) continue;` filter " +
                    "is 3.90's and must not be dropped - it is what keeps an uninstalled plugin's " +
                    "URLs unreachable.");
        }

        #endregion

        #region Group B — requires an installed store and an authenticated administrator

        private void RequireAdmin()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore(SkipNoDatabase);
            if (!_signedIn)
                Assert.Ignore("NOT EXERCISED: could not sign in as " + AdminUiRenderTests.AdminEmail +
                              ". This condition means NOT AUTHORISED only - a plugin view that " +
                              "fails to render reports as a failure, not a skip.");
        }

        /// <summary>
        /// Skips unless all three plugins are <b>installed</b>, i.e. present in
        /// <c>App_Data/InstalledPlugins.txt</c>.
        /// </summary>
        /// <remarks>
        /// A separate gate from <see cref="RequireAdmin"/> because it is a different premise:
        /// <c>RoutePublisher</c> registers a plugin's routes only when the plugin is installed
        /// (3.90's filter, kept verbatim), so anything measured off the LIVE endpoint set needs
        /// installation, not merely a database. The skip names the step that is missing rather than
        /// blaming the absence of a database.
        /// </remarks>
        private void RequireInstalledPlugins()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore(SkipNoDatabase);

            var notInstalled = new[] { CustomerRolesAsm, HasOneProductAsm }
                .Where(asm => !(_report ?? string.Empty).Split('\n')
                    .Select(l => l.Trim())
                    .Any(l => l == "plugin:" + asm + ".installed=True"))
                .ToList();
            if (notInstalled.Count > 0)
                Assert.Ignore("NOT EXERCISED: these plugins are discovered but NOT INSTALLED: " +
                              string.Join(", ", notInstalled) + ". RoutePublisher skips an " +
                              "uninstalled plugin's IRouteProvider, so no live endpoint exists. " +
                              "Install them from /Admin/Plugin/List (or write their system names " +
                              "into App_Data/InstalledPlugins.txt and restart) and re-run.");
        }

        [Test]
        public void Task_10_x_URL_generation_by_route_name_still_works()
        {
            //Three of HasOneProduct's four routes exist ONLY so its views can call
            //Url.RouteUrl(name) - Configure.cshtml names ProductAddPopup and
            //LoadProductFriendlyNames, ProductAddPopup.cshtml names ProductAddPopupList. Endpoint
            //route names are what LinkGenerator resolves, so a renamed route breaks at runtime with
            //no compile error and no failing route match.
            //
            //This needs the LIVE endpoint set, so it needs an INSTALLED plugin - see
            //Task_10_x_an_uninstalled_plugins_routes_are_deliberately_absent.
            RequireInstalledPlugins();

            AssertReport("routeUrl:Plugin.DiscountRules.CustomerRoles.Configure=" +
                "/Plugins/DiscountRulesCustomerRoles/Configure", "URL generation by route name failed");
            AssertReport("routeUrl:Plugin.DiscountRules.HasOneProduct.ProductAddPopup=" +
                "/Plugins/DiscountRulesHasOneProduct/ProductAddPopup", "URL generation by route name failed");
            AssertReport("routeUrl:Plugin.DiscountRules.HasOneProduct.ProductAddPopupList=" +
                "/Plugins/DiscountRulesHasOneProduct/ProductAddPopupList", "URL generation by route name failed");
            AssertReport("routeUrl:Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames=" +
                "/Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames", "URL generation by route name failed");
        }

        [Test]
        public void Task_10_x_an_installed_plugins_routes_reach_the_live_endpoint_set()
        {
            //The complement of the uninstalled case: once installed, RoutePublisher does invoke the
            //plugin's provider and the endpoints appear in the running application.
            RequireInstalledPlugins();

            foreach (var pattern in new[]
            {
                "Plugins/DiscountRulesCustomerRoles/Configure",
                "Plugins/DiscountRulesHasOneProduct/Configure",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopup",
                "Plugins/DiscountRulesHasOneProduct/ProductAddPopupList",
                "Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames"
            })
            {
                //asserted as the ACTION SET rather than a count, because MVC materialises one
                //endpoint per matching action descriptor: Configure is two (the GET and the
                //[HttpPost] overload) sharing one action name
                var controller = pattern.StartsWith("Plugins/DiscountRulesCustomerRoles/",
                        StringComparison.Ordinal)
                    ? "Nop.Plugin.DiscountRules.CustomerRoles.Controllers.DiscountRulesCustomerRolesController"
                    : "Nop.Plugin.DiscountRules.HasOneProduct.Controllers.DiscountRulesHasOneProductController";
                var action = pattern.Substring(pattern.LastIndexOf('/') + 1);
                AssertReport("liveEndpoint:" + pattern + ".actions=" + controller + "." + action,
                    "an INSTALLED plugin's route does not reach its action in the live endpoint " +
                    "set. RoutePublisher finds providers reflectively through ITypeFinder, which " +
                    "requires the plugin assembly to be in its assembly set.");
            }
        }

        [Test]
        public void Task_10_x_a_plugin_deploys_no_other_Nop_assembly_and_no_dead_files()
        {
            //The invariant behind <Private>False</Private> and the
            //NopPluginDoNotDeployHostAssemblies target. PluginManager PerformFileDeploy()s every
            //*.dll it finds in a plugin folder other than the main one, guarded only by
            //IsAlreadyLoaded, which compares BARE FILE NAMES against the loaded assembly list -
            //and Initialize() runs from Program.Main before anything has touched a Nop.Data type.
            //So a leaked Nop.Data.dll IS loaded, from the plugin's stale copy. Private="false"
            //alone does not prevent it: Nop.Data arrives in the TRANSITIVE ProjectReference
            //closure. Measured.
            foreach (var asm in new[] { CustomerRolesAsm, HasOneProductAsm, EcbAsm })
            {
                AssertReport("plugin:" + asm + ".strayNopDlls=<none>",
                    asm + " deploys another Nop.* assembly next to itself. See the " +
                    "NopPluginDoNotDeployHostAssemblies target in the plugin project files.");
                AssertReport("plugin:" + asm + ".deployDirParent=Plugins",
                    "the plugin's containing directory's parent is not \"Plugins\", so " +
                    "PluginManager.IsPackagePluginFolder rejects it. Check " +
                    "AppendTargetFrameworkToOutputPath=false.");
                AssertReport("plugin:" + asm + ".descriptionTxtDeployed=True",
                    "Description.txt was not deployed. Under the Razor SDK .txt files arrive as " +
                    "<None>, so `<Content Update=\"Description.txt\">` silently updates nothing - " +
                    "it must be <None Remove> + <Content Include>.");
                AssertReport("plugin:" + asm + ".logoDeployed=True", "logo.jpg was not deployed.");
                //obsolete files are gone, not merely unreferenced
                AssertReport("plugin:" + asm + ".deployedConfigCount=0",
                    "a .config file reached the plugin's deployment folder. web.config, app.config " +
                    "and packages.config are deleted by tasks 10.1-10.3; an emitted " +
                    "<assembly>.dll.config would mean app.config came back.");
                //3.90 had to deploy the .cshtml files (System.Web compiled them at runtime); they
                //are inside the assembly now
                AssertReport("plugin:" + asm + ".deployedCshtmlCount=0",
                    "a .cshtml reached the plugin's deployment folder. Views are compiled into the " +
                    "assembly on .NET 10 and CopyToOutputDirectory=\"Never\" is deliberate - with " +
                    "the copy left on, Link would place them at " +
                    "Plugins\\<ShortName>\\Plugins\\<ShortName>\\Views\\.");
            }
        }

        [Test]
        public void Task_10_x_AssemblyInfo_cs_was_kept_so_the_version_is_not_zero()
        {
            //src/Directory.Build.props sets GenerateAssemblyInfo=false solution-wide, so deleting a
            //hand-kept Properties/AssemblyInfo.cs drops AssemblyVersion to 0.0.0.0 - measured by
            //tasks 7.5 and 8.1 and easy to do by accident while "removing obsolete files".
            foreach (var asm in new[] { CustomerRolesAsm, HasOneProductAsm, EcbAsm })
                AssertReport("plugin:" + asm + ".assemblyVersion=1.0.0.0",
                    asm + "'s AssemblyVersion is not 1.0.0.0. If it is 0.0.0.0, " +
                    "Properties/AssemblyInfo.cs was deleted.");
        }

        [Test]
        public void Task_10_1_the_CustomerRoles_Configure_view_RENDERS_over_HTTP()
        {
            //*** THE END-TO-END PROOF: a plugin view, compiled into a plugin assembly, rendered by
            //the real host through the real route, in a real request. ***
            //It exercises, in one request: PluginManager's shadow-copy load, the
            //CompiledRazorAssemblyPart, the Content/Link identifier, the plugin's own
            //_ViewImports.cshtml (@inherits WebViewPage<TModel> is what makes T(...) resolve), the
            //HtmlExtensions helpers, the ported IRouteProvider's endpoint, and JavaScriptHelper.
            RequireAdmin();
            RequireInstalledPlugins();

            var response = _client.GetAsync(
                "/Plugins/DiscountRulesCustomerRoles/Configure?discountId=1").Result;
            var html = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("status=" + (int)response.StatusCode + " len=" + html.Length);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "the plugin's Configure action did not answer 200. body: " +
                html.Substring(0, Math.Min(2000, html.Length)));

            //the view's own markup - the drop-down and the save button it renders
            StringAssert.Contains("savecustomerrolesrequirement", html,
                "the response is a 200 but not this view's output.");
            StringAssert.Contains("requirement-data-input", html, "the view body did not render.");
            //Html.NopDropDownListFor over Model.AvailableCustomerRoles, i.e. the model bound and
            //the framework HTML helpers ran
            StringAssert.Contains("<select", html, "the customer-role drop-down did not render.");
            //ViewData.TemplateInfo.HtmlFieldPrefix, which the controller sets and which
            //Nop.Web.Framework's GetFullHtmlFieldId replacement has to reproduce
            StringAssert.Contains("DiscountRulesCustomerRoles0_CustomerRoleId", html,
                "the HtmlFieldPrefix did not reach the emitted field id, so " +
                "ViewCompatibilityExtensions.GetFullHtmlFieldId is not behaving as MVC 5's did.");

            //*** AND NO STOREFRONT LAYOUT. *** The view sets Layout = "", which ASP.NET Core
            //honours (RenderLayoutAsync loops while !string.IsNullOrEmpty(Layout)). If a
            //_ViewStart had applied - which is what compiling plugin views under /Views/ would
            //have caused - this fragment would arrive wrapped in _ColumnsOne.cshtml.
            StringAssert.DoesNotContain("<html", html,
                "the plugin view was wrapped in a layout. Layout = \"\" means no layout; a " +
                "storefront layout here means Nop.Web's Views/_ViewStart.cshtml applied to a " +
                "plugin view.");
        }

        [Test]
        public void Task_10_2_the_HasOneProduct_Configure_view_RENDERS_over_HTTP()
        {
            RequireAdmin();
            RequireInstalledPlugins();

            var response = _client.GetAsync(
                "/Plugins/DiscountRulesHasOneProduct/Configure?discountId=1").Result;
            var html = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("status=" + (int)response.StatusCode + " len=" + html.Length);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "body: " + html.Substring(0, Math.Min(2000, html.Length)));
            StringAssert.Contains("saveHasOneProductrequirement", html,
                "the response is a 200 but not this view's output.");
            //Url.RouteUrl by ROUTE NAME, from inside the view - the thing a renamed route would
            //break silently
            StringAssert.Contains("/Plugins/DiscountRulesHasOneProduct/ProductAddPopup", html,
                "Url.RouteUrl(\"Plugin.DiscountRules.HasOneProduct.ProductAddPopup\") produced " +
                "nothing, so the route name is not registered as an endpoint name.");
            StringAssert.Contains("/Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames", html,
                "Url.RouteUrl(\"…LoadProductFriendlyNames\") produced nothing.");
            StringAssert.DoesNotContain("<html", html, "Layout = \"\" was not honoured.");
        }

        [Test]
        public void Deferral_8_2_3_the_HasOneProduct_ProductAddPopup_RENDERS_inside_the_admin_popup_layout()
        {
            //The cross-assembly render: this plugin view's Layout is a view compiled into
            //Nop.Admin.dll, and its Kendo pager block partials another one. A 200 whose body
            //carries the popup layout's own markup is the proof that a plugin can name a view in
            //another assembly by explicit path - deferral 8.2-3's "verify that resolves, do not
            //assume it because the string looks right".
            RequireAdmin();
            RequireInstalledPlugins();

            var response = _client.GetAsync(
                "/Plugins/DiscountRulesHasOneProduct/ProductAddPopup" +
                "?btnId=btnRefresh&productIdsInput=someInput").Result;
            var html = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("status=" + (int)response.StatusCode + " len=" + html.Length);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "body: " + html.Substring(0, Math.Min(2000, html.Length)));

            //the LAYOUT ran: _AdminPopupLayout.cshtml emits the document and the admin popup body
            //class. This is Nop.Admin.dll's compiled view, reached from a plugin assembly.
            StringAssert.Contains("<html", html,
                "the admin popup layout did not run, so " +
                "Layout = \"~/Areas/Admin/Views/Shared/_AdminPopupLayout.cshtml\" did not resolve " +
                "across assemblies.");
            //the PARTIAL ran: _GridPagerMessages.cshtml contributes the Kendo pager's localised
            //messages object
            StringAssert.Contains("messages:", html,
                "the ~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml partial did not render.");
            //the plugin view's own body
            StringAssert.Contains("products-grid", html, "the view body did not render.");
            StringAssert.Contains("selectRequiredProduct", html, "the view body did not render.");
        }

        #endregion
    }
}
