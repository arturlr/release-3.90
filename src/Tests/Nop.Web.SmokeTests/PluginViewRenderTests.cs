using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// A settings type whose <b>simple name</b> matches the plugin's, so
    /// <c>ISettingService.LoadSetting&lt;T&gt;</c> reads the very same rows.
    /// </summary>
    /// <remarks>
    /// Task 11.1. <c>SettingService</c> keys a setting as <c>typeof(T).Name + "." + prop.Name</c> —
    /// the SIMPLE type name, with no namespace — so this type reads
    /// <c>facebookexternalauthsettings.clientkeyidentifier</c> exactly as the plugin's own type
    /// does. Declared here rather than referencing
    /// <c>Nop.Plugin.ExternalAuth.Facebook.FacebookExternalAuthSettings</c> because the plugin
    /// references in <c>Nop.Web.SmokeTests.csproj</c> are <b>build-order only</b>
    /// (<c>ReferenceOutputAssembly="false"</c>) and deliberately so: a compiling reference would put
    /// the plugin assembly in this project's output directory, where <c>WebAppTypeFinder</c> would
    /// load it as an ordinary base-directory assembly and the whole <c>PluginManager</c> shadow-copy
    /// path under test would be bypassed.
    /// <para>
    /// It carries only the property the assertion reads. <c>LoadSetting</c> fills what it finds and
    /// leaves the rest at its default, so a partial clone is safe.
    /// </para>
    /// </remarks>
    public class FacebookExternalAuthSettings : ISettings
    {
        public string ClientKeyIdentifier { get; set; }
    }

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
        private const string FacebookAsm = "Nop.Plugin.ExternalAuth.Facebook";
        private const string GoogleShoppingAsm = "Nop.Plugin.Feed.GoogleShopping";
        private const string PickupInStoreAsm = "Nop.Plugin.Pickup.PickupInStore";
        private const string TaxCountryStateZipAsm = "Nop.Plugin.Tax.FixedOrByCountryStateZip";
        private const string WidgetsGoogleAnalyticsAsm = "Nop.Plugin.Widgets.GoogleAnalytics";
        private const string WidgetsNivoSliderAsm = "Nop.Plugin.Widgets.NivoSlider";

        /// <summary>
        /// The plugins that ship a compiled view, i.e. the ones deferral 1.2's Razor half
        /// applies to. <c>ExchangeRate.EcbExchange</c> is deliberately excluded — it is the only
        /// viewless plugin in the solution and is what makes those assertions discriminating.
        /// </summary>
        private static readonly string[] ViewBearing =
        {
            CustomerRolesAsm, HasOneProductAsm, FacebookAsm, GoogleShoppingAsm, PickupInStoreAsm,
            TaxCountryStateZipAsm, WidgetsGoogleAnalyticsAsm, WidgetsNivoSliderAsm
        };

        /// <summary>
        /// Every migrated plugin covered by this fixture (through task 15.3).
        /// </summary>
        private static readonly string[] AllMigrated =
        {
            CustomerRolesAsm, HasOneProductAsm, EcbAsm, FacebookAsm, GoogleShoppingAsm, PickupInStoreAsm,
            TaxCountryStateZipAsm, WidgetsGoogleAnalyticsAsm, WidgetsNivoSliderAsm
        };

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

            foreach (var asm in AllMigrated)
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

            //Task 11.1 / 11.2. Feed.GoogleShopping's system name is "PromotionFeed.Froogle", NOT
            //"Feed.GoogleShopping": the plugin was renamed upstream and its system name was not,
            //and FeedGoogleShoppingController.GenerateFeed looks the plugin up BY THAT STRING
            //(_pluginFinder.GetPluginDescriptorBySystemName("PromotionFeed.Froogle")). "Tidying" it
            //would break the Generate feed button AND orphan every installed store's
            //InstalledPlugins.txt entry.
            AssertReport("plugin:" + FacebookAsm + ".systemName=ExternalAuth.Facebook",
                "system name changed");
            AssertReport("plugin:" + GoogleShoppingAsm + ".systemName=PromotionFeed.Froogle",
                "Feed.GoogleShopping's system name is 3.90's PromotionFeed.Froogle. " +
                "FeedGoogleShoppingController.GenerateFeed resolves the plugin by that literal.");

            //Task 15.1 / 15.2 / 15.3 - 3.90's system names, verbatim from each Description.txt.
            //These are the keys InstalledPlugins.txt and every ITaxProvider / IWidgetPlugin lookup
            //use, so a rename would orphan an installed store's configuration.
            AssertReport("plugin:" + TaxCountryStateZipAsm + ".systemName=Tax.FixedOrByCountryStateZip",
                "system name changed");
            AssertReport("plugin:" + WidgetsGoogleAnalyticsAsm + ".systemName=Widgets.GoogleAnalytics",
                "system name changed");
            AssertReport("plugin:" + WidgetsNivoSliderAsm + ".systemName=Widgets.NivoSlider",
                "system name changed");
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
            foreach (var asm in ViewBearing)
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

            //--- task 11.1 / 11.2 ---
            AssertReport("identifiers:ExternalAuth.Facebook.count=3",
                "expected Configure.cshtml + PublicInfo.cshtml + _ViewImports.cshtml under " +
                "/Plugins/ExternalAuth.Facebook/Views/.");
            AssertReport("identifier=/Plugins/ExternalAuth.Facebook/Views/Configure.cshtml", "");
            AssertReport("identifier=/Plugins/ExternalAuth.Facebook/Views/PublicInfo.cshtml", "");
            AssertReport("identifier=/Plugins/ExternalAuth.Facebook/Views/_ViewImports.cshtml", "");

            AssertReport("identifiers:Feed.GoogleShopping.count=2",
                "expected Configure.cshtml + _ViewImports.cshtml under " +
                "/Plugins/Feed.GoogleShopping/Views/.");
            AssertReport("identifier=/Plugins/Feed.GoogleShopping/Views/Configure.cshtml", "");
            AssertReport("identifier=/Plugins/Feed.GoogleShopping/Views/_ViewImports.cshtml", "");

            //--- task 15.1 / 15.2 / 15.3 ---
            AssertReport("identifiers:Tax.FixedOrByCountryStateZip.count=4",
                "expected Configure.cshtml + _FixedRate.cshtml + _CountryStateZip.cshtml + " +
                "_ViewImports.cshtml under /Plugins/Tax.FixedOrByCountryStateZip/Views/.");
            AssertReport("identifier=/Plugins/Tax.FixedOrByCountryStateZip/Views/Configure.cshtml", "");
            AssertReport("identifier=/Plugins/Tax.FixedOrByCountryStateZip/Views/_FixedRate.cshtml", "");
            AssertReport("identifier=/Plugins/Tax.FixedOrByCountryStateZip/Views/_CountryStateZip.cshtml", "");
            AssertReport("identifier=/Plugins/Tax.FixedOrByCountryStateZip/Views/_ViewImports.cshtml", "");

            AssertReport("identifiers:Widgets.GoogleAnalytics.count=2",
                "expected Configure.cshtml + _ViewImports.cshtml under " +
                "/Plugins/Widgets.GoogleAnalytics/Views/.");
            AssertReport("identifier=/Plugins/Widgets.GoogleAnalytics/Views/Configure.cshtml", "");
            AssertReport("identifier=/Plugins/Widgets.GoogleAnalytics/Views/_ViewImports.cshtml", "");

            AssertReport("identifiers:Widgets.NivoSlider.count=3",
                "expected Configure.cshtml + PublicInfo.cshtml + _ViewImports.cshtml under " +
                "/Plugins/Widgets.NivoSlider/Views/.");
            AssertReport("identifier=/Plugins/Widgets.NivoSlider/Views/Configure.cshtml", "");
            AssertReport("identifier=/Plugins/Widgets.NivoSlider/Views/PublicInfo.cshtml", "");
            AssertReport("identifier=/Plugins/Widgets.NivoSlider/Views/_ViewImports.cshtml", "");

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
            //task 11.1 / 11.2, verbatim from the ported controllers
            AssertReport("getView:~/Plugins/ExternalAuth.Facebook/Views/Configure.cshtml=True", "");
            AssertReport("getView:~/Plugins/ExternalAuth.Facebook/Views/PublicInfo.cshtml=True", "");
            AssertReport("getView:~/Plugins/Feed.GoogleShopping/Views/Configure.cshtml=True", "");
            //task 15.1 / 15.2 / 15.3, verbatim from the ported controllers (the Tax partials are
            //resolved through Html.PartialAsync at the exact ~/Plugins/... strings Configure.cshtml
            //passes, so the view engine must find all three)
            AssertReport("getView:~/Plugins/Tax.FixedOrByCountryStateZip/Views/Configure.cshtml=True", "");
            AssertReport("getView:~/Plugins/Tax.FixedOrByCountryStateZip/Views/_FixedRate.cshtml=True", "");
            AssertReport("getView:~/Plugins/Tax.FixedOrByCountryStateZip/Views/_CountryStateZip.cshtml=True", "");
            AssertReport("getView:~/Plugins/Widgets.GoogleAnalytics/Views/Configure.cshtml=True", "");
            AssertReport("getView:~/Plugins/Widgets.NivoSlider/Views/Configure.cshtml=True", "");
            AssertReport("getView:~/Plugins/Widgets.NivoSlider/Views/PublicInfo.cshtml=True", "");
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
            foreach (var asm in AllMigrated)
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
                "Plugins/DiscountRulesHasOneProduct/LoadProductFriendlyNames",
                //task 11.1
                "Plugins/ExternalAuthFacebook/Login",
                "Plugins/ExternalAuthFacebook/LoginCallback"
            })
                AssertReport("liveEndpoint:" + pattern + ".actions=<none>",
                    "an UNINSTALLED plugin contributed " + pattern + " to the live endpoint set. " +
                    "RoutePublisher's `if (plugin != null && !plugin.Installed) continue;` filter " +
                    "is 3.90's and must not be dropped - it is what keeps an uninstalled plugin's " +
                    "URLs unreachable.");
        }

        #region Task 11.1 / 11.2 — Group A additions

        [Test]
        public void Task_11_1_the_Facebook_route_provider_registered_3_90s_two_routes()
        {
            //Both names are load-bearing. Plugin.ExternalAuth.Facebook.Login is what
            //Views/PublicInfo.cshtml resolves the login button's href by — a rename breaks the
            //button with no compile error. LoginCallback's PATTERN is what
            //FacebookProviderAuthorizer.GenerateLocalCallbackUri builds by hand and sends to
            //Facebook as redirect_uri, so it must keep matching that string — and it is also every
            //existing deployment's registered OAuth redirect URI.
            AssertReport("routeProviders:" + FacebookAsm + ".count=1",
                "ExternalAuth.Facebook's IRouteProvider was not found.");
            AssertReport("routeProviders:" + FacebookAsm + ".priority=0",
                "Priority changed from 3.90's 0, which reorders it against every other provider.");

            AssertReport("endpoint:Plugins/ExternalAuthFacebook/Login.actions=" +
                "Nop.Plugin.ExternalAuth.Facebook.Controllers.ExternalAuthFacebookController.Login",
                "the ported route does not reach the plugin's Login action.");
            AssertReport("endpoint:Plugins/ExternalAuthFacebook/Login.routeNames=" +
                "Plugin.ExternalAuth.Facebook.Login",
                "the route NAME is not registered. Views/PublicInfo.cshtml calls " +
                "Url.RouteUrl(\"Plugin.ExternalAuth.Facebook.Login\") for the button's href, so a " +
                "renamed route produces an empty href at runtime with no error anywhere.");

            AssertReport("endpoint:Plugins/ExternalAuthFacebook/LoginCallback.actions=" +
                "Nop.Plugin.ExternalAuth.Facebook.Controllers.ExternalAuthFacebookController.LoginCallback",
                "the ported callback route does not reach LoginCallback.");
            AssertReport("endpoint:Plugins/ExternalAuthFacebook/LoginCallback.routeNames=" +
                "Plugin.ExternalAuth.Facebook.LoginCallback", "the route name changed.");
        }

        [Test]
        public void Task_11_2_GoogleShopping_ships_no_route_provider_and_that_is_faithful()
        {
            //Feed.GoogleShopping has NO RouteProvider.cs in 3.90 and none was invented here. Its
            //Configure page is reached through the admin's own ConfigureMiscPlugin action plus the
            //Html.Action bridge, and its two Kendo grid endpoints through the Default
            //{controller}/{action} route via Url.Action — which is why GoogleProductList and
            //GoogleProductUpdate are deliberately NOT marked [NopChildActionOnly]. Asserted so a
            //later reader does not "fix" the absence.
            AssertReport("routeProviders:" + GoogleShoppingAsm + ".count=0",
                "an IRouteProvider appeared in Feed.GoogleShopping. 3.90 has none; adding one would " +
                "change the URLs its Kendo grid posts to.");
        }

        [Test]
        public void Task_11_x_the_two_new_plugins_deploy_their_static_assets_and_nothing_else()
        {
            //The deployment-shape assertion for the two plugins that are the FIRST to ship a static
            //tree. The SERVING half is PluginStaticAssetTests; this is the half that proves the
            //files are there to serve — under Microsoft.NET.Sdk.Razor the default Content glob
            //covers only .cshtml/.razor, so .css/.png arrive as <None> and a bare <Content Update>
            //silently matches nothing.
            foreach (var asm in new[] { FacebookAsm, GoogleShoppingAsm })
            {
                AssertReport("plugin:" + asm + ".strayNopDlls=<none>",
                    asm + " deploys another Nop.* assembly. Feed.GoogleShopping is the sharp case: " +
                    "it references Nop.Data DIRECTLY, so Nop.Data.dll would be copied by name rather " +
                    "than by transitive accident, and PluginManager would load the stale copy as the " +
                    "process's Nop.Data.");
                AssertReport("plugin:" + asm + ".deployDirParent=Plugins", "");
                AssertReport("plugin:" + asm + ".descriptionTxtDeployed=True", "");
                AssertReport("plugin:" + asm + ".logoDeployed=True", "");
                AssertReport("plugin:" + asm + ".deployedConfigCount=0",
                    "a .config reached " + asm + "'s deployment folder. web.config, app.config and " +
                    "packages.config are deleted by tasks 11.1/11.2.");
                AssertReport("plugin:" + asm + ".deployedCshtmlCount=0", "");
            }

            //TASK 11.2 - the embedded taxonomy list, which is neither a deployed file nor a view and
            //so is invisible to every other assertion here. GoogleService.GetTaxonomyList() reads it
            //by MANIFEST NAME; a lost <EmbeddedResource> item means a silently empty "Default Google
            //category" dropdown and NopException("Default Google category is not set") on every feed
            //generation. Note taxonomy.txt is deliberately NOT deployed loose, so its absence from
            //the deployment folder is correct rather than the defect.
            AssertReport("plugin:" + GoogleShoppingAsm + ".taxonomyResourcePresent=True",
                "Nop.Plugin.Feed.GoogleShopping.Files.taxonomy.txt is not an embedded resource of " +
                "the built assembly. Check <EmbeddedResource Include=\"Files\\taxonomy.txt\" /> and " +
                "that RootNamespace still derives that exact manifest name - GoogleService passes " +
                "the string literally.");
            Assert.IsFalse((_report ?? string.Empty).Split('\n').Select(l => l.Trim())
                    .Any(l => l == "plugin:" + GoogleShoppingAsm + ".taxonomyCategoryCount=0"),
                "the embedded taxonomy.txt is present but parses to ZERO categories, which is the " +
                "same observable failure as it being absent." + Environment.NewLine + _report);
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 11.x-1 — the Html.Action bridge's action selection and model binding
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_11_x_1_the_bridge_evaluates_action_constraints_at_all()
        {
            //The two mechanisms exist, and MVC really does materialise the metadata they consume.
            //Asserted separately from the behaviour below because if MVC ever stopped producing
            //HttpMethodActionConstraint / FormValueRequiredAttribute entries the selection fix would
            //quietly become a no-op while still "being present".
            AssertReport("childAction.findActionPresent=True",
                "ChildActionExtensions.FindAction is gone or was renamed; the probe cannot drive " +
                "the bridge's real selection path and every assertion below is vacuous.");
            AssertReport("childAction.applyActionConstraintsPresent=True",
                "ChildActionExtensions.ApplyActionConstraints is gone. The bridge is back to matching " +
                "on action+controller NAME and tie-breaking by fewest parameters, which always " +
                "selects a plugin's parameterless GET Configure — so every plugin settings form " +
                "silently discards the administrator's input (runtime deferral 11.x-1).");
            AssertReport("childAction.populateValueProviderFactoriesPresent=True",
                "ChildActionExtensions.PopulateValueProviderFactories is gone. A hand-built " +
                "ControllerContext has ZERO value provider factories, so TryUpdateModelAsync and " +
                "BindComplexParameter bind nothing AND REPORT SUCCESS.");

            //Facebook: Configure() and [HttpPost] Configure(ConfigurationModel)
            AssertReport("childAction:ExternalAuthFacebook.Configure.candidateCount=2",
                "expected the GET and the [HttpPost] overload.");
            AssertReport("childAction:ExternalAuthFacebook.Configure.candidate=" +
                "Configure(ConfigurationModel) constraints=HttpMethodActionConstraint",
                "the [HttpPost] Configure overload carries no HttpMethodActionConstraint, so there " +
                "is nothing for the selection fix to read.");

            //GoogleShopping: three actions named Configure, two of them [HttpPost] with ONE
            //parameter each, distinguished ONLY by [FormValueRequired].
            AssertReport("childAction:FeedGoogleShopping.Configure.candidateCount=3",
                "expected Configure(), [HttpPost][FormValueRequired(\"save\")] Configure(model) and " +
                "[HttpPost, ActionName(\"Configure\")][FormValueRequired(\"generate\")] " +
                "GenerateFeed(model).");
            foreach (var expected in new[]
            {
                "childAction:FeedGoogleShopping.Configure.candidate=Configure(FeedGoogleShoppingModel) " +
                "constraints=FormValueRequiredAttribute|HttpMethodActionConstraint",
                "childAction:FeedGoogleShopping.Configure.candidate=GenerateFeed(FeedGoogleShoppingModel) " +
                "constraints=FormValueRequiredAttribute|HttpMethodActionConstraint"
            })
                AssertReport(expected,
                    "Feed.GoogleShopping's two POST overloads are distinguished ONLY by which submit " +
                    "button was pressed, and both take one parameter — so without the " +
                    "FormValueRequired constraint being present AND evaluated, a fewest-parameters " +
                    "tie-break between them is arbitrary: \"Save\" could generate the feed.");
        }

        [Test]
        public void Deferral_11_x_1_a_GET_selects_the_parameterless_overload()
        {
            //This report was produced by a GET (OneTimeSetUp fetches /__smoke/plugins), so MVC 5's
            //ActionMethodSelector semantics say: the [HttpPost] candidates do not accept, the
            //unconstrained one does, and it wins outright. Same answer the pre-11.1 bridge gave —
            //which is the point: the fix must not have changed the GET path, only the POST one.
            AssertReport("childAction.requestMethod=GET", "the setup request was not a GET.");
            AssertReport("childAction:ExternalAuthFacebook.Configure.selected=Configure/0",
                "a GET render of a plugin Configure page must select the parameterless overload.");
            AssertReport("childAction:FeedGoogleShopping.Configure.selected=Configure/0",
                "a GET render of a plugin Configure page must select the parameterless overload.");
        }

        [Test]
        public void Deferral_11_x_1_a_POST_selects_the_HttpPost_overload_and_binds_the_form()
        {
            //*** THE ASSERTION THE WHOLE FIX EXISTS FOR, and it is the counterfactual of the GET
            //test above: the SAME probe, the SAME descriptors, a DIFFERENT request, a DIFFERENT
            //answer. A mechanism that returned a constant would fail one of the two. ***
            //
            //Before task 11.1 both of these reported Configure/0 and boundText=<null>, i.e. the
            //administrator's input was discarded with no error anywhere.
            var report = PostProbe(new Dictionary<string, string>
            {
                { "save", "Save" },
                { "ProbeText", "hello-from-the-form" },
                { "ProbeNumber", "4242" }
            });

            AssertLine(report, "childAction.requestMethod=POST", "the probe request was not a POST.");
            AssertLine(report, "childAction.hasFormContentType=True", "the probe sent no form.");

            AssertLine(report, "childAction:ExternalAuthFacebook.Configure.selected=Configure/1",
                "a POST render of ExternalAuth.Facebook's Configure page did NOT select the " +
                "[HttpPost] Configure(ConfigurationModel) overload, so pressing Save on the plugin's " +
                "settings form re-runs the GET and discards every change. Runtime deferral 11.x-1.");

            //"save" is in the form, so [FormValueRequired("save")] Configure(model) accepts and
            //[FormValueRequired("generate")] GenerateFeed(model) does not.
            AssertLine(report, "childAction:FeedGoogleShopping.Configure.selected=Configure/1",
                "with \"save\" in the form, Feed.GoogleShopping's Configure(model) must be the only " +
                "candidate — GenerateFeed's [FormValueRequired(\"generate\")] must have refused.");

            //and the model really was bound from the form, through the bridge's OWN
            //CreateController + ExecuteAction path rather than through its helpers directly
            Assert.IsFalse(report.Split('\n').Select(l => l.Trim())
                    .Any(l => l == "childAction.valueProviderFactories=0"),
                "CreateController installed NO value provider factories, so every bind below reads " +
                "an empty request and reports success. A hand-built ControllerContext starts with " +
                "zero; PopulateValueProviderFactories is what copies them from MvcOptions." +
                Environment.NewLine + report);

            AssertLine(report, "childAction.bindParameterArity=3",
                "ChildActionExtensions.BindParameter no longer takes the ControllerBase it needs in " +
                "order to bind a complex parameter.");
            AssertLine(report, "childAction.bindParameterResult=ChildActionBindProbeModel",
                "the bridge handed the action NULL for a COMPLEX parameter, which is the pre-11.1 " +
                "behaviour: a plugin's Configure(TModel) child action received null and saved " +
                "nothing.");
            AssertLine(report, "childAction.boundText=hello-from-the-form",
                "the bridge constructed the model but did not bind it from the request.");
            AssertLine(report, "childAction.boundNumber=4242", "a non-string property did not bind.");
        }

        [Test]
        public void Deferral_11_x_1_the_generate_button_selects_GenerateFeed_not_Configure()
        {
            //The other half of the FormValueRequired discrimination, and the one that would have been
            //a WRONG ACTION rather than a no-op: both candidates are [HttpPost] with one parameter,
            //so verb-awareness alone does not separate them. A regression to Configure/1 means
            //pressing "Generate feed" silently saves the settings instead; a regression to
            //Configure/0 means it does nothing at all.
            var report = PostProbe(new Dictionary<string, string>
            {
                { "generate", "Generate feed" },
                { "ProbeText", "generate-branch" }
            });

            AssertLine(report, "childAction:FeedGoogleShopping.Configure.selected=GenerateFeed/1",
                "with \"generate\" in the form, [FormValueRequired(\"generate\")] GenerateFeed must be " +
                "the only candidate. Selecting Configure instead means the \"Generate feed\" button " +
                "saves settings and never generates a feed.");

            //Facebook declares no FormValueRequired at all, so its answer must be unchanged by the
            //button name - which is what shows the discrimination above is FormValueRequired doing
            //the work rather than something incidental about the form.
            AssertLine(report, "childAction:ExternalAuthFacebook.Configure.selected=Configure/1",
                "ExternalAuth.Facebook declares no [FormValueRequired], so any POST must select its " +
                "single [HttpPost] overload.");
        }

        /// <summary>
        /// Re-runs the <c>/__smoke/plugins</c> probe as a POST with the given form fields, so the
        /// child-action selection and binding can be observed reacting to a real request.
        /// </summary>
        private string PostProbe(Dictionary<string, string> form)
        {
            var response = _client.PostAsync(SmokeProbeMiddleware.Prefix + "plugins",
                new FormUrlEncodedContent(form)).Result;
            var report = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("---- POST /__smoke/plugins " +
                string.Join("&", form.Select(kv => kv.Key + "=" + kv.Value)) + " ----");
            TestContext.WriteLine(report);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "the probe endpoint did not answer a POST.");
            return report;
        }

        /// <summary>
        /// <see cref="AssertReport"/> against an ad-hoc report rather than the one from setup.
        /// </summary>
        private static void AssertLine(string report, string expected, string why)
        {
            var lines = (report ?? string.Empty).Split('\n').Select(l => l.Trim());
            Assert.IsTrue(lines.Any(l => l == expected),
                why + Environment.NewLine + "expected line: " + expected +
                Environment.NewLine + "full report:" + Environment.NewLine + report);
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 4.10 — the plugin's own DbContext and the GO-batched create script
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_4_10_the_plugin_create_script_is_GO_batched_and_is_split_before_execution()
        {
            //*** RUNTIME DEFERRAL 4.10, RESOLVED FOR THE FIRST OF FOUR PLUGIN CONTEXTS, WITHOUT A
            //DATABASE. *** GenerateCreateScript is a MODEL operation - it needs a provider selected
            //but never opens a connection - so the whole deferral is observable here rather than only
            //against an installed store.
            //
            //3.90's Install() was Database.ExecuteSqlCommand(CreateDatabaseScript()), one command for
            //the whole script, which was correct for EF6 because ObjectContext.CreateDatabaseScript()
            //emitted a single unseparated batch. Task 3.2 substituted EF Core's
            //Database.GenerateCreateScript(), whose output separates statements with GO - a CLIENT
            //directive, not T-SQL. Sending it as one command throws "Incorrect syntax near 'GO'".
            AssertReport("pluginContext.assemblyLoaded=True",
                "Feed.GoogleShopping is not loaded, so nothing below is measured.");
            AssertReport("pluginContext.typePresent=True",
                "GoogleProductObjectContext is gone or was renamed.");
            AssertReport("pluginContext.stringCtorPresent=True",
                "GoogleProductObjectContext lost its (string nameOrConnectionString) constructor. " +
                "Nop.Web.Framework's RegisterPluginDataContext constructs it with " +
                "Activator.CreateInstance and exactly that signature, so this fails at RUNTIME with " +
                "MissingMethodException and no compile error anywhere.");

            //THE PREMISE: the raw script really does carry a bare GO that SQL Server would reject as
            //a syntax error. MEASURED SHAPE, and it is NARROWER than deferral 4.10's wording
            //suggests: for this one-table model EF Core emits ONE statement followed by a TRAILING
            //GO, so 1 batch out is correct and the failure 3.90's code would have hit is the
            //trailing directive rather than a mid-script separator. If EF Core ever stopped emitting
            //GO at all this assertion fails, rather than the fix silently becoming pointless-looking.
            AssertReport("pluginContext.rawScriptWouldBeRejected=True",
                "EF Core's create script contains NO GO line, so deferral 4.10's premise no longer " +
                "holds for this context. Re-read the deferral before simplifying Install().");

            //THE FIX: whatever the shape, no batch handed to ExecuteSqlRaw still carries a GO.
            Assert.IsFalse((_report ?? string.Empty).Split('\n').Select(l => l.Trim())
                    .Any(l => l == "pluginContext.batchCount=0"),
                "Nop.Data.DbContextExtensions.SplitSqlIntoBatches produced NO batches from a " +
                "non-empty create script, so Install() would create nothing at all." +
                Environment.NewLine + _report);
            AssertReport("pluginContext.batchesWithBareGo=0",
                "a batch still contains a bare GO line, which SQL Server rejects as a syntax error. " +
                "This is exactly what 3.90's Database.ExecuteSqlCommand(CreateDatabaseScript()) " +
                "would have sent.");
            AssertReport("pluginContext.batchesCreatingGoogleProduct=1",
                "no single batch creates the GoogleProduct table. Either the table name changed " +
                "(GoogleProductRecordMap.ToTable is load-bearing - Uninstall() resolves it back " +
                "through GetTableName<GoogleProductRecord>() and drops it) or the script was split " +
                "in the middle of the CREATE TABLE.");
        }

        [Test]
        public void Task_11_2_the_plugin_context_model_holds_ONLY_the_plugins_own_entity()
        {
            //OnModelCreating calls ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()).
            //If that ever widened to Nop.Data's assembly, this context's model would gain all ~105
            //nopCommerce entities and Install() would generate a create script for the WHOLE
            //nopCommerce schema - executed against a live store by an administrator pressing
            //"Install" on a feed plugin.
            AssertReport("pluginContext.entityTypeCount=1",
                "the plugin context's model holds more than its own entity. Check that " +
                "OnModelCreating passes Assembly.GetExecutingAssembly() and has not been widened to " +
                "Nop.Data's maps.");
            AssertReport("pluginContext.entityTypes=GoogleProductRecord", "");
            AssertReport("pluginContext.tableNames=GoogleProduct",
                "the mapped table name is not 3.90's GoogleProduct. GoogleProductRecordMap.ToTable " +
                "is load-bearing beyond the model: Uninstall() resolves the name back through " +
                "IDbContext.GetTableName<GoogleProductRecord>() and DROPs it, so an EF Core " +
                "naming-convention default here would drop the wrong table (or none).");
        }

        [Test]
        public void Deferral_4_10_the_shared_splitter_is_deliberately_conservative()
        {
            //The helper's contract, on inputs EF Core's generator does not produce, so a future
            //"improvement" cannot quietly change what a batch is for the other three plugin contexts
            //that will use it (tasks 13.1, 14.4, 15.1).
            //
            //Input: "CREATE TABLE...;" GO, an EMPTY batch, "SELECT 'GO';" GO
            //  -> 2 batches (the empty one dropped, the trailing GO producing nothing), and the GO
            //     inside the string literal untouched.
            AssertReport("splitSql.craftedBatchCount=2",
                "the splitter no longer drops empty batches or no longer treats a lone GO line as a " +
                "separator. An empty batch sent as a command is a syntax error.");
            AssertReport("splitSql.craftedBatchesKeepStringLiteralGo=1",
                "the splitter cut on a GO inside a STRING LITERAL. Only a line that is exactly GO " +
                "after trimming is a separator - anything cleverer would need a T-SQL lexer and " +
                "would be speculation.");
            AssertReport("splitSql.emptyInputBatchCount=0",
                "whitespace input produced a batch, which would be sent to the server as an empty " +
                "command.");
        }

        // -----------------------------------------------------------------------------------
        // Task 13.1 — Nop.Plugin.Pickup.PickupInStore (its own DbContext + RouteProvider)
        // -----------------------------------------------------------------------------------

        [Test]
        public void Task_13_1_pickup_is_discovered_loaded_and_contributes_both_application_parts()
        {
            //Discovery/compatibility/load-context are asserted for every plugin in AllMigrated by
            //Task_10_x_all_three_plugins_are_discovered_and_version_compatible; this pins the two
            //facts specific to THIS plugin having views + its 3.90 system name.
            AssertReport("plugin:" + PickupInStoreAsm + ".discovered=True",
                "Pickup.PickupInStore was not discovered - check OutputPath and " +
                "AppendTargetFrameworkToOutputPath=false.");
            AssertReport("plugin:" + PickupInStoreAsm + ".supportsCurrentVersion=True",
                "Description.txt SupportedVersions must list NopVersion.CurrentVersion (3.90).");
            //It contributes BOTH parts: the AssemblyPart (controllers routable) and the
            //CompiledRazorAssemblyPart (its 4 views resolvable). Without AddRazorSupportForMvc=true
            //only the AssemblyPart would appear - deferral 1.2, measured.
            AssertReport("part:" + PickupInStoreAsm + "=AssemblyPart",
                PickupInStoreAsm + " is not an MVC AssemblyPart, so its controller is not routable.");
            AssertReport("part:" + PickupInStoreAsm + "=CompiledRazorAssemblyPart",
                PickupInStoreAsm + " contributed no Razor part, so none of its views can be found. " +
                "Check Sdk=\"Microsoft.NET.Sdk.Razor\" AND <AddRazorSupportForMvc>true</> in its " +
                "project file - deferral 1.2.");
        }

        [Test]
        public void Task_13_1_compiled_view_identifiers_are_3_90s_Plugins_paths()
        {
            //The five compiled Razor identifiers, at 3.90's ~/Plugins/... paths, produced by the
            //Content/Link block. If the Link metadata were missing they would be under /Views/;
            //if it repeated "Views\" they would be doubled. Measured on the real assembly via the
            //live ApplicationPartManager.
            AssertReport("identifiers:Pickup.PickupInStore.count=5",
                "expected Configure + Create + Edit + _CreateOrUpdate + _ViewImports under " +
                "/Plugins/Pickup.PickupInStore/Views/.");
            foreach (var id in new[]
            {
                "/Plugins/Pickup.PickupInStore/Views/Configure.cshtml",
                "/Plugins/Pickup.PickupInStore/Views/Create.cshtml",
                "/Plugins/Pickup.PickupInStore/Views/Edit.cshtml",
                "/Plugins/Pickup.PickupInStore/Views/_CreateOrUpdate.cshtml",
                "/Plugins/Pickup.PickupInStore/Views/_ViewImports.cshtml"
            })
                AssertReport("identifier=" + id,
                    "the compiled identifier " + id + " is not present at 3.90's path.");
        }

        [Test]
        public void Task_13_1_the_real_view_engine_finds_every_path_the_controller_and_views_pass()
        {
            //The four ~/Plugins/... strings the ported controller and views pass, driven through
            //the real IRazorViewEngine via the probe's caller-supplied ?getView= parameter (so no
            //edit to the probe's hardcoded list is needed - which keeps this change off the lines
            //groups 14/15 are also editing).
            foreach (var path in new[]
            {
                "~/Plugins/Pickup.PickupInStore/Views/Configure.cshtml",
                "~/Plugins/Pickup.PickupInStore/Views/Create.cshtml",
                "~/Plugins/Pickup.PickupInStore/Views/Edit.cshtml",
                "~/Plugins/Pickup.PickupInStore/Views/_CreateOrUpdate.cshtml"
            })
            {
                var report = GetProbeWithGetView(path);
                AssertLine(report, "getView:" + path + "=True",
                    "the real view engine did not resolve " + path + ". If the Content/Link " +
                    "identifier is wrong the controller's View(\"" + path + "\") throws at runtime.");
            }
        }

        [Test]
        public void Deferral_8_2_3_pickups_admin_layout_and_partial_resolve_and_the_old_paths_do_not()
        {
            //DEFERRAL 8.2-3, THREE SITES in this plugin: Configure.cshtml named
            //_GridPagerMessages.cshtml, Create.cshtml and Edit.cshtml named _AdminPopupLayout.cshtml,
            //all under the PRE-8.2 ~/Administration/Views/... tree. Rewritten to
            //~/Areas/Admin/Views/... (cross-assembly references compiled into Nop.Admin.dll). The
            //getView facts for both the new and old paths are already in the setup report (the
            //probe's hardcoded list checks exactly these), so assert them there.
            AssertReport("getView:~/Areas/Admin/Views/Shared/_AdminPopupLayout.cshtml=True",
                "the admin popup layout does not resolve, so Create/Edit cannot render - the " +
                "8.2-3 rewrite target is unreachable.");
            AssertReport("getView:~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml=True",
                "the Kendo grid pager partial does not resolve, so Configure cannot render.");
            //...and the pre-8.2 paths must NOT resolve, which is what proves the rewrite was needed.
            AssertReport("getView:~/Administration/Views/Shared/_AdminPopupLayout.cshtml=False",
                "the PRE-8.2 admin path still resolves; the 8.2-3 rewrite would then be unnecessary.");
            AssertReport("getView:~/Administration/Views/Shared/_GridPagerMessages.cshtml=False",
                "the PRE-8.2 admin path still resolves.");
        }

        [Test]
        public void Task_13_1_no_ViewStart_applies_to_pickups_Configure_which_sets_no_layout()
        {
            //*** THE SPECIFIC VIEW THE RELOCATION ALTERNATIVE WOULD HAVE BROKEN. ***
            //Pickup.PickupInStore/Views/Configure.cshtml assigns NO Layout at all - the one view in
            //the solution that relies on no _ViewStart applying. Because the Content/Link block
            //keeps its identifier under /Plugins/... (whose ancestors have no _ViewStart), none
            //applies, and it renders as a bare admin panel - correct. Had the views been relocated
            //under /Views/<Controller>/, Nop.Web's /Views/_ViewStart.cshtml
            //(Layout="~/Views/Shared/_ColumnsOne.cshtml") would have wrapped it in the storefront's
            //one-column layout. Deferral 83.1.
            AssertReport("viewStartsApplyingTo:/Plugins/Pickup.PickupInStore/Views/Configure.cshtml=<none>",
                "a _ViewStart now applies to Pickup's Configure.cshtml, which sets no Layout - it " +
                "would silently render inside whatever layout that _ViewStart names.");
        }

        [Test]
        public void Task_13_1_the_ported_route_provider_registered_3_90s_two_named_routes()
        {
            //IRouteProvider is now void RegisterRoutes(IEndpointRouteBuilder); patterns, route NAMES
            //and defaults are 3.90's, only the string[] namespaces argument dropped. Both names are
            //load-bearing: Configure.cshtml resolves the Create/Edit popup URLs by
            //Url.RouteUrl("Plugin.Pickup.PickupInStore.Create"/".Edit"). Driven against a scratch
            //IEndpointRouteBuilder over the real service provider (the live EndpointDataSource
            //carries no plugin route until the plugin is INSTALLED).
            AssertReport("routeProviders:" + PickupInStoreAsm + ".count=1",
                "Pickup.PickupInStore's IRouteProvider was not found.");
            AssertReport("routeProviders:" + PickupInStoreAsm + ".priority=0", "3.90's Priority was 0.");

            AssertReport("endpoint:Plugins/PickupInStore/Create.actions=" +
                "Nop.Plugin.Pickup.PickupInStore.Controllers.PickupInStoreController.Create",
                "the ported Create route does not reach the controller's Create action.");
            AssertReport("endpoint:Plugins/PickupInStore/Create.routeNames=Plugin.Pickup.PickupInStore.Create",
                "the Create route name changed; Configure.cshtml resolves the popup URL by that name.");
            AssertReport("endpoint:Plugins/PickupInStore/Edit.actions=" +
                "Nop.Plugin.Pickup.PickupInStore.Controllers.PickupInStoreController.Edit",
                "the ported Edit route does not reach the controller's Edit action.");
            AssertReport("endpoint:Plugins/PickupInStore/Edit.routeNames=Plugin.Pickup.PickupInStore.Edit",
                "the Edit route name changed; Configure.cshtml resolves the edit-button URL by it.");
        }

        [Test]
        public void Task_13_1_pickup_deploys_only_its_own_assembly_and_runtime_content()
        {
            //Private="false" + NopPluginDoNotDeployHostAssemblies: no OTHER Nop.* dll next to the
            //plugin. This plugin is a sharp case like Feed.GoogleShopping - it references Nop.Data
            //DIRECTLY - so a missing filter would leak Nop.Data.dll and PluginManager would load
            //the stale copy as the process's Nop.Data.
            AssertReport("plugin:" + PickupInStoreAsm + ".strayNopDlls=<none>",
                PickupInStoreAsm + " deploys another Nop.* assembly - it references Nop.Data " +
                "directly, so Nop.Data.dll would be copied by name.");
            AssertReport("plugin:" + PickupInStoreAsm + ".deployDirParent=Plugins",
                "the deployment folder's parent is not \"Plugins\" - PluginManager rejects it. " +
                "Check OutputPath and AppendTargetFrameworkToOutputPath=false.");
            AssertReport("plugin:" + PickupInStoreAsm + ".descriptionTxtDeployed=True", "");
            AssertReport("plugin:" + PickupInStoreAsm + ".logoDeployed=True",
                "logo.png (this plugin ships .png, not .jpg) did not deploy.");
            AssertReport("plugin:" + PickupInStoreAsm + ".deployedConfigCount=0",
                "a .config reached the deployment folder; web/app/packages.config are deleted at 13.1.");
            AssertReport("plugin:" + PickupInStoreAsm + ".deployedCshtmlCount=0",
                "a loose .cshtml was deployed; the views are compiled into the dll now.");
        }

        [Test]
        public void Deferral_4_10_pickups_create_script_is_GO_batched_and_split_before_execution()
        {
            //RUNTIME DEFERRAL 4.10 for THIS plugin's context, without a database.
            //GenerateCreateScript is a model operation. Same shape as the GoogleShopping proof.
            AssertReport("pickupContext.assemblyLoaded=True",
                "Pickup.PickupInStore is not loaded, so nothing below is measured.");
            AssertReport("pickupContext.typePresent=True",
                "StorePickupPointObjectContext is gone or was renamed.");
            AssertReport("pickupContext.stringCtorPresent=True",
                "StorePickupPointObjectContext lost its (string nameOrConnectionString) ctor. " +
                "RegisterPluginDataContext constructs it reflectively with exactly that signature, " +
                "so losing it fails at RUNTIME with MissingMethodException.");
            AssertReport("pickupContext.rawScriptWouldBeRejected=True",
                "EF Core's create script contains NO GO line, so deferral 4.10's premise no longer " +
                "holds for this context. Re-read the deferral before simplifying Install().");
            Assert.IsFalse((_report ?? string.Empty).Split('\n').Select(l => l.Trim())
                    .Any(l => l == "pickupContext.batchCount=0"),
                "SplitSqlIntoBatches produced NO batches from a non-empty create script, so " +
                "Install() would create nothing at all." + Environment.NewLine + _report);
            AssertReport("pickupContext.batchesWithBareGo=0",
                "a batch still contains a bare GO line, which SQL Server rejects - exactly what " +
                "3.90's Database.ExecuteSqlCommand(CreateDatabaseScript()) would have sent.");
            AssertReport("pickupContext.batchesCreatingTable=1",
                "no single batch creates the StorePickupPoint table. Either the table name changed " +
                "(StorePickupPointMap.ToTable is load-bearing - Uninstall resolves it back through " +
                "GetTableName<StorePickupPoint>() and drops it) or the CREATE TABLE was split.");
        }

        [Test]
        public void Task_13_1_the_pickup_context_model_holds_ONLY_the_plugins_own_entity()
        {
            //OnModelCreating calls ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()).
            //If that ever widened to Nop.Data's assembly, this context's model would gain all ~105
            //nopCommerce entities and Install() would generate a create script for the WHOLE
            //nopCommerce schema against a live store.
            AssertReport("pickupContext.entityTypeCount=1",
                "the plugin context's model holds more than its own entity - the assembly-scoped " +
                "ApplyConfigurationsFromAssembly was probably widened to Nop.Data's maps.");
            AssertReport("pickupContext.entityTypes=StorePickupPoint", "");
            AssertReport("pickupContext.tableNames=StorePickupPoint",
                "the mapped table name is not 3.90's StorePickupPoint - StorePickupPointMap.ToTable " +
                "is load-bearing (Uninstall drops the resolved name).");
        }

        // -----------------------------------------------------------------------------------
        // Task 15.1 / 15.2 / 15.3 — Tax.FixedOrByCountryStateZip, Widgets.GoogleAnalytics,
        // Widgets.NivoSlider. Discovery/compatibility/parts/identifiers/getView are asserted for
        // all three above (they are in ViewBearing and AllMigrated, and the probe's hardcoded
        // getView list carries their controller paths). These tests pin the facts specific to this
        // group: the Tax DbContext (deferral 4.10, the 4th and last plugin context), the Tax route
        // provider + 8.2-3 rewrites, and each plugin's deployment shape.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_4_10_tax_create_script_is_GO_batched_and_split_before_execution()
        {
            //RUNTIME DEFERRAL 4.10 for the FOURTH AND LAST plugin context, without a database.
            //GenerateCreateScript is a model operation. Same shape as the GoogleShopping/Pickup
            //proofs: 3.90's Install() was Database.ExecuteSqlCommand(CreateDatabaseScript()), which
            //throws "Incorrect syntax near 'GO'" against the EF Core create script; the ported
            //Install() goes through the shared Nop.Data.DbContextExtensions.ExecuteSqlScript.
            AssertReport("taxContext.assemblyLoaded=True",
                "Tax.FixedOrByCountryStateZip is not loaded, so nothing below is measured.");
            AssertReport("taxContext.typePresent=True",
                "CountryStateZipObjectContext is gone or was renamed.");
            AssertReport("taxContext.stringCtorPresent=True",
                "CountryStateZipObjectContext lost its (string nameOrConnectionString) ctor. " +
                "Nop.Web.Framework's RegisterPluginDataContext constructs it reflectively with " +
                "exactly that signature, so losing it fails at RUNTIME with MissingMethodException " +
                "and no compile error anywhere.");
            AssertReport("taxContext.rawScriptWouldBeRejected=True",
                "EF Core's create script contains NO GO line, so deferral 4.10's premise no longer " +
                "holds for this context. Re-read the deferral before simplifying Install().");
            Assert.IsFalse((_report ?? string.Empty).Split('\n').Select(l => l.Trim())
                    .Any(l => l == "taxContext.batchCount=0"),
                "SplitSqlIntoBatches produced NO batches from a non-empty create script, so " +
                "Install() would create nothing at all." + Environment.NewLine + _report);
            AssertReport("taxContext.batchesWithBareGo=0",
                "a batch still contains a bare GO line, which SQL Server rejects - exactly what " +
                "3.90's Database.ExecuteSqlCommand(CreateDatabaseScript()) would have sent.");
            AssertReport("taxContext.batchesCreatingTable=1",
                "no single batch creates the TaxRate table. Either the table name changed " +
                "(TaxRateMap.ToTable is load-bearing - Uninstall resolves it back through " +
                "GetTableName<TaxRate>() and drops it) or the CREATE TABLE was split.");
        }

        [Test]
        public void Task_15_1_the_tax_context_model_holds_ONLY_the_plugins_own_entity()
        {
            //OnModelCreating calls ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()).
            //If that ever widened to Nop.Data's assembly, this context's model would gain all ~105
            //nopCommerce entities and Install() would generate a create script for the WHOLE
            //nopCommerce schema against a live store.
            AssertReport("taxContext.entityTypeCount=1",
                "the plugin context's model holds more than its own entity - the assembly-scoped " +
                "ApplyConfigurationsFromAssembly was probably widened to Nop.Data's maps.");
            AssertReport("taxContext.entityTypes=TaxRate", "");
            AssertReport("taxContext.tableNames=TaxRate",
                "the mapped table name is not 3.90's TaxRate - TaxRateMap.ToTable is load-bearing " +
                "(Uninstall drops the resolved name).");
        }

        [Test]
        public void Task_15_1_the_ported_tax_route_provider_registered_3_90s_named_route()
        {
            //IRouteProvider is now void RegisterRoutes(IEndpointRouteBuilder); pattern, route NAME
            //and defaults are 3.90's, only the string[] namespaces argument dropped. The name is
            //load-bearing: _CountryStateZip.cshtml resolves the "Add tax rate" AJAX target by
            //Url.RouteUrl("Plugin.Tax.FixedOrByCountryStateZip.AddRateByCountryStateZip"). Driven
            //against a scratch IEndpointRouteBuilder over the real service provider.
            AssertReport("routeProviders:" + TaxCountryStateZipAsm + ".count=1",
                "Tax.FixedOrByCountryStateZip's IRouteProvider was not found.");
            AssertReport("routeProviders:" + TaxCountryStateZipAsm + ".priority=0", "3.90's Priority was 0.");
            AssertReport("endpoint:Plugins/FixedOrByCountryStateZip/AddRateByCountryStateZip.actions=" +
                "Nop.Plugin.Tax.FixedOrByCountryStateZip.Controllers.FixedOrByCountryStateZipController.AddRateByCountryStateZip",
                "the ported route does not reach the controller's AddRateByCountryStateZip action.");
            AssertReport("endpoint:Plugins/FixedOrByCountryStateZip/AddRateByCountryStateZip.routeNames=" +
                "Plugin.Tax.FixedOrByCountryStateZip.AddRateByCountryStateZip",
                "the route name changed; _CountryStateZip.cshtml resolves the Add button URL by it.");
        }

        [Test]
        public void Deferral_8_2_3_tax_grid_pager_partial_resolves_and_the_old_path_does_not()
        {
            //DEFERRAL 8.2-3, TWO SITES in this plugin: _FixedRate.cshtml and _CountryStateZip.cshtml
            //each named ~/Administration/Views/Shared/_GridPagerMessages.cshtml, rewritten to
            //~/Areas/Admin/Views/Shared/... (a cross-assembly reference compiled into Nop.Admin.dll).
            //The getView facts for both the new and old paths are in the setup report.
            AssertReport("getView:~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml=True",
                "the Kendo grid pager partial does not resolve, so the tax grids cannot render.");
            AssertReport("getView:~/Administration/Views/Shared/_GridPagerMessages.cshtml=False",
                "the PRE-8.2 admin path still resolves; the 8.2-3 rewrite would then be unnecessary.");
        }

        [Test]
        public void Task_15_x_each_plugin_deploys_only_its_own_assembly_and_runtime_content()
        {
            //Private="false" + NopPluginDoNotDeployHostAssemblies: no OTHER Nop.* dll next to a
            //plugin. Tax.FixedOrByCountryStateZip is a sharp case like Feed.GoogleShopping/Pickup -
            //it references Nop.Data DIRECTLY - so a missing filter would leak Nop.Data.dll and
            //PluginManager would load the stale copy as the process's Nop.Data.
            foreach (var asm in new[] { TaxCountryStateZipAsm, WidgetsGoogleAnalyticsAsm, WidgetsNivoSliderAsm })
            {
                AssertReport("plugin:" + asm + ".strayNopDlls=<none>",
                    asm + " deploys another Nop.* assembly next to itself.");
                AssertReport("plugin:" + asm + ".deployDirParent=Plugins",
                    "the deployment folder's parent is not \"Plugins\" - PluginManager rejects it. " +
                    "Check OutputPath and AppendTargetFrameworkToOutputPath=false.");
                AssertReport("plugin:" + asm + ".descriptionTxtDeployed=True", "");
                AssertReport("plugin:" + asm + ".logoDeployed=True", "logo.jpg did not deploy.");
                AssertReport("plugin:" + asm + ".deployedConfigCount=0",
                    "a .config reached the deployment folder; web/app/packages.config are deleted at 15.x.");
                AssertReport("plugin:" + asm + ".deployedCshtmlCount=0",
                    "a loose .cshtml was deployed; the views are compiled into the dll now.");
            }
        }

        /// <summary>
        /// Fetches <c>/__smoke/plugins?getView=&lt;path&gt;</c>, driving the real
        /// <see cref="Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine"/> for a caller-supplied path.
        /// Used so this fixture can assert view-engine resolution for its own plugin without editing
        /// the probe's hardcoded path list (which groups 14/15 edit concurrently).
        /// </summary>
        private string GetProbeWithGetView(string viewPath)
        {
            var url = SmokeProbeMiddleware.Prefix + "plugins?getView=" +
                Uri.EscapeDataString(viewPath);
            var report = _client.GetStringAsync(url).Result ?? string.Empty;
            return report;
        }

        #endregion

        #endregion

        #region Group B — task 11.1 / 11.2, requires an installed store

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
            foreach (var asm in AllMigrated)
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
            foreach (var asm in AllMigrated)
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

        [Test]
        public void Task_11_1_the_Facebook_Configure_view_RENDERS_over_HTTP()
        {
            //The end-to-end proof for 11.1's admin view: the plugin's compiled Configure.cshtml,
            //rendered by the real host. It also exercises the @Html.Action("StoreScopeConfiguration",
            //"Setting", new { area = "Admin" }) call at the top of that view — a CROSS-AREA child
            //action from a plugin into Nop.Admin, i.e. the area-awareness task 8.8 added.
            RequireAdmin();
            RequireInstalled(FacebookAsm);

            var response = _client.GetAsync("/ExternalAuthFacebook/Configure").Result;
            var html = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("status=" + (int)response.StatusCode + " len=" + html.Length);

            //[NopChildActionOnly] removes this action from INBOUND matching, which is 3.90's
            //[ChildActionOnly] behaviour (it threw; this 404s). So the direct URL must NOT serve,
            //and the view has to be reached the way the admin reaches it.
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
                "GET /ExternalAuthFacebook/Configure served the bare admin settings panel. " +
                "[NopChildActionOnly] + NopChildActionOnlyConvention must suppress the endpoint " +
                "(deferral 7.3-4).");

            //...through the admin's own ConfigureMethod action, whose view does
            //@Html.Action(Model.ConfigurationActionName, ...) - the bridge.
            var viaAdmin = _client
                .GetAsync("/Admin/ExternalAuthentication/ConfigureMethod?systemName=ExternalAuth.Facebook")
                .Result;
            var adminHtml = viaAdmin.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("via admin: status=" + (int)viaAdmin.StatusCode +
                " len=" + adminHtml.Length);

            Assert.AreEqual(HttpStatusCode.OK, viaAdmin.StatusCode,
                "body: " + adminHtml.Substring(0, Math.Min(2000, adminHtml.Length)));
            //the plugin view's own fields, emitted by Html.NopEditorFor
            StringAssert.Contains("ClientKeyIdentifier", adminHtml,
                "the plugin's Configure view did not render inside the admin page. If the admin page " +
                "rendered but this field is absent, the Html.Action bridge did not find the plugin's " +
                "controller.");
            StringAssert.Contains("ClientSecret", adminHtml, "the plugin's Configure view did not render.");
            //and the cross-area child action ran: StoreScopeConfiguration is Nop.Admin's
            StringAssert.Contains("StoreScope", adminHtml,
                "@Html.Action(\"StoreScopeConfiguration\", \"Setting\", new { area = \"Admin\" }) did " +
                "not render, so the bridge's area preference did not reach Nop.Admin from a plugin view.");
        }

        [Test]
        public void Deferral_11_x_1_saving_the_Facebook_settings_form_REALLY_SAVES()
        {
            //*** THE END-TO-END COUNTERPART of the Group A selection assertions, and the one that
            //would have caught runtime deferral 11.x-1 without any knowledge of the mechanism: POST
            //the plugin's own settings form to the admin URL its Html.BeginForm() targets, then read
            //the value back out of the store. Before task 11.1 this silently kept the old value. ***
            RequireAdmin();
            RequireInstalled(FacebookAsm);

            const string url = "/Admin/ExternalAuthentication/ConfigureMethod?systemName=ExternalAuth.Facebook";
            var marker = "smoke-11-1-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            //the anti-forgery token the plugin's own @Html.AntiForgeryToken() emitted
            var page = _client.GetStringAsync(url).Result;
            var token = ExtractAntiForgeryToken(page);
            TestContext.WriteLine("antiforgery token found: " + (token != null));

            var form = new Dictionary<string, string>
            {
                { "save", "Save" },
                { "ClientKeyIdentifier", marker },
                { "ClientSecret", marker + "-secret" }
            };
            if (token != null)
                form["__RequestVerificationToken"] = token;

            var post = _client.PostAsync(url, new FormUrlEncodedContent(form)).Result;
            var body = post.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("POST status=" + (int)post.StatusCode + " len=" + body.Length);
            Assert.AreEqual(HttpStatusCode.OK, post.StatusCode,
                "body: " + body.Substring(0, Math.Min(2000, body.Length)));

            //The re-rendered form must show the NEW value. That is the whole assertion: the POST
            //selected the [HttpPost] overload, the model bound from the form, SaveSetting ran, and
            //the GET that re-renders read it back.
            StringAssert.Contains(marker, body,
                "the plugin's settings form did not save. The re-rendered page still shows the old " +
                "ClientKeyIdentifier, which is EXACTLY the pre-11.1 symptom: the Html.Action bridge " +
                "selected the parameterless GET Configure overload instead of " +
                "[HttpPost] Configure(ConfigurationModel), so nothing was ever written. See " +
                "ChildActionExtensions.ApplyActionConstraints and runtime deferral 11.x-1.");

            //and independently of the rendered HTML, from the settings store itself
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var saved = settingService.LoadSetting<FacebookExternalAuthSettings>(0);
            TestContext.WriteLine("ClientKeyIdentifier from ISettingService: " + saved.ClientKeyIdentifier);
            Assert.AreEqual(marker, saved.ClientKeyIdentifier,
                "the setting was not persisted. The rendered page may have echoed the posted value " +
                "without saving it.");
        }

        [Test]
        public void Task_11_2_the_GoogleShopping_Configure_view_RENDERS_over_HTTP()
        {
            //11.2's view is the largest Razor change in any plugin: two @helper declarations became
            //@functions methods captured through WebViewPage.Capture, and the deferral 8.2-3
            //_GridPagerMessages partial is a CROSS-ASSEMBLY reference into Nop.Admin.dll. A 200
            //whose body carries all three is the proof.
            RequireAdmin();
            RequireInstalled(GoogleShoppingAsm);

            var viaAdmin = _client
                .GetAsync("/Admin/Plugin/ConfigureMiscPlugin?systemName=PromotionFeed.Froogle").Result;
            var html = viaAdmin.Content.ReadAsStringAsync().Result ?? string.Empty;
            TestContext.WriteLine("status=" + (int)viaAdmin.StatusCode + " len=" + html.Length);

            Assert.AreEqual(HttpStatusCode.OK, viaAdmin.StatusCode,
                "body: " + html.Substring(0, Math.Min(2000, html.Length)));

            //the tab shell, which is what @Html.RenderBootstrapTabHeader/Content produced
            StringAssert.Contains("googlebase-configure", html, "the view body did not render.");
            //TabGeneral's markup - i.e. Capture(TabGeneral) really did write into the tab content.
            //If the @helper -> @functions + Capture conversion had produced an EMPTY HelperResult
            //this is what would be missing, and the page would still be a 200.
            StringAssert.Contains("ProductPictureSize", html,
                "the tab-general pane is empty, so Capture(TabGeneral) wrote nothing. The " +
                "@helper -> @functions conversion must be paired with WebViewPage.Capture, which " +
                "PushWriter-redirects the page output into the consuming helper's writer (task 8.4).");
            //TabOverride's markup, including the Kendo grid
            StringAssert.Contains("products-grid", html, "the tab-override pane is empty.");
            StringAssert.Contains("google-popup-editor", html, "the tab-override pane is empty.");
            //DEFERRAL 8.2-3: the cross-assembly partial from Nop.Admin.dll
            StringAssert.Contains("messages:", html,
                "the ~/Areas/Admin/Views/Shared/_GridPagerMessages.cshtml partial did not render. " +
                "Deferral 8.2-3: the pre-8.2 path ~/Administration/Views/Shared/... matches nothing.");
            //the two grid URLs, generated by Url.Action - these are why GoogleProductList and
            //GoogleProductUpdate are deliberately NOT [NopChildActionOnly]
            StringAssert.Contains("FeedGoogleShopping/GoogleProductList", html,
                "Url.Action(\"GoogleProductList\", ...) produced nothing, so the Kendo grid has no " +
                "read URL.");
            StringAssert.Contains("FeedGoogleShopping/GoogleProductUpdate", html,
                "Url.Action(\"GoogleProductUpdate\", ...) produced nothing.");
            //the Default Google category dropdown, populated from the EMBEDDED taxonomy.txt. Empty
            //here means GetManifestResourceStream returned null, i.e. the EmbeddedResource item was
            //lost and every feed generation would throw "Default Google category is not set".
            StringAssert.Contains("Apparel", html,
                "the Default Google category dropdown is empty, so " +
                "GoogleService.GetTaxonomyList() read no embedded taxonomy.txt. Check the " +
                "<EmbeddedResource Include=\"Files\\taxonomy.txt\" /> item and that RootNamespace " +
                "still derives the manifest name Nop.Plugin.Feed.GoogleShopping.Files.taxonomy.txt.");
        }

        [Test]
        public void Task_11_2_the_two_Kendo_grid_endpoints_are_reachable_by_URL()
        {
            //The complement of Task_11_1_the_Facebook_Configure_view_RENDERS_over_HTTP's 404
            //assertion: these two actions were NEVER [ChildActionOnly] in 3.90 and are called by URL
            //from the grid, so suppressing their endpoints would 404 the grid. Asserted so a later
            //"mark every plugin action [NopChildActionOnly]" tidy-up fails loudly.
            RequireAdmin();
            RequireInstalled(GoogleShoppingAsm);

            var response = _client.PostAsync("/FeedGoogleShopping/GoogleProductList",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "page", "1" },
                    { "pageSize", "10" }
                })).Result;
            TestContext.WriteLine("status=" + (int)response.StatusCode);

            Assert.AreNotEqual(HttpStatusCode.NotFound, response.StatusCode,
                "POST /FeedGoogleShopping/GoogleProductList is a 404, so the Kendo grid on the " +
                "configuration page cannot load. These two actions must NOT be marked " +
                "[NopChildActionOnly] - 3.90 did not mark them either.");
        }

        /// <summary>
        /// Skips unless the named plugin reports itself installed.
        /// </summary>
        private void RequireInstalled(string assemblyName)
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore(SkipNoDatabase);

            var installed = (_report ?? string.Empty).Split('\n')
                .Select(l => l.Trim())
                .Any(l => l == "plugin:" + assemblyName + ".installed=True");
            if (!installed)
                Assert.Ignore("NOT EXERCISED: " + assemblyName + " is discovered but NOT INSTALLED. " +
                              "Install it from the admin plugin list (or write its system name into " +
                              "App_Data/InstalledPlugins.txt and restart) and re-run.");
        }

        /// <summary>
        /// Pulls the anti-forgery token out of a rendered page, or null.
        /// </summary>
        private static string ExtractAntiForgeryToken(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"(?<v>[^\"]+)\"");
            if (!match.Success)
                match = System.Text.RegularExpressions.Regex.Match(html,
                    "value=\"(?<v>[^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
            return match.Success ? match.Groups["v"].Value : null;
        }

        #endregion
    }
}
