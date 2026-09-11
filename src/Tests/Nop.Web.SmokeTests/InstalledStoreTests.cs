using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core.Data;
using Nop.Core.Domain.Seo;
using Nop.Core.Infrastructure;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 7.7, group C — the storefront, exercised against an actually installed store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the group that covers tasks 7.3's and 7.4's own prioritised list: the home page and
    /// its ~15 <c>@Html.Action</c> child actions, slug routing through
    /// <c>SlugRouteTransformer</c>, <c>@Html.Widget</c> on empty widget zones, the <c>?v=</c>
    /// cache-busting suffix, and <c>IUserAgentHelper.IsSearchEngine()</c>. None of it is reachable
    /// without a database, because <c>InstallUrlMiddleware</c> redirects every request until
    /// <c>App_Data/Settings.txt</c> exists.
    /// </para>
    /// <para>
    /// <b>Every test here is SKIPPED, never silently passed, when no store is installed</b> — see
    /// <see cref="SetUp"/>. That is deliberate: substituting a weaker assertion that looks
    /// equivalent would be worse than reporting honestly that the check did not run.
    /// </para>
    /// <para>
    /// To run this group, install a store against a reachable SQL Server (task 7.7 used
    /// <c>mcr.microsoft.com/mssql/server:2022-latest</c> on a Docker network shared with the test
    /// container) and leave <c>App_Data/Settings.txt</c> in place.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class InstalledStoreTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
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
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore("NOT EXERCISED: no database is installed (App_Data/Settings.txt absent), " +
                              "so the storefront is unreachable - InstallUrlMiddleware redirects everything to /install.");
        }

        // -----------------------------------------------------------------------------------
        // Priority 1 from tasks 7.3/7.4 — the home page and the Html.Action child-action bridge
        // -----------------------------------------------------------------------------------

        [Test]
        public void Priority_1_home_page_renders()
        {
            var response = _client.GetAsync("/").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "The storefront home page did not render. Body: " + Truncate(response));

            var html = response.Content.ReadAsStringAsync().Result;
            StringAssert.Contains("<!DOCTYPE html>", html);
            StringAssert.Contains("</html>", html);
            //Nothing the exception handler or the 404 re-execute would produce.
            StringAssert.DoesNotContain("An error occurred while processing your request", html);
        }

        [Test]
        public void Priority_1_the_home_page_child_action_fan_out_actually_rendered()
        {
            //Task 7.3 named this "the single most important thing for 7.7 to exercise": the
            //Html.Action bridge in Nop.Web/Extensions/ChildActionExtensions.cs was verified only
            //against a standalone probe app with synthetic controllers, never against
            //nopCommerce's real ones. The home page fans out to ~15 child actions.
            //
            //Each marker below is markup that ONLY the named child action's partial can emit, so
            //its presence proves that action executed and its view rendered. A silently-failing
            //bridge would render the layout with these regions empty.
            var html = _client.GetStringAsync("/").Result;

            var markers = new Dictionary<string, string>
            {
                { "Common/Logo (header logo)",              "class=\"header-logo\"" },
                { "Common/HeaderLinks (header links)",      "class=\"header-links\"" },
                { "Catalog/TopMenu (top navigation)",       "class=\"top-menu\"" },
                { "Common/Footer (footer)",                 "class=\"footer\"" },
                { "ShoppingCart/FlyoutShoppingCart",        "id=\"flyout-cart\"" },
                { "Catalog/SearchBox (search box)",         "class=\"search-box" }
            };

            var missing = markers.Where(m => !html.Contains(m.Value))
                                 .Select(m => m.Key + " (marker: " + m.Value + ")")
                                 .ToList();

            Assert.IsEmpty(missing,
                "Child actions that did NOT render through the Html.Action bridge:" + Environment.NewLine +
                string.Join(Environment.NewLine, missing));
        }

        // -----------------------------------------------------------------------------------
        // Priority 2 — slug routing and the seven suppressed name-only routes, against the
        // REAL endpoint set rather than the synthetic probe app task 7.3 used
        // -----------------------------------------------------------------------------------

        [Test]
        public void Priority_2_a_real_product_slug_resolves_through_SlugRouteTransformer()
        {
            var slug = FindActiveSlug("Product");
            if (slug == null)
                Assert.Ignore("No active Product UrlRecord in the database (was sample data installed?).");

            TestContext.WriteLine("product slug under test: /" + slug);
            var response = _client.GetAsync("/" + slug).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "SlugRouteTransformer did not resolve /" + slug + ". Body: " + Truncate(response));

            var html = response.Content.ReadAsStringAsync().Result;
            //The product-details page, not the home page and not PageNotFound.
            StringAssert.Contains("html-product-details-page", html);
        }

        [Test]
        public void Priority_2_a_literal_route_is_not_swallowed_by_the_generic_slug_route()
        {
            //runtime-deferrals.md §28.2: because endpoint Order beats precedence,
            //GenericUrlRouteProvider.Priority staying at -1000000 (so IRoutePublisher registers
            //{generic_se_name} LAST) is what keeps every literal route reachable. A probe showed
            ///cart resolving to Common/GenericUrl when the order was wrong.
            var response = _client.GetAsync("/cart").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "/cart did not resolve. Body: " + Truncate(response));
            var html = response.Content.ReadAsStringAsync().Result;
            StringAssert.Contains("order-summary-content", html,
                "/cart rendered something other than the shopping-cart page - the generic slug route may have won.");
        }

        [Test]
        public void Priority_2_an_unknown_slug_yields_the_PageNotFound_view()
        {
            //SlugRouteTransformer's urlRecord == null branch -> Common/PageNotFound, plus
            //Program.cs's UseStatusCodePagesWithReExecute. Requirement 4.6.
            var response = _client.GetAsync("/definitely-not-a-real-slug-7f3a9").Result;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            var html = response.Content.ReadAsStringAsync().Result;
            StringAssert.Contains("html-not-found-page", html,
                "A 404 did not re-execute the PageNotFound view. Body: " + Truncate(response));
        }

        // -----------------------------------------------------------------------------------
        // Priority 3 — @Html.Widget must render NOTHING for an empty widget zone, not throw
        // -----------------------------------------------------------------------------------

        [Test]
        public void Priority_3_empty_widget_zones_render_nothing_and_do_not_throw()
        {
            //Deferral 32: HtmlExtensions.Widget resolves IViewComponentHelper and invokes a view
            //component NAMED "Widget". If WidgetViewComponent were missing or misnamed, the very
            //first @Html.Widget on the page would throw
            //"A view component named 'Widget' could not be found" - so a 200 home page is already
            //most of the proof. These two pages between them carry the most widget zones.
            foreach (var path in new[] { "/", "/cart" })
            {
                var response = _client.GetAsync(path).Result;
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                    "@Html.Widget may have thrown on " + path + ". Body: " + Truncate(response));
                var html = response.Content.ReadAsStringAsync().Result;
                StringAssert.DoesNotContain("could not be found", html);
                //No plugin is installed yet, so every zone is empty and Default.cshtml must emit
                //nothing at all rather than a stray wrapper.
                StringAssert.DoesNotContain("widget-zone", html);
            }
        }

        // -----------------------------------------------------------------------------------
        // Priority 6 (7.4's numbering) — cache busting: ?v= present AND the URL still serves
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_33_emitted_asset_urls_carry_a_version_and_still_serve()
        {
            //Deferral 33/14.33 closed by task 7.4 by putting NopStaticFileProvider on
            //WebRootFileProvider, which is the provider DefaultFileVersionProvider captures. Task
            //7.4 proved the two halves independently; this is the first time they are observed
            //composed through PageHeadBuilder and a real theme.
            var html = _client.GetStringAsync("/").Result;

            var assets = Regex.Matches(html, "(?:href|src)=\"(/[^\"]+\\.(?:css|js)(?:\\?[^\"]*)?)\"")
                              .Select(m => m.Groups[1].Value)
                              .Distinct()
                              .ToList();
            Assert.IsNotEmpty(assets, "No same-origin css/js references on the home page.");
            TestContext.WriteLine("assets: " + string.Join(Environment.NewLine + "  ", assets));

            var versioned = assets.Where(a => a.Contains("?v=")).ToList();
            Assert.IsNotEmpty(versioned,
                "NOT ONE emitted asset URL carries ?v= - PageHeadBuilder's cache busting is inert " +
                "and deferral 33 is not actually closed at runtime.");

            var broken = assets.Where(a => _client.GetAsync(a).Result.StatusCode != HttpStatusCode.OK).ToList();
            Assert.IsEmpty(broken, "Emitted asset URLs that do not serve: " + string.Join(", ", broken));
        }

        // -----------------------------------------------------------------------------------
        // Priority 5 — IUserAgentHelper.IsSearchEngine(), newly live as of task 7.4
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_7_20_IsSearchEngine_is_live_and_the_crawler_file_is_written()
        {
            //Before task 7.4 authored NopConfig.UserAgentStringsPath this returned false
            //unconditionally, so crawlers were treated as customers. The FIRST call parses the
            //46 MB App_Data/browscap.xml and then WRITES App_Data/browscap.crawlersonly.xml, so
            //both the answer and the cost matter, and the directory must be writable.
            var crawlerFile = Nop.Core.CommonHelper.MapPath("~/App_Data/browscap.crawlersonly.xml");

            using (var request = new HttpRequestMessage(HttpMethod.Get, SmokeProbeMiddleware.Prefix + "searchengine"))
            {
                request.Headers.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)");
                var body = _client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                TestContext.WriteLine("Googlebot: " + body);
                StringAssert.DoesNotContain("EXCEPTION=", body);
                StringAssert.Contains("isSearchEngine=True", body,
                    "IsSearchEngine() returned false for Googlebot - deferral 7.20 is not closed at runtime.");
            }

            Assert.IsTrue(System.IO.File.Exists(crawlerFile),
                "browscap.crawlersonly.xml was not written to " + crawlerFile +
                " - App_Data may not be writable, which would mean the 46 MB parse repeats on every start.");

            //A normal browser must NOT be classified as a crawler.
            using (var request = new HttpRequestMessage(HttpMethod.Get, SmokeProbeMiddleware.Prefix + "searchengine"))
            {
                request.Headers.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
                var body = _client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                TestContext.WriteLine("Chrome: " + body);
                StringAssert.Contains("isSearchEngine=False", body);
            }
        }

        // -----------------------------------------------------------------------------------
        // Requirement 4.3 / 4.4 — more controllers, more views, and the ported result types
        // -----------------------------------------------------------------------------------

        [Test]
        public void Representative_storefront_routes_respond()
        {
            var expectations = new Dictionary<string, HttpStatusCode>
            {
                { "/", HttpStatusCode.OK },
                { "/login", HttpStatusCode.OK },
                { "/register", HttpStatusCode.OK },
                { "/cart", HttpStatusCode.OK },
                { "/wishlist", HttpStatusCode.OK },
                { "/search", HttpStatusCode.OK },
                { "/contactus", HttpStatusCode.OK },
                { "/newproducts", HttpStatusCode.OK },
                { "/robots.txt", HttpStatusCode.OK },
                { "/sitemap.xml", HttpStatusCode.OK },
                //CommonController.PageNotFound deliberately SETS 404 - it is the error view, not a page.
                { "/page-not-found", HttpStatusCode.NotFound }
            };

            var failures = new List<string>();
            foreach (var pair in expectations)
            {
                var response = _client.GetAsync(pair.Key).Result;
                if (response.StatusCode != pair.Value)
                    failures.Add(pair.Key + " -> " + (int)response.StatusCode + " " + response.StatusCode +
                                 "  " + Truncate(response));
            }
            Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
        }

        [Test]
        public void Requirement_4_6_robots_txt_and_sitemap_xml_keep_their_content_types()
        {
            //robots.txt: task 7.3 replaced Response.Write(content) + return null with
            //Content(content, MimeTypes.TextPlain), because HttpResponse.Write does not exist and
            //ASP.NET Core throws on a null action result.
            var robots = _client.GetAsync("/robots.txt").Result;
            Assert.AreEqual(HttpStatusCode.OK, robots.StatusCode);
            Assert.AreEqual("text/plain", robots.Content.Headers.ContentType.MediaType);
            StringAssert.Contains("User-agent", robots.Content.ReadAsStringAsync().Result);

            //sitemap.xml exercises ISitemapGenerator, whose UrlHelper -> IUrlHelper signature
            //change (task 6.2 §9b) propagated into ICommonModelFactory.PrepareSitemapXml.
            var sitemap = _client.GetAsync("/sitemap.xml").Result;
            Assert.AreEqual(HttpStatusCode.OK, sitemap.StatusCode);
            var xml = sitemap.Content.ReadAsStringAsync().Result;
            StringAssert.Contains("<urlset", xml);
            //It must generate absolute storefront URLs, not throw or emit empties.
            StringAssert.Contains("<loc>", xml);
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 11.20 — SECURITY. FluentValidation on a real storefront form.
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_11_20_FluentValidation_rejects_a_bad_registration_SECURITY()
        {
            //RegisterValidator: Email NotEmpty + EmailAddress, Password NotEmpty, ConfirmPassword
            //Equal(Password). If the provider were not registered, ModelState.IsValid would be
            //true and the controller would attempt to create a customer.
            //
            //The antiforgery token is fetched from the GET page and replayed, because this action
            //carries [PublicAntiForgery] and SecuritySettings.EnableXsrfProtectionForPublicStore
            //defaults to true. That makes this test cover BOTH deferral 11.20 and deferral 11.26 -
            //a token-less POST is genuinely refused (see
            //Deferral_7_7_1_a_refused_XSRF_post_answers_404_instead_of_400_KNOWN_GAP), so reaching
            //the validator at all proves IAntiforgery accepted a real token.
            var token = GetAntiforgeryToken("/register");
            var form = new Dictionary<string, string>
            {
                { "__RequestVerificationToken", token },
                { "Email", "not-an-email" },
                { "Password", "abc" },
                { "ConfirmPassword", "different" }
            };

            var response = _client.PostAsync("/register", new FormUrlEncodedContent(form)).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "Expected the register view to be re-rendered with errors. " + Truncate(response));
            var html = response.Content.ReadAsStringAsync().Result;
            StringAssert.Contains("field-validation-error", html,
                "No field-level validation errors rendered - FluentValidation did not run.");
            StringAssert.Contains("Wrong email", html,
                "The EmailAddress rule did not fire for \"not-an-email\".");
            StringAssert.Contains("The password and confirmation password do not match", html,
                "The Equal(x => x.Password) rule did not fire.");
        }

        [Test]
        public void Deferral_11_26_a_token_less_POST_is_refused_SECURITY()
        {
            //The other half of the same mechanism: without a token the action must NOT execute.
            var form = new Dictionary<string, string> { { "Email", "x@example.com" } };
            var response = _client.PostAsync("/register", new FormUrlEncodedContent(form)).Result;

            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
                "A POST with no antiforgery token was ACCEPTED - PublicAntiForgeryAttribute is not firing.");
            var html = response.Content.ReadAsStringAsync().Result;
            StringAssert.DoesNotContain("field-validation-error", html,
                "The action ran despite the missing token.");
        }

        [Test]
        public void Deferral_7_7_1_a_refused_XSRF_post_answers_404_instead_of_400_KNOWN_GAP()
        {
            //NEW FINDING (task 7.7), recorded as deferral 7.7-1 and asserted so it cannot be lost.
            //
            //PublicAntiForgeryAttribute correctly sets BadRequestResult (400) when the token is
            //missing. But Program.cs registers UseStatusCodePagesWithReExecute("/page-not-found"),
            //which fires for ANY empty-bodied 4xx/5xx - and CommonController.PageNotFound then sets
            //Response.StatusCode = 404. So the 400 is REWRITTEN to 404 with the "Page not found"
            //page. Task 7.2 recorded the imprecision (runtime-deferrals.md §23) but judged only
            //"the uncommon bare 403/400" affected; in fact it hits EVERY XSRF refusal on the public
            //store, and it will hit the whole admin surface at task 8.3.
            //
            //Not a security hole - the request IS refused and the action does NOT run (see the test
            //above) - but the status code is wrong and misleading to any client that distinguishes
            //them. When it is fixed this test should flip to expecting 400.
            var form = new Dictionary<string, string> { { "Email", "x@example.com" } };
            var response = _client.PostAsync("/register", new FormUrlEncodedContent(form)).Result;

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
                "The XSRF refusal status changed. If it is now 400, deferral 7.7-1 is fixed - " +
                "update this test and the register.");
            StringAssert.Contains("html-not-found-page", response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// Fetches a page and extracts its <c>__RequestVerificationToken</c>. The client keeps
        /// cookies (<c>WebApplicationFactoryClientOptions.HandleCookies</c> defaults to true), so
        /// the matching antiforgery cookie is carried into the POST.
        /// </summary>
        private string GetAntiforgeryToken(string path)
        {
            var html = _client.GetStringAsync(path).Result;
            var match = Regex.Match(html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"");
            if (!match.Success)
                match = Regex.Match(html,
                    "value=\"(?<token>[^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
            Assert.IsTrue(match.Success, "No __RequestVerificationToken in " + path);
            return match.Groups["token"].Value;
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 4.7b — lazy-loading proxies made entity serialization cycle-prone
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_4_7b_JSON_endpoints_do_not_hit_an_entity_object_cycle()
        {
            //Enabling Microsoft.EntityFrameworkCore.Proxies (deferral 4.7) introduced a NEW
            //hazard: System.Text.Json throws "A possible object cycle was detected" for any
            //Nop.Core.Domain entity whose navigations now lazy-load. nopCommerce's convention is
            //to project to view models, so these should be safe - this test finds out rather than
            //assuming.
            var jsonEndpoints = new[]
            {
                //"a" is shorter than CatalogSettings.ProductSearchTermMinimumLength, for which the action
                //returns Content("") by design - so a real term is used.
                "/catalog/searchtermautocomplete?term=book",
                "/country/getstatesbycountryid?countryId=1&addSelectStateItem=true"
            };

            var failures = new List<string>();
            foreach (var path in jsonEndpoints)
            {
                var response = _client.GetAsync(path).Result;
                var body = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    failures.Add(path + " -> " + (int)response.StatusCode);
                    continue;
                }
                try
                {
                    using (JsonDocument.Parse(body)) { }
                }
                catch (JsonException ex)
                {
                    failures.Add(path + " -> unparsable JSON: " + ex.Message);
                }
            }
            Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 39 — SECURITY, re-checked with Settings.txt actually on disk
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_39_Settings_txt_containing_the_connection_string_returns_404_SECURITY()
        {
            var settingsTxt = Nop.Core.CommonHelper.MapPath("~/App_Data/Settings.txt");
            Assert.IsTrue(System.IO.File.Exists(settingsTxt),
                "Settings.txt must exist for this test to mean anything: " + settingsTxt);

            //With a store installed there is no install redirect, so this is the unambiguous case:
            //a real file under the content root, refused with a plain 404 by the allow-list.
            foreach (var path in new[] { "/App_Data/Settings.txt", "/appsettings.json", "/web.config" })
            {
                var response = _client.GetAsync(path).Result;
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
                    "SERVED " + path + " - this is a data-exposure bug.");
                Assert.IsFalse(response.Content.ReadAsStringAsync().Result.Contains("Data Source"),
                    "Response body for " + path + " leaked connection-string text.");
            }
        }

        // -----------------------------------------------------------------------------------
        // Runtime deferral 7.3-4 — former [ChildActionOnly] actions ARE reachable by URL
        // -----------------------------------------------------------------------------------

        [Test]
        public void Deferral_7_3_4_former_child_actions_are_reachable_by_URL_KNOWN_GAP()
        {
            //Now observable for real, which install mode could not do. This asserts the CURRENT
            //(undesired) behaviour so that task 8.3's fix - a marker attribute plus an
            //IActionModelConvention adding SuppressMatchingMetadata, per runtime-deferrals.md
            //§32.2 - has a test that flips when it lands.
            var response = _client.GetAsync("/Common/Footer").Result;
            TestContext.WriteLine("/Common/Footer -> " + (int)response.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "Former child actions are no longer URL-reachable - deferral 7.3-4 is fixed, update this test.");
            var html = response.Content.ReadAsStringAsync().Result;
            StringAssert.Contains("class=\"footer\"", html);
            StringAssert.DoesNotContain("<!DOCTYPE html>", html,
                "Expected the bare partial, which is exactly the exposure 7.3-4 describes.");
        }

        // -----------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------

        private static string FindActiveSlug(string entityName)
        {
            var containerManager = EngineContext.Current.ContainerManager;
            using (var scope = containerManager.Container.BeginLifetimeScope())
            {
                var repository = containerManager.Resolve<Nop.Core.Data.IRepository<UrlRecord>>(scope: scope);
                return repository.Table
                    .Where(x => x.EntityName == entityName && x.IsActive)
                    .OrderBy(x => x.Id)
                    .Select(x => x.Slug)
                    .FirstOrDefault();
            }
        }

        private static string Truncate(HttpResponseMessage response)
        {
            var body = response.Content.ReadAsStringAsync().Result ?? string.Empty;
            return body.Length <= 600 ? body : body.Substring(0, 600) + " …";
        }
    }
}
