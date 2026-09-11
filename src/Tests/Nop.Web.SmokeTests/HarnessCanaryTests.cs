using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Deliberately-failing tests whose only purpose is to prove this harness can fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A green smoke run is worthless if nothing was ever shown to make it red. This migration has
    /// already been bitten twice by exactly that: the Six Labors licence task that failed on every
    /// compile while MSBuild reported only warnings (<c>ContinueOnError=true</c>), and Roslyn's
    /// refusal to bind method bodies while declaration errors existed, which hid a real
    /// <c>CS1929</c> in <c>FilePermissionHelper</c> across three completed tasks. So each of the
    /// three mechanisms this suite depends on has a canary that travels the same code path and
    /// asserts something known to be false:
    /// </para>
    /// <list type="number">
    /// <item>a real HTTP request through TestServer</item>
    /// <item>a resolve through the real Autofac container</item>
    /// <item>the <c>/__smoke/*</c> probe middleware's text output</item>
    /// <item>the <c>/__smoke/action</c> probe's endpoint / descriptor report, which is what the
    /// deferral 7.3-4 assertions read</item>
    /// <item>the configured Razor view-location expander's emitted location formats, which is what
    /// the task 8.2 / deferral 8.1-4 assertions read</item>
    /// <item>the admin static-asset assertions (task 8.5)</item>
    /// <item>the <c>/__smoke/adminarea</c> probe's report, which is what task 8.8's admin area /
    /// application-part / compiled-view / controller-area assertions read</item>
    /// <item>an ADMIN-AUTHENTICATED page fetch, which is what every assertion in
    /// <c>AdminUiRenderTests</c> rests on</item>
    /// </list>
    /// <para>
    /// The fixture is <see cref="ExplicitAttribute"/>, so an ordinary <c>dotnet test</c> reports it
    /// as skipped and CI stays green. Run it on demand to re-confirm the harness:
    /// </para>
    /// <code>dotnet test src/Tests/Nop.Web.SmokeTests --filter "FullyQualifiedName~HarnessCanaryTests"</code>
    /// <para>
    /// <b>All of them MUST report Failed.</b> If any of them passes, or is silently skipped when
    /// selected explicitly, the corresponding group of real assertions cannot be trusted.
    /// Task 7.7 ran this and observed 3 failed / 0 passed; the deferral 7.3-4 / 7.7-1 fix added a
    /// fourth and observed 4 failed / 0 passed; task 8.2 added a fifth and observed
    /// 5 failed / 0 passed; task 8.5 added a sixth; task 8.8 added a seventh and an eighth and
    /// observed 8 failed / 0 passed.
    /// </para>
    /// </remarks>
    [TestFixture]
    [Explicit("Deliberately failing. Run explicitly to prove the harness reports failures.")]
    [Category("Canary")]
    public class HarnessCanaryTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        [Test]
        public void CANARY_http_assertions_can_fail()
        {
            //The install probe endpoint answers 200. Asserting 418 must be reported as a failure.
            var response = _client.GetAsync(SmokeProbeMiddleware.Prefix + "ok").Result;
            Assert.AreEqual((HttpStatusCode)418, response.StatusCode,
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_container_assertions_can_fail()
        {
            //IWebHelper resolves. Asserting it does not must be reported as a failure.
            var webHelper = EngineContext.Current.Resolve<IWebHelper>();
            Assert.IsNull(webHelper, "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_probe_middleware_assertions_can_fail()
        {
            //The scope probe never emits this token. Asserting it does must be reported as a failure.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "scope").Result;
            StringAssert.Contains("thisTokenIsNeverEmitted=True", body,
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_action_probe_assertions_can_fail()
        {
            //Guards the deferral 7.3-4 assertions specifically. They are shaped as substring
            //matches on the /__smoke/action probe output, and a substring match that never appears
            //is the failure mode that would make them vacuous. There is no
            //Common.NoSuchActionExists, so the probe reports visibleToChildActionBridge=False and
            //endpointCount=0; asserting the opposite must be reported as a failure.
            var body = _client.GetStringAsync(
                SmokeProbeMiddleware.Prefix + "action?controller=Common&action=NoSuchActionExists").Result;
            StringAssert.Contains("visibleToChildActionBridge=True", body,
                "CANARY: this assertion is meant to fail.");
        }
        [Test]
        public void CANARY_view_location_expander_assertions_can_fail()
        {
            //Guards the task 8.2 / deferral 8.1-4 assertions. They read the CONFIGURED expander out
            //of RazorViewEngineOptions and inspect the location formats it emits - a mechanism no
            //other canary covers. The expander never emits this format, so asserting that it does
            //must be reported as a failure.
            var razor = _factory.Services
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<
                    Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>>().Value;
            var expander = razor.ViewLocationExpanders.First();

            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
                new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = _factory.Services },
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            var context = new Microsoft.AspNetCore.Mvc.Razor.ViewLocationExpanderContext(
                actionContext, "SomeView", "SomeController", "Admin", null, false);
            context.Values = new System.Collections.Generic.Dictionary<string, string>();
            expander.PopulateValues(context);

            var locations = expander
                .ExpandViewLocations(context, new[] { "/FRAMEWORK/DEFAULT/{0}.cshtml" })
                .ToList();

            CollectionAssert.Contains(locations, "/ThisLocationFormatIsNeverEmitted/{0}.cshtml",
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_admin_static_asset_assertions_can_fail()
        {
            //Guards the task 8.5 assertions in AdminStaticAssetTests. Those come in two shapes and
            //this canary covers both against the SAME path, so it cannot pass by half:
            //
            //  (a) "an admin asset serves"  - a 200 assertion. The failure mode that would make it
            //      vacuous is a client that reports 200 for anything, so this asserts 200 for an
            //      admin path that genuinely does not exist.
            //  (b) "a denied path is not versioned" - asserted via IFileVersionProvider, which
            //      returns the path UNCHANGED when it cannot see the file. So a ?v= assertion on a
            //      non-existent admin asset must fail; if it passed, the versioning assertions in
            //      Deferral_33_admin_assets_are_cache_busted_for_free would be measuring an
            //      unconditional suffix rather than the allow-list.
            const string doesNotExist = "/Administration/Content/NoSuchAdminAsset_8_5_canary.css";

            var versionProvider = _factory.Services
                .GetRequiredService<Microsoft.AspNetCore.Mvc.ViewFeatures.IFileVersionProvider>();
            var versioned = versionProvider.AddFileVersionToPath("/", doesNotExist);

            var status = _client.GetAsync(doesNotExist).Result.StatusCode;

            Assert.IsTrue(
                status == System.Net.HttpStatusCode.OK && versioned.Contains("?v="),
                "CANARY: this assertion is meant to fail. status=" + status + " versioned=" + versioned);
        }
        [Test]
        public void CANARY_admin_area_probe_assertions_can_fail()
        {
            //Guards the task 8.8 admin-area assertions, which all read the /__smoke/adminarea
            //probe's text report - a mechanism no other canary covers. Its lines are shaped
            //`key=value` and asserted with substring matches, so the failure mode that would make
            //them vacuous is a substring that never appears. adminControllerCount is 54; asserting
            //an impossible value must be reported as a failure.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "adminarea").Result;
            StringAssert.Contains("adminControllerCount=999999", body,
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_plugin_probe_assertions_can_fail()
        {
            //Guards the /__smoke/plugins report, which is what every Group A assertion in
            //PluginViewRenderTests (tasks 10.1-10.3) reads - plugin discovery, application parts,
            //compiled Razor identifiers, _ViewStart isolation, endpoints and URL generation. Those
            //assertions are exact-line matches on `key=value` lines, so the failure mode that
            //would make them vacuous is a line that never appears. There is no such plugin, so
            //asserting it was discovered must be reported as a failure.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "plugins").Result;
            StringAssert.Contains("plugin:Nop.Plugin.ThereIsNoSuchPlugin.discovered=True", body,
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_plugin_view_engine_assertions_can_fail()
        {
            //Guards the `getView:` half specifically - the assertions that the real
            //IRazorViewEngine resolves each plugin view path and the two cross-assembly
            //~/Areas/Admin/Views/Shared/ paths (deferral 8.2-3). It travels the same code path: the
            //probe accepts an extra path to look up, and this one cannot exist in any assembly, so
            //asserting that it resolves must be reported as a failure. Without this canary a change
            //that made GetView report Success unconditionally would leave those assertions green.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix +
                "plugins?getView=~/Plugins/NoSuchPlugin/Views/NoSuchView.cshtml").Result;
            StringAssert.Contains("getView:~/Plugins/NoSuchPlugin/Views/NoSuchView.cshtml=True", body,
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_authenticated_admin_page_assertions_can_fail()
        {
            //Guards AdminUiRenderTests, whose assertions are HTML substring matches against pages
            //fetched by an ADMIN-AUTHENTICATED client. Two things could make them vacuous: the
            //sign-in silently not happening (so every page is a 302 and a DoesNotContain assertion
            //passes trivially), or a substring that never appears. This exercises both against the
            //same request: it signs in exactly as the fixture does, then asserts a marker no admin
            //page emits.
            //
            //If NO database is installed this cannot sign in - in which case it STILL fails, on the
            //200 assertion, which is the correct outcome for a canary.
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
            client.PostAsync("/login", new FormUrlEncodedContent(
                new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Email", AdminUiRenderTests.AdminEmail },
                    { "Password", AdminUiRenderTests.AdminPassword }
                })).Wait();

            var response = client.GetAsync("/Admin/").Result;
            var html = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            client.Dispose();

            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.OK &&
                html.Contains("ThisMarkerIsNeverEmittedByAnyAdminPage"),
                "CANARY: this assertion is meant to fail. status=" + (int)response.StatusCode +
                " len=" + html.Length);
        }
    }
}
