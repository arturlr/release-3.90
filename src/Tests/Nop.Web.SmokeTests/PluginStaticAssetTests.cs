using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Nop.Core;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 11.1 — the PLUGIN static-asset surface, over real HTTP against the real host.
    /// Closes runtime deferral <b>10.x-1</b> for all three affected plugins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE DEFERRAL.</b> 3.90 served the whole application root through <c>System.Web</c>'s
    /// static handler, and the only thing it explicitly refused under <c>~/Plugins</c> was
    /// <c>*.dll</c> (a <c>DenyAccessToPluginDLLs</c> <c>HttpForbiddenHandler</c> in
    /// <c>Web.config</c>). Task 7.4 inverted that default: <c>NopStaticFileProvider</c> is an
    /// <b>allow-list</b>, and <c>Plugins/</c> was not in it. So every
    /// <c>~/Plugins/&lt;ShortName&gt;/Content/*.css</c> and <c>Scripts/*.js</c> returned 404 with
    /// nothing logged — the exact shape of failure a stylesheet reference produces, i.e. none at
    /// all. Task 11.1 added a third-level <c>Plugins/*/{Content,Scripts}/**</c> rule.
    /// </para>
    /// <para>
    /// <b>WHY IT MATTERS MOST FOR <c>ExternalAuth.Facebook</c>.</b>
    /// <c>Views/PublicInfo.cshtml</c> renders a single <c>&lt;a class="facebook-btn"&gt;</c> with
    /// <b>no text and no inline style</b>; <c>facebookstyles.css</c> supplies its width, height and
    /// background image. A 404 on the stylesheet therefore does not produce an ugly button, it
    /// produces a zero-size invisible one — the storefront login page silently loses the control
    /// altogether. <see cref="Task_11_1_the_Facebook_button_css_really_is_load_bearing"/> asserts
    /// that property of the CSS itself, so this fixture cannot be dismissed as cosmetic.
    /// </para>
    /// <para>
    /// <b>EVERY TEST RUNS UNCONDITIONALLY — no database.</b> Same reasoning as
    /// <see cref="AdminStaticAssetTests"/>: <c>NopStaticFileProvider</c> resolves a subpath against
    /// the Nop.Web content root on the filesystem, and the plugin assets are physically at
    /// <c>Plugins/&lt;ShortName&gt;/Content/</c> underneath it because each plugin's
    /// <c>OutputPath</c> deploys them there. So asset serving is observable without the plugin
    /// being installed, without its routes existing and without the store being installed.
    /// </para>
    /// <para>
    /// <b>REFUSALS ARE ASSERTED AGAINST FILES THAT REALLY EXIST</b> — the model
    /// <see cref="AdminStaticAssetTests"/> established. The plugin assembly, its
    /// <c>.deps.json</c> and its <c>Description.txt</c> are already on disk (a deployed plugin), so
    /// those refusals are decisions. For the two paths that would otherwise not exist —
    /// <c>Plugins/bin/*</c> and a file directly inside a plugin folder — real files are
    /// <b>planted</b> in <see cref="OneTimeSetUp"/> and removed in
    /// <see cref="OneTimeTearDown"/>. A 404 against an absent file cannot distinguish "the rule
    /// works" from "there was nothing to refuse".
    /// </para>
    /// <para>
    /// <b>Task 15.3 (<c>Widgets.NivoSlider</c>) needs nothing further here</b> — the rule covers
    /// <c>Scripts/</c> as well as <c>Content/</c>, and
    /// <see cref="Task_11_1_the_Scripts_half_of_the_rule_works_for_15_3"/> proves that half with a
    /// planted file rather than leaving it to be discovered at 15.3.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class PluginStaticAssetTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        private string _plantedShadowCopy;
        private string _plantedPluginRootFile;
        private string _plantedScriptsAsset;
        private string _plantedPluginConfig;

        /// <summary>
        /// The real static assets both of task 11.x's plugins ship, exactly as their views name
        /// them.
        /// </summary>
        /// <remarks>
        /// <c>facebookstyles.css</c> and the PNG it references come from
        /// <c>Views/PublicInfo.cshtml</c>'s <c>AddCssFileParts</c> plus the <c>background</c>
        /// declaration inside that stylesheet; <c>styles.css</c> comes from
        /// <c>Feed.GoogleShopping/Views/Configure.cshtml</c>. The PNG is included because a
        /// stylesheet that serves while its images do not is the same invisible-button failure one
        /// step later.
        /// </remarks>
        private static readonly string[] MustServe =
        {
            "/Plugins/ExternalAuth.Facebook/Content/facebookstyles.css",
            "/Plugins/ExternalAuth.Facebook/Content/Images/facebook-signing.png",
            "/Plugins/Feed.GoogleShopping/Content/styles.css",
            //Task 15.3 - Widgets.NivoSlider is the first plugin that ships a Scripts/ tree as well
            //as a Content/ tree. Its Views/PublicInfo.cshtml names all four of these by ordinary
            //~/Plugins/... URL (Html.AddScriptParts / Html.AddCssFileParts). The Scripts half of the
            //allow-list rule was pinned with a planted file at task 11.1
            //(Task_11_1_the_Scripts_half_of_the_rule_works_for_15_3); these are the FIRST real
            //Scripts/ asset URLs, and the sample-images jpg the storefront slider actually renders.
            "/Plugins/Widgets.NivoSlider/Scripts/jquery.nivo.slider.js",
            "/Plugins/Widgets.NivoSlider/Content/nivoslider/nivo-slider.css",
            "/Plugins/Widgets.NivoSlider/Content/nivoslider/themes/custom/custom.css",
            "/Plugins/Widgets.NivoSlider/Content/nivoslider/sample-images/banner1.jpg"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                //A refusal is a 404 on an installed store and a 302 to /install while
                //uninstalled; following the redirect would turn it into a 200 of the install page
                //and make every refusal assertion ambiguous. Same as AdminStaticAssetTests.
                AllowAutoRedirect = false
            });

            //--- plant the files whose refusal must not be vacuous ---------------------------
            //(1) ~/Plugins/bin is PluginManager's shadow-copy directory: every installed plugin's
            //    assembly is copied there and loaded from there. It is in DeniedSubpaths.
            _plantedShadowCopy = CommonHelper.MapPath("~/Plugins/bin/11_1_smoke_planted.txt");
            //(2) a file directly inside a plugin folder, i.e. at the level that holds the
            //    assembly, Description.txt and .deps.json. Extension chosen so DeniedExtensions
            //    is NOT what refuses it - the STRUCTURAL rule has to be what does.
            _plantedPluginRootFile =
                CommonHelper.MapPath("~/Plugins/ExternalAuth.Facebook/11_1_smoke_planted.txt");
            //(3) the Scripts half of the rule, which neither 11.x plugin exercises but 15.3 needs.
            _plantedScriptsAsset =
                CommonHelper.MapPath("~/Plugins/ExternalAuth.Facebook/Scripts/11_1_smoke_planted.js");
            //(4) a .config INSIDE an allowed Content tree, so the extension deny-list is shown to
            //    still apply after the widening rather than being bypassed by it.
            _plantedPluginConfig =
                CommonHelper.MapPath("~/Plugins/ExternalAuth.Facebook/Content/11_1_smoke_planted.config");

            foreach (var planted in Planted())
            {
                Directory.CreateDirectory(Path.GetDirectoryName(planted));
                File.WriteAllText(planted,
                    "PLANTED BY Nop.Web.SmokeTests task 11.1 - stands in for real plugin deployment content.");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();

            foreach (var planted in Planted())
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

            //the Scripts directory is ours; remove it if we created it and it is now empty
            try
            {
                var scripts = _plantedScriptsAsset == null ? null : Path.GetDirectoryName(_plantedScriptsAsset);
                if (scripts != null && Directory.Exists(scripts) &&
                    !Directory.EnumerateFileSystemEntries(scripts).Any())
                    Directory.Delete(scripts);
            }
            catch (IOException)
            {
            }
        }

        private IEnumerable<string> Planted()
        {
            return new[]
            {
                _plantedShadowCopy, _plantedPluginRootFile, _plantedScriptsAsset, _plantedPluginConfig
            }.Where(p => p != null);
        }

        // -----------------------------------------------------------------------------------
        // Deferral 10.x-1 — the assets serve
        // -----------------------------------------------------------------------------------

        [Test, TestCaseSource(nameof(MustServe))]
        public void Deferral_10_x_1_a_plugin_static_asset_serves(string path)
        {
            //Before task 11.1 every one of these returned 404 (302 in install mode), because
            //NopStaticFileProvider's allow-list covered only Content/, Scripts/,
            //Administration/{Content,Scripts}/ and Themes/{theme}/Content/.
            var onDisk = CommonHelper.MapPath("~" + path);
            Assert.IsTrue(File.Exists(onDisk),
                "PREMISE BROKEN: " + path + " is not deployed, so serving it proves nothing about " +
                "the allow-list. Expected at " + onDisk + ". Each plugin's project file must carry " +
                "the <None Remove=\"Content\\**\" /> + <Content Include=\"Content\\**\" " +
                "CopyToOutputDirectory=\"Always\" /> pair - under Microsoft.NET.Sdk.Razor the " +
                "default Content glob covers only .cshtml/.razor, so a bare Update matches nothing.");

            var response = _client.GetAsync(path).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "plugin static asset does not serve: " + path + Environment.NewLine +
                "This is runtime deferral 10.x-1. NopStaticFileProvider needs the third-level " +
                "Plugins/<ShortName>/{Content,Scripts} rule (AllowedPluginRoots).");

            var body = response.Content.ReadAsByteArrayAsync().Result;
            Assert.Greater(body.Length, 0, "served an EMPTY body for " + path);
        }

        [Test]
        public void Task_11_1_the_Facebook_button_css_really_is_load_bearing()
        {
            //This is what makes the fixture non-cosmetic. Views/PublicInfo.cshtml renders
            //    <a href="..." class="facebook-btn"></a>
            //with no text and no inline style, so if the stylesheet 404s the anchor has zero size
            //and the login button VANISHES rather than looking wrong. Asserted from the stylesheet
            //itself so a future edit that moved the sizing inline would make this fail loudly
            //instead of leaving a stale claim in a comment.
            var css = File.ReadAllText(
                CommonHelper.MapPath("~/Plugins/ExternalAuth.Facebook/Content/facebookstyles.css"));
            TestContext.WriteLine(css);

            StringAssert.Contains(".facebook-btn", css,
                "facebookstyles.css no longer styles .facebook-btn, which is the class " +
                "Views/PublicInfo.cshtml puts on the login anchor.");
            Assert.IsTrue(css.Contains("width") && css.Contains("height"),
                "facebookstyles.css no longer sizes the button, so the claim that a 404 makes it " +
                "invisible is stale - re-read Views/PublicInfo.cshtml before editing this test.");
            StringAssert.Contains("facebook-signing.png", css,
                "the stylesheet no longer references its background image, so the PNG entry in " +
                "MustServe is no longer justified.");
        }

        [Test]
        public void Deferral_10_x_1_plugin_assets_get_the_same_cache_headers_as_every_other_asset()
        {
            //Proves the plugin assets go through the SAME StaticFileOptions - i.e. task 7.4's
            //translation of 3.90's <clientCache cacheControlMaxAge="7.00:00:00"/> applies to them
            //too, rather than a second unconfigured pipeline having been introduced.
            var response = _client.GetAsync("/Plugins/Feed.GoogleShopping/Content/styles.css").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var cacheControl = response.Headers.CacheControl != null
                ? response.Headers.CacheControl.ToString() : string.Empty;
            TestContext.WriteLine("Cache-Control: " + cacheControl);
            StringAssert.Contains("max-age=604800", cacheControl,
                "plugin assets are not getting 3.90's seven-day <clientCache> max-age");
            StringAssert.Contains("public", cacheControl);
        }

        [Test]
        public void Deferral_10_x_1_plugin_assets_are_cache_busted_for_free()
        {
            //Task 7.4 assigned the same NopStaticFileProvider instance to
            //IWebHostEnvironment.WebRootFileProvider, which is what IFileVersionProvider resolves
            //against - so anything the provider serves is also versioned and the two cannot drift.
            //Plugin views reach the versioning through Html.AddCssFileParts -> PageHeadBuilder,
            //so this is the mechanism behind the ~/Plugins/... URL those views emit.
            var versionProvider = _factory.Services.GetRequiredService<IFileVersionProvider>();

            const string assetPath = "/Plugins/ExternalAuth.Facebook/Content/facebookstyles.css";
            var versioned = versionProvider.AddFileVersionToPath("/", assetPath);
            TestContext.WriteLine(assetPath + "  ->  " + versioned);

            StringAssert.Contains("?v=", versioned,
                "IFileVersionProvider left a plugin asset unversioned - it cannot see the file, so " +
                "PageHeadBuilder's cache busting is inert for every plugin stylesheet.");
            Assert.AreEqual(HttpStatusCode.OK, _client.GetAsync(versioned).Result.StatusCode,
                "the versioned plugin asset URL does not serve: " + versioned);

            //a path OUTSIDE the allow-list must come back unversioned, which is what proves the
            //check above measures the provider rather than an unconditional suffix
            const string denied = "/Plugins/ExternalAuth.Facebook/Description.txt";
            Assert.AreEqual(denied, versionProvider.AddFileVersionToPath("/", denied),
                "a denied plugin path was versioned - the provider is not the allow-list after all");
        }

        [Test]
        public void Task_11_1_the_Scripts_half_of_the_rule_works_for_15_3()
        {
            //Neither of task 11.x's plugins ships a Scripts/ tree; Widgets.NivoSlider (15.3) does,
            //and names ~/Plugins/Widgets.NivoSlider/Scripts/jquery.nivo.slider.js from its
            //PublicInfo.cshtml. Asserting that half here with a PLANTED file means 15.3 inherits a
            //proven rule instead of finding out at render time.
            Assert.IsTrue(File.Exists(_plantedScriptsAsset),
                "PREMISE BROKEN: the planted script was not created at " + _plantedScriptsAsset);

            var response = _client
                .GetAsync("/Plugins/ExternalAuth.Facebook/Scripts/11_1_smoke_planted.js").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "the Scripts half of AllowedPluginRoots does not serve. Task 15.3 depends on it.");
        }

        [Test]
        public void Task_15_3_NivoSliders_real_Scripts_and_Content_assets_serve()
        {
            //Task 11.1 pinned the Scripts/ half of the rule with a PLANTED file specifically so
            //15.3 would inherit a proven rule. This is the same rule exercised by the FIRST plugin
            //that actually ships a Scripts/ tree - jquery.nivo.slider.js and the nivoslider css /
            //sample images that Views/PublicInfo.cshtml references by ordinary ~/Plugins/... URL.
            //(The paths are also in MustServe, which asserts existence-on-disk + 200 + non-empty
            //body for every one; this test states the 15.3-specific reasoning and would fail loudly
            //if the asset tree stopped deploying.)
            foreach (var path in new[]
            {
                "/Plugins/Widgets.NivoSlider/Scripts/jquery.nivo.slider.js",
                "/Plugins/Widgets.NivoSlider/Content/nivoslider/nivo-slider.css",
                "/Plugins/Widgets.NivoSlider/Content/nivoslider/sample-images/banner1.jpg"
            })
            {
                var onDisk = CommonHelper.MapPath("~" + path);
                Assert.IsTrue(File.Exists(onDisk),
                    "PREMISE BROKEN: " + path + " is not deployed. Task 15.3's project file must " +
                    "carry <None Remove=\"Content\\**\"/><Content Include=\"Content\\**\" " +
                    "CopyToOutputDirectory=\"Always\"/> AND the same for Scripts\\** - under " +
                    "Microsoft.NET.Sdk.Razor the default Content glob covers only .cshtml/.razor, " +
                    "so a bare Update matches nothing. Expected at " + onDisk);

                var response = _client.GetAsync(path).Result;
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                    "NivoSlider asset does not serve: " + path);
                Assert.Greater(response.Content.ReadAsByteArrayAsync().Result.Length, 0,
                    "served an EMPTY body for " + path);
            }
        }

        [Test]
        public void Task_15_3_NivoSliders_own_assembly_metadata_and_closed_paths_still_404_SECURITY()
        {
            //Widening the allow-list to reach NivoSlider's large Content/+Scripts/ trees must NOT
            //expose the plugin's own assembly, dependency graph or PluginManager metadata. Every
            //path below is a REAL file on disk (a deployed plugin), so each 404 is a decision, not a
            //file that happened not to exist. This is the same property
            //Task_11_1_widening_the_allow_list_did_not_expose_a_plugin_DEPLOYMENT_SECURITY asserts
            //for the 11.x plugins, restated against the plugin with the biggest asset surface.
            var mustNotServe = new[]
            {
                "/Plugins/Widgets.NivoSlider/Nop.Plugin.Widgets.NivoSlider.dll",
                "/Plugins/Widgets.NivoSlider/Nop.Plugin.Widgets.NivoSlider.deps.json",
                "/Plugins/Widgets.NivoSlider/Description.txt"
            };
            foreach (var path in mustNotServe)
            {
                var onDisk = CommonHelper.MapPath("~" + path);
                Assert.IsTrue(File.Exists(onDisk),
                    "PREMISE BROKEN: " + path + " is not on disk, so refusing it proves nothing. " +
                    "Expected at " + onDisk);
            }
            foreach (var path in mustNotServe)
                AssertNotServed(path);

            //A mis-cased asset URL still 404s from the filesystem (PhysicalFileProvider is
            //case-sensitive on Linux) - the allow-list matches the DIRECTORY segment
            //case-insensitively, the FILE name exactly. Same property as
            //Task_11_1_a_plugin_asset_url_is_case_exact_against_the_filesystem.
            Assert.AreEqual(HttpStatusCode.OK,
                _client.GetAsync("/Plugins/Widgets.NivoSlider/Scripts/jquery.nivo.slider.js")
                    .Result.StatusCode);
            AssertNotServed("/Plugins/Widgets.NivoSlider/Scripts/JQUERY.NIVO.SLIDER.JS");
        }

        // -----------------------------------------------------------------------------------
        // The surface that must STAY closed — SECURITY
        // -----------------------------------------------------------------------------------

        [Test]
        public void Task_11_1_widening_the_allow_list_did_not_expose_a_plugin_DEPLOYMENT_SECURITY()
        {
            //A plugin folder is not a curated asset tree - it is a DEPLOYMENT directory an
            //administrator adds to at runtime by unpacking a plugin zip, and PluginManager does not
            //clean it on uninstall. So the rule has to refuse everything at the plugin-folder level
            //STRUCTURALLY rather than leaning on DeniedExtensions to catch file types one at a time.
            //
            //Every path below is a REAL file on disk (asserted first), so each 404 is a decision.
            var mustNotServe = new List<string>
            {
                //the plugin ASSEMBLY. 3.90 refused this explicitly, with a DenyAccessToPluginDLLs
                //HttpForbiddenHandler in Web.config - the one thing it did refuse under ~/Plugins.
                "/Plugins/ExternalAuth.Facebook/Nop.Plugin.ExternalAuth.Facebook.dll",
                "/Plugins/Feed.GoogleShopping/Nop.Plugin.Feed.GoogleShopping.dll",

                //the .deps.json - new in this migration (deferral 10.x-3) and the one deployed file
                //whose extension DeniedExtensions does NOT cover, so only the structural rule
                //refuses it. It lists the plugin's full dependency graph with versions.
                "/Plugins/ExternalAuth.Facebook/Nop.Plugin.ExternalAuth.Facebook.deps.json",

                //Description.txt - PluginManager's own metadata, not an asset
                "/Plugins/ExternalAuth.Facebook/Description.txt",
                "/Plugins/Feed.GoogleShopping/Description.txt",

                //the planted stand-in for "whatever else a third-party plugin package contained".
                //A .txt, so the extension deny-list is not what refuses it.
                "/Plugins/ExternalAuth.Facebook/11_1_smoke_planted.txt",

                //~/Plugins/bin - PluginManager's SHADOW-COPY directory, where every installed
                //plugin's assembly is copied and loaded from. In DeniedSubpaths, which is evaluated
                //before every allow rule.
                "/Plugins/bin/11_1_smoke_planted.txt",

                //a .config inside an ALLOWED Content tree: the widening must not have bypassed
                //DeniedExtensions for the paths it newly reaches.
                "/Plugins/ExternalAuth.Facebook/Content/11_1_smoke_planted.config"
            };

            //Fail loudly if a path stopped existing, rather than quietly asserting nothing.
            foreach (var path in mustNotServe)
            {
                var onDisk = CommonHelper.MapPath("~" + path);
                Assert.IsTrue(File.Exists(onDisk),
                    "PREMISE BROKEN: " + path + " is not on disk, so refusing it proves nothing. " +
                    "Expected at " + onDisk);
            }

            foreach (var path in mustNotServe)
                AssertNotServed(path);
        }

        [Test]
        public void Task_11_1_the_Plugins_directory_tree_cannot_be_enumerated_SECURITY()
        {
            //UseDirectoryBrowser is not registered, so a directory request 404s at the middleware
            //regardless - but IsAllowedDirectory is what a future DirectoryBrowser or any other
            //IFileProvider consumer would ask, and the plugin folder level must stay closed there
            //too. Requested with and without the trailing slash, both spellings.
            foreach (var path in new[]
            {
                "/Plugins", "/Plugins/",
                "/Plugins/bin", "/Plugins/bin/",
                "/Plugins/ExternalAuth.Facebook", "/Plugins/ExternalAuth.Facebook/",
                "/Plugins/Feed.GoogleShopping/"
            })
                AssertNotServed(path);
        }

        [Test]
        public void Task_11_1_a_plugin_view_is_not_deployed_at_all_so_it_cannot_be_served()
        {
            //Task 10.x's recipe sets CopyToOutputDirectory="Never" on every plugin
            //Views\**\*.cshtml, because on .NET the views are compiled into the assembly. So the
            //view tree is not part of the exposed surface in the first place - and would be refused
            //twice over if it were (segment 3 is not Content/Scripts, and .cshtml is in
            //DeniedExtensions). Asserted from BOTH sides so "404" is not credited to the wrong rule.
            foreach (var shortName in new[] { "ExternalAuth.Facebook", "Feed.GoogleShopping" })
            {
                var viewsDir = CommonHelper.MapPath("~/Plugins/" + shortName + "/Views");
                Assert.IsFalse(Directory.Exists(viewsDir),
                    "a plugin deployed its Views/ directory: " + viewsDir + ". The recipe's " +
                    "Content/Link block must set CopyToOutputDirectory=\"Never\"; note that Link " +
                    "also drives the output-copy location, so leaving the copy on lands the files " +
                    "at Plugins/<ShortName>/Plugins/<ShortName>/Views/.");
                AssertNotServed("/Plugins/" + shortName + "/Views/Configure.cshtml");
            }
        }

        [Test]
        public void Task_11_1_a_plugin_asset_url_is_case_exact_against_the_filesystem()
        {
            //Deferral 7.7-4's property, restated for the new rule. NopStaticFileProvider decides
            //ALLOW/DENY case-insensitively (matching 3.90's IIS behaviour) but PhysicalFileProvider
            //performs the lookup and IS case-sensitive on Linux - so a mis-cased URL must 404 from
            //the filesystem rather than be silently mis-classified in the allow-list. This is
            //exactly the trap that made Feed.GoogleShopping's own feed URL unreachable
            //(GoogleShoppingFeedFile documents it), so it is worth pinning here as well.
            Assert.AreEqual(HttpStatusCode.OK,
                _client.GetAsync("/Plugins/ExternalAuth.Facebook/Content/Images/facebook-signing.png")
                    .Result.StatusCode);

            //the directory segment is what the allow-list matches case-insensitively; the FILE name
            //is what the filesystem matches exactly
            AssertNotServed("/Plugins/ExternalAuth.Facebook/Content/Images/FACEBOOK-SIGNING.PNG");
        }

        [Test]
        public void Task_11_2_the_generated_feed_directory_is_still_served_and_case_correct()
        {
            //Task 7.4 deliberately kept ~/Content/files/ExportImport served - its comment says
            //narrowing it "would break that feed URL" - and Feed.GoogleShopping is the plugin that
            //URL belongs to. Task 11.2 found that the URL the plugin EMITTED was
            //"content/files/exportimport/...", all lower case, which the allow-list accepts and the
            //filesystem then refuses on Linux. GoogleShoppingFeedFile.RelativeDirectory is now the
            //single source of both the write path and the URL.
            //
            //Asserted with a PLANTED file named the way the plugin names one, so this measures
            //serving rather than the presence of a feed nobody generated.
            var relative = "Content/files/ExportImport";
            var planted = Path.Combine(CommonHelper.MapPath("~/" + relative),
                "1-googleshopping_1122334455.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(planted));
            File.WriteAllText(planted, "<rss version=\"2.0\"><channel /></rss>");
            try
            {
                Assert.AreEqual(HttpStatusCode.OK,
                    _client.GetAsync("/" + relative + "/1-googleshopping_1122334455.xml")
                        .Result.StatusCode,
                    "the generated Google Shopping feed URL does not serve. Task 7.4 kept " +
                    "~/Content/files/ExportImport in the allow-list specifically for it.");

                //and the all-lower-case spelling 3.90 emitted does NOT serve, which is the defect
                //task 11.2 fixed rather than a hypothetical
                AssertNotServed("/content/files/exportimport/1-googleshopping_1122334455.xml");
            }
            finally
            {
                try
                {
                    File.Delete(planted);
                }
                catch (IOException)
                {
                }
            }
        }

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
