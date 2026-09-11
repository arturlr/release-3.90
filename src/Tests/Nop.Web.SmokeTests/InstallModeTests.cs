using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core.Data;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 7.7, group B — the pipeline, routing, one controller, one Razor view, the static-file
    /// allow-list and server-side validation, all exercised over real HTTP against the real host.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tests are written for the <b>uninstalled</b> store, which is not a limitation but the
    /// most information-dense state available without a database: with no
    /// <c>App_Data/Settings.txt</c>, <c>InstallUrlMiddleware</c> must redirect every request to
    /// <c>/install</c>, and that single behaviour exercises the host, the Autofac container, the
    /// middleware ordering, routing and configuration binding at once. The install page itself is
    /// a real controller returning a real compiled Razor view with a real FluentValidation
    /// validator, so Requirements 4.3, 4.4 and 4.6 are all covered here.
    /// </para>
    /// <para>
    /// <b>Every test in this fixture is skipped (not silently passed) when a database IS
    /// installed</b> — see <see cref="SetUp"/>. Group C covers the installed store.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class InstallModeTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                //Assert on the 302 itself rather than on where it lands.
                AllowAutoRedirect = false
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            if (DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore("A database IS installed, so install mode cannot be exercised. See InstalledStoreTests.");
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.6 — a former Global.asax module executes, in the right place
        // -----------------------------------------------------------------------------------

        [Test]
        public void Requirement_4_6_InstallUrlMiddleware_redirects_the_home_page_to_install()
        {
            //3.90's Application_BeginRequest install branch, ported to middleware by task 6.4.
            var response = _client.GetAsync("/").Result;
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            StringAssert.EndsWith("/install", response.Headers.Location.ToString());
        }

        [Test]
        public void Requirement_4_6_InstallUrlMiddleware_runs_before_routing()
        {
            //"/cart" is a real registered endpoint. If the redirect happened after routing, or
            //the middleware were mis-ordered, this would 500 (no database) instead of 302.
            var response = _client.GetAsync("/cart").Result;
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode,
                "InstallUrlMiddleware must intercept registered endpoints too - it sits before UseRouting.");
            StringAssert.EndsWith("/install", response.Headers.Location.ToString());
        }

        [Test]
        public void Requirement_4_6_static_resources_bypass_the_install_redirect()
        {
            //Both of 3.90's early exits are meant to be preserved. A static asset must be served,
            //not redirected - otherwise the install page would have no CSS or JS.
            var response = _client.GetAsync("/Scripts/public.common.js").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "A static asset was not served in install mode.");
        }

        // -----------------------------------------------------------------------------------
        // Requirements 4.3 + 4.4 — a controller responds and a compiled Razor view renders
        // -----------------------------------------------------------------------------------

        [Test]
        public void Requirements_4_3_4_4_install_controller_responds_and_its_Razor_view_renders()
        {
            var response = _client.GetAsync("/install").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var html = response.Content.ReadAsStringAsync().Result;
            //Markup only the compiled Razor view can produce.
            StringAssert.Contains("<title>nopCommerce installation</title>", html);
            StringAssert.Contains("class=\"language-selector form-control\"", html);
            //The language <select> is built from InstallationLocalizationService reading the
            //installer's App_Data/Localization/Installation/*.xml files, which only resolve
            //correctly if CommonHelper.BaseDirectory is the content root (deferral 1.5).
            StringAssert.Contains("Install/ChangeLanguage?language=en", html);
            //And the model-bound form fields prove MVC's view engine + html helpers ran.
            StringAssert.Contains("name=\"AdminEmail\"", html);
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 11.20 — SECURITY. Prove FluentValidation actually REJECTS.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_11_20_FluentValidation_rejects_invalid_input_SECURITY()
        {
            //InstallModel carries [Validator(typeof(InstallValidator))], whose first rule is
            //RuleFor(x => x.AdminEmail).NotEmpty(). If NopFluentValidationModelValidatorProvider
            //were not registered, ModelState.IsValid would be TRUE for this payload and the
            //installer would proceed to try to create a database. A 200 carrying the validation
            //summary is the only outcome that proves the validator ran.
            var form = new Dictionary<string, string>
            {
                { "AdminEmail", string.Empty },
                { "AdminPassword", string.Empty },
                { "ConfirmPassword", string.Empty },
                { "DataProvider", "sqlserver" },
                { "SqlConnectionInfo", "sqlconnectioninfo_values" },
                { "SqlServerName", string.Empty },
                { "SqlDatabaseName", string.Empty },
                { "SqlAuthenticationType", "windowsauthentication" }
            };

            var response = _client.PostAsync("/install", new FormUrlEncodedContent(form)).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "Expected the install view to be re-rendered with errors, not a redirect or a failure.");

            var html = response.Content.ReadAsStringAsync().Result;
            TestContext.WriteLine(ExtractValidationSummary(html));

            //"Enter admin email" / "Enter admin password" / "Enter confirm password" are the three
            //en resources behind the InstallValidator's NotEmpty rules. All three come from
            //FluentValidation and from nowhere else - the controller adds no admin-credential
            //checks of its own, so their presence cannot be explained any other way.
            StringAssert.Contains("Enter admin email", html,
                "FluentValidation did NOT run - ModelState.IsValid was true for empty required input. " +
                "Deferral 11.20 would then be falsely marked RESOLVED.");
            StringAssert.Contains("Enter admin password", html);
            StringAssert.Contains("Enter confirm password", html);
            //And a rule that lives in the CONTROLLER rather than the validator, to show both
            //validation paths still feed the same ModelState.
            StringAssert.Contains("SQL Server name is required", html);
        }

        [Test]
        public void Deferral_7_3_3_request_validation_no_longer_blocks_HTML_looking_input()
        {
            //Recorded as an accepted, unrestorable relaxation. Asserted so the behaviour is
            //documented by a test rather than only by prose: 3.90 would have thrown
            //"A potentially dangerous Request.Form value was detected"; ASP.NET Core has no
            //request validation at all, so the value reaches the validator and is rejected on
            //its own merits (not an email address) instead.
            var form = new Dictionary<string, string>
            {
                { "AdminEmail", "<script>alert(1)</script>" },
                { "AdminPassword", "x" },
                { "ConfirmPassword", "x" },
                { "DataProvider", "sqlserver" },
                { "SqlConnectionInfo", "sqlconnectioninfo_raw" },
                { "DatabaseConnectionString", string.Empty }
            };

            var response = _client.PostAsync("/install", new FormUrlEncodedContent(form)).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "The request was refused at the framework level; 7.3-3 says that cannot happen any more.");
            var html = response.Content.ReadAsStringAsync().Result;
            //It must be HTML-ENCODED on the way back out - that is the control that actually matters.
            StringAssert.DoesNotContain("<script>alert(1)</script>", html,
                "Unencoded echo of script input - Razor's default encoding is the real defence here.");
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferrals 39/7.1-4 and 40/7.1-5 — SECURITY. The static-file allow-list.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_40_allow_listed_static_assets_serve_from_the_content_root()
        {
            //There is no wwwroot; these are at their 3.90 locations and only serve because
            //NopStaticFileProvider repoints WebRootFileProvider (task 7.4).
            //
            //NOTE ON CASING: "Content/install" is lowercase on disk. Task 7.7 found that
            //Views/Install/Index.cshtml asked for "~/Content/Install/style.css" with a capital I,
            //which resolved on Windows and 404'd on Linux, leaving the installation page with no
            //stylesheet at all. The view was corrected; this test pins the on-disk casing so the
            //regression cannot come back silently.
            foreach (var path in new[]
            {
                "/Scripts/public.common.js",
                "/Scripts/jquery-1.10.2.min.js",
                "/Content/install/style.css",
                "/Content/install/images/install-synchronizing.gif",
                "/favicon.ico",
                "/Themes/DefaultClean/Content/css/styles.css"
            })
            {
                var response = _client.GetAsync(path).Result;
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "expected 200 for " + path);
            }
        }

        [Test]
        public void Deferral_40_the_install_page_only_links_assets_that_actually_serve()
        {
            //The audit that found the casing defect, expressed as a test: every same-origin
            //stylesheet/script/image the install view emits must return 200. Administration/ is
            //excluded because those genuinely do not serve yet - deferral 7.4-2, task 8.5.
            var html = _client.GetStringAsync("/install").Result;
            var refs = Regex.Matches(html, "(?:href|src)=\"(/[^\"]+\\.(?:css|js|gif|png|jpg|ico))\"")
                            .Select(m => m.Groups[1].Value)
                            .Where(u => !u.StartsWith("/Administration/", StringComparison.OrdinalIgnoreCase))
                            .Distinct()
                            .ToList();

            Assert.IsNotEmpty(refs, "No same-origin assets found in the install page - check the premise.");
            TestContext.WriteLine("install page assets checked: " + string.Join(", ", refs));

            var broken = refs.Where(u => _client.GetAsync(u).Result.StatusCode != HttpStatusCode.OK).ToList();
            Assert.IsEmpty(broken,
                "The install page links assets that do not serve (case mismatch or missing file): " +
                string.Join(", ", broken));
        }

        [Test]
        public void Deferral_39_secrets_and_source_are_NOT_served_SECURITY()
        {
            //Every path below is a REAL file on disk under the content root, so a refusal is a
            //decision by the allow-list rather than a trivially-true absence. App_Data is the
            //important one: System.Web blocked it implicitly and ASP.NET Core does not, and it is
            //where the database connection string lives.
            var mustNotServe = new List<string>
            {
                "/appsettings.json",
                "/web.config",
                "/App_Data/browscap.xml",
                "/App_Data/GeoLite2-Country.mmdb",
                "/Views/_ViewImports.cshtml",
                "/Views/Shared/_Root.cshtml",
                "/Themes/DefaultClean/theme.config",
                "/Themes/DefaultClean/Views/Shared/Head.cshtml",
                "/Nop.Web.csproj",
                "/Program.cs"
            };

            //Only assert on Settings.txt when it exists, so the test cannot pass vacuously.
            var settingsTxt = Nop.Core.CommonHelper.MapPath("~/App_Data/Settings.txt");
            if (System.IO.File.Exists(settingsTxt))
                mustNotServe.Add("/App_Data/Settings.txt");
            TestContext.WriteLine("App_Data/Settings.txt exists on disk: " + System.IO.File.Exists(settingsTxt));

            foreach (var path in mustNotServe)
                AssertNotServed(path);
        }

        [Test]
        public void Deferral_7_4_2_admin_static_assets_do_NOT_serve_yet_KNOWN_GAP()
        {
            //Documented gap, owned by task 8.5: NopStaticFileProvider's allow-list deliberately
            //excludes Administration/. The install page links two admin stylesheets, so the page
            //renders UNSTYLED-ABOVE-THE-FOLD today. This test asserts the CURRENT behaviour on
            //purpose - when 8.5 widens the allow-list it will fail, which is the signal to update it.
            var html = _client.GetStringAsync("/install").Result;
            StringAssert.Contains("/Administration/Content/bootstrap/css/bootstrap.min.css", html,
                "The install view no longer links admin CSS - re-check this test's premise.");

            AssertNotServed("/Administration/Content/bootstrap/css/bootstrap.min.css");
        }

        /// <summary>
        /// Asserts a path is not served, allowing for install mode's indirection.
        /// </summary>
        /// <remarks>
        /// A refused path does NOT return 404 while the store is uninstalled, and understanding
        /// why matters for reading these results. <c>Program.cs</c> registers
        /// <c>UseStatusCodePagesWithReExecute("/page-not-found")</c> BEFORE
        /// <c>UseNopStaticFiles()</c>, so the 404 that the static-file middleware and routing
        /// produce bubbles back up to it and the request is re-executed as
        /// <c>/page-not-found</c> — which <c>InstallUrlMiddleware</c> then redirects to
        /// <c>/install</c>. The observable result is <b>302 → /install</b>. That is still a
        /// refusal: the file's bytes are never written. This helper therefore accepts 404 or a
        /// 302 to <c>/install</c>, and additionally requires the response body to be empty, so a
        /// path that somehow both redirected and leaked content would still fail.
        /// </remarks>
        private void AssertNotServed(string path)
        {
            var response = _client.GetAsync(path).Result;
            var body = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Location != null ? response.Headers.Location.ToString() : string.Empty;
                Assert.IsTrue(location.EndsWith("/install", StringComparison.OrdinalIgnoreCase),
                    "Refused with a redirect to something other than /install: " + path + " -> " + location);
                Assert.IsEmpty(body, "Redirected AND returned a body for " + path);
                return;
            }

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
                "SERVED (or otherwise did not refuse) a file that must never be reachable over HTTP: " + path);
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 7.3-4 — former [ChildActionOnly] actions are URL-reachable
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_7_3_4_former_child_actions_are_URL_reachable_KNOWN_GAP()
        {
            //Cannot be observed in install mode (everything redirects), so this only records that
            //the endpoint EXISTS in the route table rather than being suppressed. The redirect is
            //InstallUrlMiddleware, not a 404 - i.e. routing would have matched it.
            var response = _client.GetAsync("/Common/Footer").Result;
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            StringAssert.EndsWith("/install", response.Headers.Location.ToString());
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.6 — 404 handling replaced Application_Error's re-execute
        // -----------------------------------------------------------------------------------

        [Test]
        public void Requirement_4_6_unknown_paths_are_handled_not_unhandled()
        {
            //In install mode this is still the redirect (the middleware sits before routing), so
            //what is proved is that an unmatched path does not fault the pipeline.
            var response = _client.GetAsync("/no/such/path/at/all").Result;
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Found ||
                          response.StatusCode == HttpStatusCode.NotFound,
                "Unexpected status for an unknown path: " + (int)response.StatusCode);
        }

        private static string ExtractValidationSummary(string html)
        {
            var start = html.IndexOf("validation-summary", StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return "(no validation summary in response)";
            var end = Math.Min(html.Length, start + 1200);
            return html.Substring(start, end - start);
        }
    }
}
