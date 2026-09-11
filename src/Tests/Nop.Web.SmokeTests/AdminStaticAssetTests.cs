using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NUnit.Framework;
using Nop.Core;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 8.5 — the admin static-asset surface, over real HTTP against the real host.
    /// Closes the admin half of runtime deferral 7.4-2 and its serving half of deferral 8.1-1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every test in this fixture runs unconditionally — no database, and no
    /// <c>Nop.Admin.dll</c> required.</b> That is a deliberate design choice, not an accident:
    /// <c>NopStaticFileProvider</c> resolves a request subpath against the <b>Nop.Web content
    /// root</b> on the filesystem, and the admin assets are physically at
    /// <c>Administration/Content/</c> and <c>Administration/Scripts/</c> underneath it. So
    /// admin asset serving is observable without the admin area being routable at all, which
    /// sidesteps deferral <b>8.4-1</b> (<c>Nop.Admin.dll</c> is not in the test project's base
    /// directory, so <c>WebAppTypeFinder</c> cannot load it and the Admin area is absent from
    /// the test host until task 8.8 fixes that). Putting these assertions in the always-run set
    /// means they protect the fix in <b>both</b> the installed and uninstalled states, where
    /// <see cref="InstallModeTests"/> and <see cref="InstalledStoreTests"/> each cover only one.
    /// </para>
    /// <para>
    /// <b>Refusals are asserted against files that REALLY EXIST.</b> A <c>.bak</c> and a
    /// <c>tmp/*.zip</c> are planted on disk in <see cref="OneTimeSetUp"/> and removed in
    /// <see cref="OneTimeTearDown"/>, so a 404 is a decision by the allow-list rather than a
    /// trivially-true absence. Task 8.1 recorded the same reasoning for the publish exclusions:
    /// testing a refusal against a file that is not there cannot distinguish "the rule works"
    /// from "there was nothing to refuse".
    /// </para>
    /// </remarks>
    [TestFixture]
    public class AdminStaticAssetTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        private string _plantedBak;
        private string _plantedZip;

        /// <summary>
        /// Admin assets that must serve. One per asset family, chosen so a partial regression
        /// cannot hide: stylesheet, RTL stylesheet, third-party CSS, image, web font, Kendo
        /// (the <c>{0}</c>-substituted family task 8.4 resolved concretely), TinyMCE language
        /// pack, Roxy Fileman page + its client-fetched JSON, and admin JavaScript.
        /// </summary>
        /// <remarks>
        /// <c>Administration/Scripts/admin.navigation.js</c> and
        /// <c>Administration/Content/images/throbber-synchronizing.gif</c> are the two files
        /// deferral <b>8.1-2</b> was about: <c>_AdminLayout.cshtml</c> asked for them as
        /// <c>~/Administration/scripts/…</c> and <c>~/administration/content/images/…</c>, which
        /// resolved on Windows and 404'd on Linux. Task 8.4 fixed the view; these entries pin
        /// the on-disk casing so a widened allow-list cannot be credited for serving a path the
        /// views spell differently.
        /// </remarks>
        private static readonly string[] MustServe =
        {
            "/Administration/Content/styles.css",
            "/Administration/Content/styles.rtl.css",
            "/Administration/Content/bootstrap/css/bootstrap.min.css",
            "/Administration/Content/adminLTE/AdminLTE-2.3.0.min.css",
            "/Administration/Content/images/throbber-synchronizing.gif",
            "/Administration/Content/fontAwesome/fonts/fontawesome-webfont.woff2",
            "/Administration/Content/kendo/2014.1.318/kendo.common.min.css",
            "/Administration/Content/tinymce/langs/de_DE.js",
            "/Administration/Content/Roxy_Fileman/index.html",
            "/Administration/Content/Roxy_Fileman/conf.json",
            "/Administration/Content/Roxy_Fileman/lang/en.json",
            "/Administration/Scripts/admin.common.js",
            "/Administration/Scripts/admin.navigation.js",
            "/Administration/Scripts/kendo/2014.1.318/kendo.web.min.js"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                //A refusal is a 404 on an installed store and a 302 to /install while
                //uninstalled (Program.cs registers UseNopStatusCodePages BEFORE
                //UseNopStaticFiles, so the static-file 404 re-executes /page-not-found, which
                //InstallUrlMiddleware then redirects). Following the redirect would turn that
                //into a 200 of the install page and make every refusal assertion ambiguous.
                AllowAutoRedirect = false
            });

            //Plant the two files whose refusal must not be vacuous.
            _plantedBak = CommonHelper.MapPath("~/Administration/db_backups/8_5_smoke_planted.bak");
            _plantedZip = CommonHelper.MapPath("~/Administration/Content/Roxy_Fileman/tmp/8_5_smoke_planted.zip");
            Directory.CreateDirectory(Path.GetDirectoryName(_plantedBak));
            Directory.CreateDirectory(Path.GetDirectoryName(_plantedZip));
            File.WriteAllText(_plantedBak, "PLANTED BY Nop.Web.SmokeTests - stands in for a database backup.");
            File.WriteAllText(_plantedZip, "PLANTED BY Nop.Web.SmokeTests - stands in for a Roxy Fileman temp archive.");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();

            foreach (var planted in new[] { _plantedBak, _plantedZip })
            {
                try
                {
                    if (planted != null && File.Exists(planted))
                        File.Delete(planted);
                }
                catch (IOException)
                {
                    //best effort - a leftover probe file is visible in `git status`, not silent
                }
            }
        }

        // -----------------------------------------------------------------------------------
        // Deferral 7.4-2, admin half — the assets serve
        // -----------------------------------------------------------------------------------

        [Test, TestCaseSource(nameof(MustServe))]
        public void Deferral_7_4_2_an_admin_asset_serves(string path)
        {
            //Before task 8.5 every one of these returned 404 (302 in install mode), because
            //NopStaticFileProvider's allow-list covered only Content/, Scripts/ and
            //Themes/{theme}/Content/ - Administration/ was deliberately excluded by task 7.4.
            var response = _client.GetAsync(path).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "admin asset does not serve: " + path);

            var body = response.Content.ReadAsByteArrayAsync().Result;
            Assert.Greater(body.Length, 0, "served an EMPTY body for " + path);
        }

        [Test]
        public void Deferral_7_4_2_admin_assets_get_the_same_cache_headers_as_storefront_assets()
        {
            //Proves the admin assets go through the SAME StaticFileOptions - i.e. task 7.4's
            //translation of 3.90's <clientCache cacheControlMaxAge="7.00:00:00"/> applies to
            //them too, rather than there being a second, unconfigured static-file pipeline.
            var response = _client.GetAsync("/Administration/Content/styles.css").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var cacheControl = response.Headers.CacheControl != null
                ? response.Headers.CacheControl.ToString() : string.Empty;
            TestContext.WriteLine("Cache-Control: " + cacheControl);
            StringAssert.Contains("max-age=604800", cacheControl,
                "admin assets are not getting 3.90's seven-day <clientCache> max-age");
            StringAssert.Contains("public", cacheControl);
        }

        [Test]
        public void Deferral_33_admin_assets_are_cache_busted_for_free()
        {
            //Deferral 7.4-2 predicted this comes free: IFileVersionProvider resolves against
            //IWebHostEnvironment.WebRootFileProvider, which task 7.4 assigned the very same
            //NopStaticFileProvider instance the middleware serves - so anything the provider
            //serves is also versioned, and the two cannot drift apart. Asserted directly rather
            //than through a rendered page, because that needs the admin area loaded (8.4-1).
            var versionProvider = _factory.Services.GetRequiredService<IFileVersionProvider>();

            const string assetPath = "/Administration/Content/styles.css";
            var versioned = versionProvider.AddFileVersionToPath("/", assetPath);
            TestContext.WriteLine(assetPath + "  ->  " + versioned);

            StringAssert.Contains("?v=", versioned,
                "IFileVersionProvider left an admin asset unversioned - it cannot see the file, " +
                "so PageHeadBuilder's cache busting is inert for the whole admin UI.");

            //and the versioned URL must still serve, or cache busting has broken serving
            Assert.AreEqual(HttpStatusCode.OK, _client.GetAsync(versioned).Result.StatusCode,
                "the versioned admin asset URL does not serve: " + versioned);

            //a path OUTSIDE the allow-list must come back unversioned, which is what proves the
            //check above is measuring the provider rather than an unconditional suffix
            var denied = versionProvider.AddFileVersionToPath("/", "/Administration/sitemap.config");
            Assert.AreEqual("/Administration/sitemap.config", denied,
                "a denied path was versioned - the provider is not the allow-list after all");
        }

        // -----------------------------------------------------------------------------------
        // The surface that must STAY closed — SECURITY
        // -----------------------------------------------------------------------------------

        [Test]
        public void Task_8_5_widening_the_allow_list_did_not_expose_the_rest_of_Administration_SECURITY()
        {
            //Each path is a REAL file on disk (asserted below), so a refusal is a decision.
            //
            //db_backups/*.bak is the important one and it has history: 3.90 mapped .bak in
            //<staticContent><mimeMap> specifically so backups could be downloaded over HTTP,
            //which bypassed [AdminAuthorize] entirely. Task 7.4 refused to reproduce the
            //mapping and task 8.3 replaced the static link in CommonController with a
            //permission-checked streaming action. Widening the allow-list for Content/ and
            //Scripts/ must not have undone either.
            var mustNotServe = new List<string>
            {
                //the planted database backup, and the directory that holds it
                "/Administration/db_backups/8_5_smoke_planted.bak",
                "/Administration/db_backups/placeholder.txt",

                //the planted Roxy Fileman temp archive - INSIDE Administration/Content/, so this
                //is DeniedSubpaths doing the work, not the Content/Scripts rule failing to match
                "/Administration/Content/Roxy_Fileman/tmp/8_5_smoke_planted.zip",
                "/Administration/Content/Roxy_Fileman/tmp/placeholder.txt",

                //configuration read from disk at runtime, never over HTTP
                "/Administration/sitemap.config",

                //TASK 8.7 removed "/Administration/Web.config" from this list, because 8.7 DELETED
                //that file - and 8.5's own rule is that a refusal asserted against a path that is
                //not on disk proves nothing, which is why the premise loop below fails loudly
                //rather than quietly passing. The ".config must not serve" case it contributed is
                //still carried by sitemap.config immediately above (same DeniedExtensions entry,
                //same outside-the-allow-list position). The stronger statement that replaced it -
                //that no admin web.config exists at ANY casing any more - is asserted by
                //Task_8_7_the_admin_project_has_no_web_config_at_any_casing below.

                //source and project files
                "/Administration/Nop.Admin.csproj",
                "/Administration/Controllers/CommonController.cs",
                "/Administration/Areas/Admin/Views/_ViewImports.cshtml",
                "/Administration/Areas/Admin/Views/Shared/_AdminLayout.cshtml",
                "/Administration/Areas/Admin/Views/Product/List.cshtml",

                //the admin assembly and the host's own secrets, re-asserted here because a
                //widened allow-list is exactly the change that could have reached them
                "/Administration/bin/Debug/net10.0/Nop.Admin.dll",
                "/appsettings.json",
                "/web.config"
            };

            //Only assert on Settings.txt when it exists, so this cannot pass vacuously.
            var settingsTxt = CommonHelper.MapPath("~/App_Data/Settings.txt");
            if (File.Exists(settingsTxt))
                mustNotServe.Add("/App_Data/Settings.txt");
            TestContext.WriteLine("App_Data/Settings.txt exists on disk: " + File.Exists(settingsTxt));

            //Fail loudly if a path stopped existing, rather than quietly asserting nothing.
            foreach (var path in mustNotServe)
            {
                if (path.StartsWith("/Administration/bin/", StringComparison.OrdinalIgnoreCase))
                    continue;   //build output - present only after Nop.Admin has been built
                var onDisk = CommonHelper.MapPath("~" + path);
                Assert.IsTrue(File.Exists(onDisk),
                    "PREMISE BROKEN: " + path + " is not on disk, so refusing it proves nothing. " +
                    "Expected at " + onDisk);
            }

            foreach (var path in mustNotServe)
                AssertNotServed(path);
        }

        [Test]
        public void Task_8_7_the_admin_project_has_no_web_config_at_any_casing()
        {
            //TASK 8.7 deleted Administration/Web.config (task 8.4 had already deleted the view one
            //at Areas/Admin/Views/Web.config). This asserts they STAY deleted, which is a stronger
            //statement than the refusal it replaced in the SECURITY test above - "must not be
            //served" became "must not exist" - and it guards two separate live hazards that a
            //re-added file would reintroduce:
            //
            //  1. IIS parses EVERY file named web.config in the served tree, case-insensitively on
            //     Windows. The legacy file's <runtime><assemblyBinding> block and the view file's
            //     System.Web.WebPages.Razor <configSections> group name assemblies that do not
            //     exist on .NET 10, which is an HTTP 500.19 configuration error.
            //  2. It changes what the SDK's TransformWebConfig task GENERATES on publish. Task 8.7
            //     measured that the presence of a web.config item flips the generated ANCM shim's
            //     filename between "Web.config" and "web.config", and either one collides with
            //     Nop.Web's own web.config in the single-directory deployment deferral 8.5-1
            //     describes. IsTransformWebConfigDisabled in Nop.Admin.csproj is the guard for the
            //     generation half; this test is the guard for the file half.
            //
            //NOT VACUOUS BY CONSTRUCTION: the same search is required to FIND sitemap.config. A
            //broken root path or glob would report "no web.config" and would also report "no
            //sitemap.config", so the second assertion fails and the first cannot pass for free.
            var adminRoot = CommonHelper.MapPath("~/Administration");
            Assert.IsTrue(Directory.Exists(adminRoot), "PREMISE BROKEN: " + adminRoot + " not found");

            var configs = Directory
                .EnumerateFiles(adminRoot, "*.config", SearchOption.AllDirectories)
                .Where(p => !p.Replace('\\', '/').Contains("/obj/") &&
                            !p.Replace('\\', '/').Contains("/bin/"))
                .ToList();

            var webConfigs = configs
                .Where(p => string.Equals(Path.GetFileName(p), "web.config",
                                          StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.IsEmpty(webConfigs,
                "A web.config reappeared under Administration/. Task 8.7 deleted the last one; see " +
                "the IsTransformWebConfigDisabled comment in Nop.Admin.csproj before re-adding any " +
                "file with this name. Found: " + string.Join(", ", webConfigs));

            //the control: proves the search really looks at the admin tree
            Assert.IsTrue(
                configs.Any(p => string.Equals(Path.GetFileName(p), "sitemap.config",
                                               StringComparison.OrdinalIgnoreCase)),
                "PREMISE BROKEN: sitemap.config was not found by the same search, so the " +
                "no-web.config result above proves nothing. Searched " + adminRoot +
                ", found " + configs.Count + " .config files.");
        }

        [Test]
        public void Task_8_5_the_Administration_directory_itself_cannot_be_enumerated_SECURITY()
        {
            //UseDirectoryBrowser is not registered, so this is belt-and-braces - but
            //IsAllowedDirectory is public-ish behaviour (protected virtual) and a plugin could
            //compose over the provider, so the refusal is asserted rather than assumed.
            var provider = (IFileProvider)_factory.Services
                .GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;

            foreach (var dir in new[]
            {
                "",
                "Administration",
                "Administration/db_backups",
                "Administration/Areas",
                "Administration/Areas/Admin/Views",
                "Administration/Content/Roxy_Fileman/tmp",
                "App_Data"
            })
            {
                var contents = provider.GetDirectoryContents(dir);
                Assert.IsFalse(contents.Exists,
                    "enumerable directory that must not be: '" + dir + "'");
            }

            //...while the two admin asset roots ARE enumerable, which is what shows the
            //assertions above are not passing because the provider refuses everything.
            foreach (var dir in new[] { "Administration/Content", "Administration/Scripts" })
            {
                Assert.IsTrue(provider.GetDirectoryContents(dir).Exists,
                    "admin asset root is not enumerable, so the allow-list is not matching: " + dir);
            }
        }

        // -----------------------------------------------------------------------------------
        // Deferral 7.7-4 — casing. The whole point of the audit, expressed as a test.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_7_7_4_admin_asset_urls_are_case_exact_against_the_filesystem()
        {
            //On a case-sensitive filesystem a mis-cased URL must 404. NopStaticFileProvider's own
            //allow-list comparisons are OrdinalIgnoreCase on purpose (matching 3.90's
            //case-insensitive IIS behaviour), so the refusal comes from PhysicalFileProvider
            //hitting the real filesystem - which is exactly the behaviour that turned eight
            //Windows-only-working paths into Linux 404s in task 7.7.
            //
            //Skipped rather than silently passed on a case-INSENSITIVE filesystem, where both
            //spellings legitimately serve and the assertion would be meaningless.
            var probe = CommonHelper.MapPath("~/Administration/Content/styles.css");
            var miscased = CommonHelper.MapPath("~/administration/content/styles.css");
            if (File.Exists(miscased))
            {
                Assert.Ignore("NOT EXERCISED: the filesystem is case-insensitive, so a mis-cased " +
                              "URL legitimately serves and this assertion cannot distinguish anything. " +
                              "Run on Linux/containers - which is what build-environment.md prescribes.");
            }
            Assert.IsTrue(File.Exists(probe), "PREMISE BROKEN: " + probe + " is not on disk.");

            foreach (var path in new[]
            {
                //the two spellings deferral 8.1-2 found in _AdminLayout.cshtml
                "/Administration/scripts/admin.navigation.js",
                "/administration/content/images/throbber-synchronizing.gif",
                //and the containing tree, so the whole prefix is pinned
                "/administration/Content/styles.css",
                "/Administration/content/styles.css"
            })
            {
                AssertNotServed(path);
            }
        }

        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Asserts a path is not served, in either store state.
        /// </summary>
        /// <remarks>
        /// Accepts 404, or a 302 to <c>/install</c> with an empty body — the install-mode
        /// indirection <see cref="InstallModeTests"/> documents at length. Either way the file's
        /// bytes are never written, and a response that both redirected and carried a body
        /// still fails.
        /// </remarks>
        private void AssertNotServed(string path)
        {
            var response = _client.GetAsync(path).Result;
            var body = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode == HttpStatusCode.Found)
            {
                var location = response.Headers.Location != null
                    ? response.Headers.Location.ToString() : string.Empty;
                Assert.IsTrue(location.EndsWith("/install", StringComparison.OrdinalIgnoreCase),
                    "Refused with a redirect to something other than /install: " + path + " -> " + location);
                Assert.IsEmpty(body, "Redirected AND returned a body for " + path);
                return;
            }

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
                "SERVED (or otherwise did not refuse) a path that must never be reachable over HTTP: "
                + path);
        }
    }
}
