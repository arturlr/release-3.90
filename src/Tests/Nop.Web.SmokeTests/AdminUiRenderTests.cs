using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core;
using Nop.Core.Data;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 8.8 — the admin UI, rendered. Everything here needs an installed store AND an
    /// authenticated administrator, which is why tasks 8.2 through 8.6 could not write any of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixture closes the checks those tasks deferred to the 8.8 gate: task 8.4's four
    /// runtime checks (§62), deferral 8.5-1's second half, deferral 8.3-2's
    /// <c>BackupFileDownload</c> authorization assertion, and task 8.6's §74 Roxy Fileman
    /// request. It is also what found the two <c>Html.Action</c> area defects recorded on
    /// <c>Nop.Web.Framework.ChildActionExtensions</c> — before that fix <b>every request in this
    /// fixture returned 500</b> and the admin UI could not render a single page.
    /// </para>
    /// <para>
    /// <b>It skips rather than weakens.</b> Without a database, or without an administrator whose
    /// credentials match <see cref="AdminEmail"/>/<see cref="AdminPassword"/>, every test
    /// <c>Assert.Ignore</c>s with the command needed to get there. Substituting an assertion that
    /// looks equivalent but does not need a login would be worse than skipping, because it would
    /// report green for a UI nobody rendered. See build-environment.md.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class AdminUiRenderTests
    {
        /// <summary>
        /// The administrator build-environment.md's install recipe creates. Not a secret: this is
        /// a throwaway local store, and the credentials must be predictable for the fixture to be
        /// able to sign in at all.
        /// </summary>
        public const string AdminEmail = "admin@gate88.local";
        public const string AdminPassword = "Gate88Pass!";

        private const string SkipNoDatabase =
            "NOT EXERCISED: no database is installed, so no admin page can be rendered. " +
            "See build-environment.md for the SQL Server + installer recipe.";

        private NopWebApplicationFactory _factory;
        private HttpClient _client;
        private bool _signedIn;
        private string _plantedImage;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                //A refusal must stay visible as a 302 to /login rather than being followed into a
                //200 of the login page, which would make the authorization assertions ambiguous.
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var login = _client.PostAsync("/login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "Email", AdminEmail },
                    { "Password", AdminPassword }
                })).Result;

            //*** THIS TEST MUST NOT BE "CAN I RENDER AN ADMIN PAGE". ***
            //An earlier version set _signedIn from `GET /Admin/ == 200`, and when a revert
            //experiment deliberately broke admin rendering, every test in this fixture SKIPPED
            //instead of failing - which is precisely the "written to skip, so the suite asserts
            //the bug" trap deferral 8.3-2 warns about. The discriminator is therefore whether the
            //request is REFUSED, not whether it succeeds: [AdminAuthorize] answers a
            //ChallengeResult, i.e. a 302 to the login path, for anyone who is not an authorised
            //administrator. Any other status - including 500 - means authentication and
            //authorization passed and a genuine failure is the fixture's business to report.
            var probe = _client.GetAsync("/Admin/").Result;
            _signedIn = login.StatusCode == HttpStatusCode.Found &&
                        probe.StatusCode != HttpStatusCode.Found;
            TestContext.WriteLine("sign-in probe: POST /login -> " + (int)login.StatusCode +
                ", GET /Admin/ -> " + (int)probe.StatusCode + ", signedIn=" + _signedIn);

            if (!_signedIn)
                return;

            //A real PNG inside Roxy Fileman's FILES_ROOT, for the imaging request. Copied from the
            //repository rather than generated, so the bytes are a genuine image file.
            var source = CommonHelper.MapPath("~/Administration/Content/images/throbber-synchronizing.gif");
            _plantedImage = CommonHelper.MapPath("~/Content/Images/uploaded/8_8_smoke_planted.png");
            var png = CommonHelper.MapPath("~/Administration/Content/images/logo.png");
            if (File.Exists(png))
                File.Copy(png, _plantedImage, true);
            else if (File.Exists(source))
                File.Copy(source, _plantedImage, true);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            try
            {
                if (_plantedImage != null && File.Exists(_plantedImage))
                    File.Delete(_plantedImage);
            }
            catch (IOException)
            {
                //best effort - a leftover probe file shows up in `git status`, not silently
            }

            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        private void RequireAdmin()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore(SkipNoDatabase);
            if (!_signedIn)
                Assert.Ignore("NOT EXERCISED: could not sign in as " + AdminEmail + ". POST /login " +
                              "did not redirect, or /Admin/ answered a ChallengeResult. Install a " +
                              "store with those credentials (build-environment.md) or update " +
                              "AdminEmail/AdminPassword in this fixture. Note this condition means " +
                              "NOT AUTHORISED only - a broken admin page reports as a failure, not " +
                              "a skip.");
        }

        private string Get(string url, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var response = _client.GetAsync(url).Result;
            var body = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            Assert.AreEqual(expected, response.StatusCode,
                "GET " + url + " -> " + (int)response.StatusCode + ". Body: " +
                (body.Length > 900 ? body.Substring(0, 900) + " …" : body));
            return body;
        }

        // -----------------------------------------------------------------------------------
        // The premise: an admin page renders at all. This is the assertion that would have
        // caught the Html.Action area defects, and it is deliberately first.
        // -----------------------------------------------------------------------------------

        [Test]
        [TestCase("/Admin/")]
        [TestCase("/Admin/Home/Index")]
        [TestCase("/Admin/Common/SystemInfo")]
        [TestCase("/Admin/Setting/GeneralCommon")]
        [TestCase("/Admin/Product/Create")]
        [TestCase("/Admin/Category/Create")]
        [TestCase("/Admin/Customer/Create")]
        [TestCase("/Admin/Discount/Create")]
        [TestCase("/Admin/Order/List")]
        [TestCase("/Admin/Language/List")]
        public void Task_8_8_an_admin_page_renders(string url)
        {
            RequireAdmin();
            var html = Get(url);

            Assert.Greater(html.Length, 20000, "suspiciously short admin page: " + url);
            StringAssert.Contains("<!DOCTYPE html>", html);
            //_AdminLayout, i.e. Areas/Admin/Views/ resolution worked end to end
            StringAssert.Contains("sidebar-menu", html, url + " did not render _AdminLayout.");
            //no exception leaked into the page
            StringAssert.DoesNotContain("NopException", html);
            StringAssert.DoesNotContain("An unhandled exception", html);
        }

        [Test]
        public void Task_8_8_the_admin_dashboard_renders_its_child_actions_deferral_7_3_1()
        {
            //The Html.Action bridge, cross-area, on the page with the most child actions.
            //Areas/Admin/Views/Home/Index.cshtml invokes NopCommerceNews, CommonStatistics,
            //OrderAverageReport, OrderIncompleteReport, LatestOrders, CustomerStatistics,
            //OrderStatistics and the two Bestsellers reports; _AdminLayout adds LanguageSelector,
            //MultistoreDisabledWarning and AclDisabledWarning.
            //
            //BEFORE the task 8.8 fix to ChildActionExtensions this returned 500 with
            //  "Html.Action('NopCommerceNews','Home'): the view 'NopCommerceNews' was not found.
            //   Searched: /Themes/DefaultClean/Views/Home/..., /Views/Home/..., /Views/Shared/..."
            //i.e. the child view was resolved through the NON-AREA location formats.
            RequireAdmin();
            var html = Get("/Admin/");

            //markup only these child actions can emit
            StringAssert.Contains("nopcommerce-news", html,
                "the NopCommerceNews child action did not render");
            StringAssert.Contains("common-statistics", html,
                "the CommonStatistics child action did not render");
        }

        [Test]
        public void Task_8_8_the_admin_layout_gets_the_ADMIN_LanguageSelector_not_the_storefronts()
        {
            //The second Html.Action area defect, asserted specifically. Common.LanguageSelector
            //exists in BOTH Nop.Web and Nop.Admin. Before the fix, FindAction matched on
            //controller+action NAME only, so _AdminLayout.cshtml - the layout of EVERY admin page -
            //invoked Nop.Web's CommonController and the request died with a 500:
            //  "The model item passed into the ViewDataDictionary is of type
            //   'Nop.Web.Models.Common.LanguageSelectorModel', but this ViewDataDictionary instance
            //   requires a model item of type 'Nop.Admin.Models.Common.LanguageSelectorModel'"
            //
            //HONEST NOTE ON WHAT CAN AND CANNOT BE ASSERTED HERE. There is no positive marker to
            //look for: Areas/Admin/Views/Common/LanguageSelector.cshtml opens with
            //`@if (Model.AvailableLanguages.Count > 1)`, and a stock install has ONE language, so
            //the correct render is EMPTY. The observable difference between the defect and the fix
            //is therefore the status code plus the absence of that message - which is a real
            //difference (500 -> 200), not a weak proxy, because the mismatch throws during model
            //activation and cannot produce a 200. The failability proof for this test is the
            //temporary revert of FindAction's area preference, which turns it red again.
            RequireAdmin();
            var html = Get("/Admin/");

            StringAssert.DoesNotContain("ViewDataDictionary", html,
                "the model-type mismatch is back: the bridge is invoking the STOREFRONT " +
                "CommonController.LanguageSelector from an admin view.");
            StringAssert.DoesNotContain("Nop.Web.Models.Common", html);
            //and the layout really did get as far as the element the selector sits in
            StringAssert.Contains("navbar-custom-menu", html,
                "_AdminLayout did not render as far as the language selector's container.");
        }

        // -----------------------------------------------------------------------------------
        // Task 8.4 §62 check (1) — the 78 @helper -> Capture conversions.
        // "One assertion covers all 78 at once, and the failure mode is SILENT: it compiles and
        //  every tab-pane is simply empty."
        // -----------------------------------------------------------------------------------

        [Test]
        [TestCase("/Admin/Product/Create", 5)]
        [TestCase("/Admin/Category/Create", 3)]
        [TestCase("/Admin/Discount/Create", 5)]
        [TestCase("/Admin/Customer/Create", 1)]
        public void Task_8_4_tab_bodies_render_INSIDE_their_tab_pane_wrappers(string url, int minPanes)
        {
            //Task 8.4 converted all 78 @helper declarations to `async Task` methods in @functions
            //wrapped by WebViewPage<TModel>.Capture(Func<Task>), which redirects the writer with
            //RazorPageBase.PushWriter/PopWriter so the body lands where the CONSUMING helper
            //renders it. 8.4 proved that on a hand-written page shape and measured the failure
            //mode by disabling PushWriter/PopWriter: every tab body was hoisted ABOVE its wrapper
            //and every <div class="tab-pane"> came out EMPTY - markup that compiles, passes the
            //gate, and is only visible in the rendered HTML.
            //
            //This asserts the real generated views: each tab-pane div must have content between
            //its opening and closing tag, and the page must not carry the hoisted-body signature
            //(a tab-pane div immediately followed by its own closing div).
            RequireAdmin();
            var html = Get(url);

            var panes = Regex.Matches(html, "class=\"tab-pane[^\"]*\"\\s+id=\"(?<id>[^\"]+)\"\\s*>")
                .Cast<Match>().ToList();
            Assert.GreaterOrEqual(panes.Count, minPanes,
                url + " rendered only " + panes.Count + " tab-pane wrappers; expected at least " +
                minPanes + ". The premise of this test is gone, so check the view, not the fix.");

            foreach (var pane in panes)
            {
                var after = html.Substring(pane.Index + pane.Length);
                //the first 200 characters after the wrapper's ">" must contain real content, not
                //whitespace followed by </div>
                var head = after.Length > 200 ? after.Substring(0, 200) : after;
                Assert.IsFalse(Regex.IsMatch(head, @"^\s*</div>"),
                    "tab-pane '" + pane.Groups["id"].Value + "' on " + url + " is EMPTY. The " +
                    "@helper -> Capture conversion has regressed: the body is being written at the " +
                    "call site instead of inside the wrapper. See WebViewPage<TModel>.Capture and " +
                    "runtime-deferrals.md §59.2.1. Content that followed: " +
                    Regex.Replace(head, @"\s+", " "));
            }
        }

        // -----------------------------------------------------------------------------------
        // Task 8.4 §62 check (2) — Menu.cshtml
        // -----------------------------------------------------------------------------------

        [Test]
        public void Task_8_4_the_admin_menu_renders_from_sitemap_config_with_nesting()
        {
            //Areas/Admin/Views/Shared/Menu.cshtml exercises three things nothing else does:
            //  * XmlSiteMap.LoadFrom("~/Administration/sitemap.config") - a PHYSICAL read through
            //    CommonHelper.MapPath, unaffected by task 8.2's view relocation, and the file is
            //    only in the publish output because of task 8.5's Link metadata;
            //  * the RECURSIVE Capture nesting - RenderMenuItem calls itself through Capture for
            //    every child node, which is why Capture uses Push/PopWriter as a STACK;
            //  * SiteMapNode.RouteValues on the ported Microsoft.AspNetCore.Routing type (task 6.4).
            RequireAdmin();
            var html = Get("/Admin/");

            StringAssert.Contains("sidebar-menu", html);
            //a top-level node, and a NESTED one - the nesting is the recursion under test
            StringAssert.Contains("treeview-menu", html,
                "no nested menu level rendered, so the recursive Capture never produced output");
            foreach (var link in new[]
            {
                "/Admin/Product/List", "/Admin/Order/List", "/Admin/Customer/List",
                "/Admin/Setting/GeneralCommon"
            })
            {
                StringAssert.Contains("href=\"" + link + "\"", html,
                    "menu link " + link + " is missing - sitemap.config was not read, or " +
                    "SiteMapNode.RouteValues did not generate the URL.");
            }
        }

        // -----------------------------------------------------------------------------------
        // Task 8.4 §62 check (3) — GetFullHtmlFieldId, the shim task 8.4 added because ASP.NET
        // Core dropped TemplateInfo.GetFullHtmlFieldId. Verified in isolation only.
        // -----------------------------------------------------------------------------------

        [Test]
        [TestCase("/Admin/Customer/Create", "DateOfBirth")]
        [TestCase("/Admin/Order/List", "StartDate")]
        [TestCase("/Admin/Order/List", "EndDate")]
        public void Task_8_4_a_datepicker_input_id_matches_the_selector_that_binds_the_widget(
            string url, string field)
        {
            //The editor template renders the <input> and then a $("#<id>").kendoDatePicker() call
            //built from ViewData.TemplateInfo.GetFullHtmlFieldId(""). A divergence between the two
            //silently strips the widget from every admin date field - the page still renders, the
            //script still runs, and it simply selects nothing.
            RequireAdmin();
            var html = Get(url);

            var input = Regex.Match(html, "<input[^>]*\\sid=\"(?<id>[^\"]*" + Regex.Escape(field) + "[^\"]*)\"");
            Assert.IsTrue(input.Success,
                "no <input> whose id contains '" + field + "' on " + url +
                " - the premise of this test is gone.");
            var id = input.Groups["id"].Value;
            TestContext.WriteLine(url + " -> input id=" + id);

            StringAssert.Contains("$(\"#" + id + "\").kendoDatePicker()", html,
                "the datepicker selector does not match the input id '" + id + "'. " +
                "GetFullHtmlFieldId and the rendered id have diverged - see " +
                "Nop.Web.Framework/ViewCompatibilityExtensions.cs.");
        }

        // -----------------------------------------------------------------------------------
        // Task 8.4 §62 check (4) — the Shared-before-controller resolution quirk.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Task_8_4_no_admin_view_currently_exercises_the_Shared_shadowing_quirk()
        {
            //HONEST RESULT, recorded rather than papered over. Task 8.4 asked 8.8 to verify the
            //3.90 quirk - a same-named Shared view SHADOWS the controller-specific one - "against
            //real admin views, which 8.2 could only prove on a probe". Measured: THERE IS NO SUCH
            //PAIR. No file name appears both in Areas/Admin/Views/Shared/ and in a controller
            //folder, so the real view set cannot exercise it and no honest end-to-end assertion is
            //available. 8.2's probe result and
            //Task_8_2_the_Admin_area_searches_Shared_BEFORE_the_controller_folder (which reads the
            //configured expander's emitted location formats) remain the evidence.
            //
            //This test is therefore the useful thing that IS assertable: it pins the absence. If
            //someone later adds Areas/Admin/Views/Shared/List.cshtml, every controller's own
            //List.cshtml would silently stop being used - so this fails and says so.
            RequireAdmin();

            var root = Path.Combine(NopWebApplicationFactory.ResolveNopWebContentRoot(),
                "Administration", "Areas", "Admin", "Views");
            var shared = Directory.GetFiles(Path.Combine(root, "Shared"), "*.cshtml")
                .Select(Path.GetFileName)
                .ToList();
            Assert.IsNotEmpty(shared, "PREMISE BROKEN: no views under " + root + "/Shared");

            var controllerViews = Directory.GetDirectories(root)
                .Where(d => !string.Equals(Path.GetFileName(d), "Shared", StringComparison.OrdinalIgnoreCase))
                .SelectMany(d => Directory.GetFiles(d, "*.cshtml")
                    .Select(f => Path.GetFileName(d) + "/" + Path.GetFileName(f)))
                .ToList();
            Assert.IsNotEmpty(controllerViews, "PREMISE BROKEN: no controller-folder views under " + root);

            var shadowed = controllerViews
                .Where(v => shared.Contains(v.Substring(v.IndexOf('/') + 1), StringComparer.OrdinalIgnoreCase))
                .ToList();

            Assert.IsEmpty(shadowed,
                "A view now exists in BOTH Areas/Admin/Views/Shared/ and a controller folder. The " +
                "admin area deliberately searches Shared FIRST (3.90 quirk, runtime-deferrals.md " +
                "§16.1/§50.1), so the SHARED one wins and the controller-specific one is dead. If " +
                "that is intended, assert it here instead of this absence. Pairs: " +
                string.Join(", ", shadowed));
        }

        // -----------------------------------------------------------------------------------
        // Deferral 8.5-1, second half — every asset an admin page emits must actually serve.
        // -----------------------------------------------------------------------------------

        [Test]
        [TestCase("/Admin/")]
        [TestCase("/Admin/Setting/GeneralCommon")]
        [TestCase("/Admin/Product/Create")]
        public void Deferral_8_5_1_every_asset_an_admin_page_emits_returns_200(string url)
        {
            //The equivalent of Deferral_40_the_install_page_only_links_assets_that_actually_serve,
            //for the admin. _AdminLayout.cshtml's 81 asset references are verified STATICALLY by
            //task 8.4's casing audit and 14 of them REPRESENTATIVELY by task 8.5's
            //AdminStaticAssetTests - but until the admin area was loadable (deferral 8.4-1) nothing
            //rendered a page and demanded that every emitted URL resolve.
            //
            //This is the assertion that would catch a mis-cased or mis-rooted asset path added in
            //future: task 8.4 found two such defects in _AdminLayout.cshtml alone
            //(~/Administration/scripts/... and ~/administration/content/images/...), both of which
            //worked on Windows and 404'd on Linux.
            RequireAdmin();
            var html = Get(url);

            //same-origin, filesystem-rooted references only. MVC route links (/Admin/Product/List)
            //are excluded - they are endpoints, not assets, and several legitimately require POST.
            var assets = Regex.Matches(html, "(?:href|src)=\"(?<u>/[^\"?#]+(?:\\?[^\"]*)?)\"")
                .Cast<Match>()
                .Select(m => m.Groups["u"].Value)
                .Where(u => Regex.IsMatch(u,
                    @"\.(css|js|gif|png|jpg|jpeg|svg|ico|woff2?|eot|ttf|otf)(\?|$)",
                    RegexOptions.IgnoreCase))
                .Distinct()
                .ToList();

            Assert.Greater(assets.Count, 15,
                "only " + assets.Count + " assets found on " + url +
                " - the extraction is broken, so a green result would be vacuous.");
            TestContext.WriteLine(url + ": checking " + assets.Count + " assets");

            var failures = new List<string>();
            foreach (var asset in assets)
            {
                var status = _client.GetAsync(asset).Result.StatusCode;
                if (status != HttpStatusCode.OK)
                    failures.Add((int)status + " " + asset);
            }

            Assert.IsEmpty(failures,
                url + " emits asset URLs that do not serve. On a case-sensitive filesystem this is " +
                "usually a casing defect in the view (task 8.4 found two in _AdminLayout.cshtml); it " +
                "can also mean NopStaticFileProvider's allow-list does not cover the path " +
                "(deferral 7.4-2). Failures:" + Environment.NewLine +
                string.Join(Environment.NewLine, failures));
        }

        // -----------------------------------------------------------------------------------
        // Deferral 8.3-2 — BackupFileDownload must require authorization.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_8_3_2_BackupFileDownload_requires_authorization_SECURITY()
        {
            //THE POINT OF THIS ACTION. 3.90 served database backups as STATIC FILES - the grid link
            //was GetStoreLocation() + "Administration/db_backups/" + name, and Nop.Web/Web.config
            //carried a .bak mimeMap so IIS would serve them. Static files never entered the MVC
            //pipeline, so they never met [AdminAuthorize]: anyone who could guess a filename could
            //download the whole database WITHOUT BEING SIGNED IN. Task 8.3 replaced the link with
            //this action, which inherits the class-level [AdminAuthorize] and re-checks
            //ManageMaintenance.
            //
            //Asserted with a SEPARATE, unauthenticated client - the fixture's own client is signed
            //in, so reusing it would prove nothing.
            RequireAdmin();

            using (var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            }))
            {
                var response = anonymous.GetAsync(
                    "/Admin/Common/BackupFileDownload?fileName=8_8_smoke_does_not_exist.bak").Result;
                var body = response.Content.ReadAsStringAsync().Result ?? string.Empty;
                TestContext.WriteLine("anonymous -> " + (int)response.StatusCode + " loc=" +
                    (response.Headers.Location == null ? "-" : response.Headers.Location.ToString()));

                //ChallengeResult through the cookie handler, i.e. a redirect to the login path -
                //never the file, and never a 200.
                Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
                    "An UNAUTHENTICATED request reached BackupFileDownload. This is the 3.90 defect " +
                    "task 8.3 fixed - database backups must not be reachable without signing in.");
                Assert.AreEqual(HttpStatusCode.Found, response.StatusCode,
                    "Expected a 302 challenge to the login path. Body: " +
                    (body.Length > 400 ? body.Substring(0, 400) : body));
                StringAssert.Contains("/login", response.Headers.Location.ToString().ToLowerInvariant());
            }

            //And the static path it replaced must STILL be refused, so the fix cannot be undone by
            //widening the static-file allow-list. (AdminStaticAssetTests asserts this against a
            //planted .bak too; here it is asserted next to the action that replaced it.)
            var staticResponse = _client.GetAsync("/Administration/db_backups/8_8_smoke_does_not_exist.bak").Result;
            Assert.AreEqual(HttpStatusCode.NotFound, staticResponse.StatusCode,
                "The 3.90 static-file path for database backups is being served again.");
        }

        [Test]
        public void Deferral_8_3_2_an_admin_page_requires_authorization_SECURITY()
        {
            //The general form: [AdminAuthorize] on BaseAdminController must gate the whole area, not
            //just the one action above. A ChallengeResult, not a 200 and not a 500.
            RequireAdmin();

            using (var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            }))
            {
                foreach (var url in new[]
                {
                    "/Admin/", "/Admin/Common/SystemInfo", "/Admin/Product/Create",
                    "/Admin/Customer/List", "/Admin/Setting/GeneralCommon"
                })
                {
                    var response = anonymous.GetAsync(url).Result;
                    Assert.AreEqual(HttpStatusCode.Found, response.StatusCode,
                        "anonymous GET " + url + " -> " + (int)response.StatusCode +
                        "; expected a 302 challenge to the login path.");
                    StringAssert.Contains("/login",
                        response.Headers.Location.ToString().ToLowerInvariant(), url);
                }
            }
        }

        // -----------------------------------------------------------------------------------
        // Task 8.6 §74 — Roxy Fileman over real HTTP. The ImageSharp port is verified at the
        // unit level by src/Tests/Nop.Admin.Tests; this is the first time it runs in a request.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Task_8_6_RoxyFileman_answers_DIRLIST_reading_its_conf_json()
        {
            //Covers GetSetting() -> conf.json, which is read through CommonHelper.MapPath as a
            //PHYSICAL path (one of the two reasons task 8.5 kept the admin Content/ tree where it
            //is rather than relocating it under wwwroot), plus FixPath/CheckPath.
            RequireAdmin();
            var body = Get("/Admin/RoxyFileman/ProcessRequest?a=DIRLIST");
            TestContext.WriteLine(body);

            StringAssert.Contains("Content/Images/uploaded", body,
                "DIRLIST did not report FILES_ROOT from conf.json.");
        }

        [Test]
        public void Task_8_6_RoxyFileman_GENERATETHUMB_returns_a_real_PNG_over_HTTP()
        {
            //Task 8.6 replaced Bitmap/Graphics/Image.GetThumbnailImage with ImageSharp, and
            //MEASURED that the System.Drawing pipeline it replaced throws
            //DllNotFoundException: libgdiplus on this platform - so on Linux this request could not
            //have produced a byte before that task. §74 asks for exactly this one request because
            //it covers ShowThumbnail, the buffered Response.Body write (Kestrel forbids synchronous
            //writes, which 3.90's Save-straight-into-the-response would have hit), GetImageEncoder
            //and CheckPath/FixPath in one go.
            RequireAdmin();
            if (_plantedImage == null || !File.Exists(_plantedImage))
                Assert.Ignore("NOT EXERCISED: could not plant an image in Roxy Fileman's FILES_ROOT.");

            var response = _client.GetAsync(
                "/Admin/RoxyFileman/ProcessRequest?a=GENERATETHUMB" +
                "&f=/Content/Images/uploaded/8_8_smoke_planted.png&width=140&height=120").Result;
            var bytes = response.Content.ReadAsByteArrayAsync().Result;
            TestContext.WriteLine("GENERATETHUMB -> " + (int)response.StatusCode + " " +
                (response.Content.Headers.ContentType == null ? "?" : response.Content.Headers.ContentType.ToString()) +
                " " + bytes.Length + " bytes");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "GENERATETHUMB failed. Body: " +
                System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(400, bytes.Length)));
            Assert.AreEqual("image/png", response.Content.Headers.ContentType.MediaType,
                "Task 8.6 encodes the thumbnail as PNG regardless of source format.");
            Assert.Greater(bytes.Length, 100, "empty thumbnail body");

            //a real PNG signature, not an error page or an empty stream
            Assert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes.Take(4).ToArray(),
                "the response is not a PNG. First bytes: " +
                BitConverter.ToString(bytes.Take(16).ToArray()));
        }

        [Test]
        public void Task_8_6_RoxyFileman_FILESLIST_reports_image_dimensions()
        {
            //ListFiles reads Width/Height with ImageSharp's header-only Image.Identify, which
            //replaced Image.FromStream. Task 8.6 also changed it to report 0x0 for a file it cannot
            //recognise instead of aborting the whole listing; a real image must still report real
            //dimensions, which is the half that proves Identify is actually working.
            RequireAdmin();
            if (_plantedImage == null || !File.Exists(_plantedImage))
                Assert.Ignore("NOT EXERCISED: could not plant an image in Roxy Fileman's FILES_ROOT.");

            var body = Get("/Admin/RoxyFileman/ProcessRequest?a=FILESLIST&d=/Content/Images/uploaded&type=image");
            TestContext.WriteLine(body);

            StringAssert.Contains("8_8_smoke_planted.png", body, "the planted image was not listed");
            Assert.IsTrue(Regex.IsMatch(body, "\"w\":\"[1-9][0-9]*\""),
                "no non-zero width reported, so Image.Identify returned nothing for a real image: " + body);
        }
    }
}
