using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core.Data;
using Nop.Services.Payments;
using Nop.Web.Extensions;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Tasks 12.1–12.5 — the five Payments plugins, measured inside the real host.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything here reads the <c>/__smoke/payments</c> report (see
    /// <see cref="PaymentPluginProbe"/>), which runs inside a live request against the real
    /// <c>PluginManager</c>, <c>ApplicationPartManager</c>, <c>IRazorViewEngine</c>,
    /// <c>IActionDescriptorCollectionProvider</c> and <c>EndpointDataSource</c>. The plugins are
    /// not planted: each one's <c>OutputPath</c> is 3.90's
    /// <c>Presentation\Nop.Web\Plugins\&lt;ShortName&gt;\</c>, so building them puts them exactly
    /// where production puts them.
    /// </para>
    /// <para>
    /// <b>This fixture is separate from <see cref="PluginViewRenderTests"/> on purpose</b> — see
    /// the remarks on <see cref="PaymentPluginProbe"/>. The payment plugins carry three facts no
    /// other plugin group has: the <c>IPaymentMethod</c> action/controller/<c>RouteValueDictionary</c>
    /// triple that the <c>Html.Action</c> bridge resolves (deferral 7.3-1), a private third-party
    /// assembly that must be deployed (task 12.3), and two .NET Framework facades that must
    /// resolve inside the host.
    /// </para>
    /// <para>
    /// <b>What needs a database.</b> Nothing in Group A. Group B — the HTTP render of a payment
    /// method's <c>PaymentInfo</c> view through the bridge — needs an installed store, because
    /// <c>InstallUrlMiddleware</c> redirects every request to <c>/install</c> before routing
    /// runs. Group B skips with an explicit message rather than being weakened into something
    /// that passes without rendering anything.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class PaymentPluginTests
    {
        private const string CheckMoneyOrder = "Nop.Plugin.Payments.CheckMoneyOrder";
        private const string Manual = "Nop.Plugin.Payments.Manual";
        private const string PayPalDirect = "Nop.Plugin.Payments.PayPalDirect";
        private const string PayPalStandard = "Nop.Plugin.Payments.PayPalStandard";
        private const string PurchaseOrder = "Nop.Plugin.Payments.PurchaseOrder";

        private static readonly string[] All =
        {
            CheckMoneyOrder, Manual, PayPalDirect, PayPalStandard, PurchaseOrder
        };

        private const string SkipNoDatabase =
            "NOT EXERCISED: no database is installed, so InstallUrlMiddleware redirects every " +
            "request to /install and no payment view can be rendered over HTTP. Group A still " +
            "runs. See build-environment.md for the SQL Server + installer recipe.";

        private NopWebApplicationFactory _factory;
        private HttpClient _client;
        private string _report;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            _report = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "payments").Result;
            TestContext.WriteLine("---- /__smoke/payments ----");
            TestContext.WriteLine(_report);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        private IEnumerable<string> Lines()
        {
            return _report.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim());
        }

        private void AssertReports(string expectedLine)
        {
            Assert.IsTrue(Lines().Any(l => l == expectedLine),
                "the /__smoke/payments report does not contain the exact line:\n  " + expectedLine +
                "\nFull report:\n" + _report);
        }

        private string ValueOf(string key)
        {
            var line = Lines().FirstOrDefault(l => l.StartsWith(key + "=", StringComparison.Ordinal));
            return line == null ? null : line.Substring(key.Length + 1);
        }

        #region Group A - no database required

        [Test]
        public void Task_12_x_all_five_payment_plugins_are_discovered_and_version_compatible()
        {
            foreach (var asm in All)
            {
                AssertReports("plugin:" + asm + ".discovered=True");
                //Asserted, not assumed: Description.txt's "SupportedVersions: 3.90" really does
                //satisfy NopVersion.CurrentVersion. A plugin that fails this lands in
                //IncompatiblePlugins and is skipped with no error.
                AssertReports("plugin:" + asm + ".supportsCurrentVersion=True");
            }

            AssertReports("nopVersion=3.90");
            AssertReports("incompatibleCount=0");
        }

        [Test]
        public void Task_12_x_each_payment_plugin_is_assignable_to_IPaymentMethod_in_the_default_load_context()
        {
            //The two halves are one fact. PluginManager loads a plugin into the DEFAULT
            //AssemblyLoadContext (deferral 8.2-1); had it used a separate context, the plugin
            //would carry its OWN copy of every Nop.Services type, IPaymentMethod would not be
            //the host's IPaymentMethod, the cast in IPaymentService would fail, and the payment
            //method would be discovered, loaded and then silently absent from checkout.
            foreach (var asm in All)
            {
                AssertReports("plugin:" + asm + ".loadContextIsDefault=True");
                AssertReports("plugin:" + asm + ".assignableToIPaymentMethod=True");
            }
        }

        [Test]
        public void Deferral_1_2_every_payment_plugin_contributes_a_compiled_Razor_application_part()
        {
            //All five ship .cshtml, so all five must contribute BOTH part types. Without
            //<AddRazorSupportForMvc>true</AddRazorSupportForMvc> the assembly carries no
            //[ProvideApplicationPartFactory], MVC falls back to the default factory, only an
            //AssemblyPart appears - and every view is unresolvable with no error anywhere.
            foreach (var asm in All)
            {
                AssertReports("part:" + asm + "=AssemblyPart");
                Assert.IsTrue(Lines().Any(l => l.StartsWith("part:" + asm + "=", StringComparison.Ordinal) &&
                                               l.Contains("RazorAssemblyPart")),
                    asm + " contributed no compiled-Razor application part - check " +
                    "AddRazorSupportForMvc in its project file. Full report:\n" + _report);
            }
        }

        [Test]
        public void Task_12_x_compiled_view_identifiers_are_3_90s_Plugins_paths()
        {
            //The Content/Link block's whole purpose: the compiled identifiers are
            ///Plugins/<ShortName>/Views/..., byte-identical to 3.90, so not one
            //View("~/Plugins/...") call site had to be edited.
            foreach (var shortName in PaymentPluginProbe.ShortNames)
            {
                AssertReports("identifier=/Plugins/" + shortName + "/Views/Configure.cshtml");
                AssertReports("identifier=/Plugins/" + shortName + "/Views/PaymentInfo.cshtml");
                AssertReports("identifier=/Plugins/" + shortName + "/Views/_ViewImports.cshtml");
                //Exactly three: the two views plus the _ViewImports that replaced web.config.
                //A fourth would mean something unexpected got compiled in.
                AssertReports("identifiers:" + shortName + ".count=3");
            }
        }

        [Test]
        public void Task_12_x_the_real_view_engine_finds_every_path_the_controllers_pass()
        {
            foreach (var shortName in PaymentPluginProbe.ShortNames)
            {
                AssertReports("getView:~/Plugins/" + shortName + "/Views/Configure.cshtml=True");
                AssertReports("getView:~/Plugins/" + shortName + "/Views/PaymentInfo.cshtml=True");
            }

            //THE COUNTERFACTUAL, and it is what makes the four assertions above mean something:
            //without the Link metadata the views would have compiled at their project-relative
            //paths, i.e. /Views/Configure.cshtml - in Nop.Web's own identifier namespace, where
            //they would also have collided with each other across the five plugins. Those paths
            //must NOT resolve.
            AssertReports("getView:~/Views/Configure.cshtml=False");
            AssertReports("getView:~/Views/PaymentInfo.cshtml=False");
        }

        [Test]
        public void Deferral_7_3_1_the_IPaymentMethod_route_triple_resolves_to_this_plugins_own_controller()
        {
            //THE CONTRACT THESE FIVE PLUGINS DEPEND ON, and the reason task 7.3 could not replace
            //the Html.Action bridge with view components: IPaymentMethod hands the host an action
            //name, a controller name and a RouteValueDictionary at RUNTIME, and
            //Views/Checkout/PaymentInfo.cshtml renders it with @Html.Action. If the bridge cannot
            //find the action, the payment step renders empty with no error.
            var expected = new Dictionary<string, string>
            {
                { CheckMoneyOrder, "PaymentCheckMoneyOrder" },
                { Manual, "PaymentManual" },
                { PayPalDirect, "PaymentPayPalDirect" },
                { PayPalStandard, "PaymentPayPalStandard" },
                { PurchaseOrder, "PaymentPurchaseOrder" }
            };

            foreach (var kvp in expected)
            {
                AssertReports("contract:" + kvp.Key + ".paymentInfo=" + kvp.Value + "/PaymentInfo");
                AssertReports("contract:" + kvp.Key + ".configuration=" + kvp.Value + "/Configure");

                //3.90 set {"area", null} explicitly, i.e. "not in an area". Task 8.8 made the
                //bridge area-aware (§77.1), so this value decides which candidate wins when the
                //same controller+action exists in two areas. Preserved verbatim.
                AssertReports("contract:" + kvp.Key + ".paymentInfo.area=<null>");
                AssertReports("contract:" + kvp.Key + ".configuration.area=<null>");

                //...and it resolves to exactly ONE controller type: this plugin's own.
                var expectedType = kvp.Key + ".Controllers." + kvp.Value + "Controller";
                AssertReports("contract:" + kvp.Key + ".paymentInfo.resolves=" + expectedType);
                AssertReports("contract:" + kvp.Key + ".configuration.resolves=" + expectedType);
            }
        }

        [Test]
        public void Task_12_x_a_payment_plugin_deploys_no_other_Nop_assembly()
        {
            //§83.3: a leaked Nop.*.dll is shadow-copied by PluginManager.PerformFileDeploy and
            //loaded into the default context as the process's copy of that assembly, at which
            //point typeof(IPlugin).IsAssignableFrom(t) is false for the plugin's own type.
            foreach (var asm in All)
                AssertReports("plugin:" + asm + ".strayNopDlls=<none>");
        }

        [Test]
        public void Task_12_3_PayPalDirect_deploys_its_private_PayPal_SDK_and_the_other_four_deploy_nothing()
        {
            //THE DEFECT THIS TEST EXISTS FOR. Task 10.1's template claims the
            //NopPluginDoNotDeployHostAssemblies filter's "Nop." scoping is what lets
            //Payments.PayPalDirect's private SDK deploy. That is true and NOT SUFFICIENT, and it
            //had never been exercised because no group-10 plugin has a package runtime asset:
            //$(CopyLocalLockFileAssemblies) is FALSE for a class library, so a plugin does not
            //copy its NuGet packages' runtime assets at all. Measured: with the recipe alone,
            //PayPal.dll was ABSENT from the deployment directory - and the plugin still built
            //clean, was discovered, reported compatible and could be installed. The first thing
            //to fail would have been the first real PayPal call.
            AssertReports("plugin:" + PayPalDirect + ".otherDlls=PayPal.dll");

            //The complement, which is what makes the line above a statement about PayPal rather
            //than about laxity: the other four deploy NOTHING besides their own assembly.
            foreach (var asm in new[] { CheckMoneyOrder, Manual, PayPalStandard, PurchaseOrder })
                AssertReports("plugin:" + asm + ".otherDlls=<none>");
        }

        [Test]
        public void Task_12_3_the_PayPal_SDKs_dotnet_framework_facades_resolve_inside_the_host()
        {
            //PayPal.dll is a net451 assembly with AssemblyRefs to System.Web and
            //System.Configuration - neither of which has an implementation on .NET. Both are
            //type-forwarding facades. System.Web's targets are in the shared framework
            //(System.Web.HttpUtility.dll); System.Configuration's are in the OUT-OF-BAND
            //System.Configuration.ConfigurationManager package, which this host obtains only
            //TRANSITIVELY (deferral 12.3-2). Measured without it: the first PayPal call threw
            //FileNotFoundException.
            AssertReports("facadeType:System.Web.HttpUtility, System.Web=System.Web.HttpUtility");
            foreach (var t in new[]
            {
                "System.Configuration.ConfigurationManager",
                "System.Configuration.ConfigurationSection",
                "System.Configuration.NameValueConfigurationCollection"
            })
            {
                AssertReports("facadeType:" + t + ", System.Configuration=" +
                              "System.Configuration.ConfigurationManager");
            }
        }

        [Test]
        public void Task_12_3_the_PayPal_SDK_is_loaded_and_its_System_Web_dependent_path_executes()
        {
            //Not "the file is on disk" - "PluginManager shadow-copied it, the runtime loaded it,
            //and the code path that needs the System.Web facade RUNS". SDKUtil.FormatURIPath is
            //what every SDK resource GET goes through (Sale.Get, Authorization.Get, Capture.Get,
            //Agreement.Get, Webhook.Get), i.e. the plugin's capture, refund, void and recurring
            //paths.
            AssertReports("paypalSdkLoaded=True");
            AssertReports("paypalSdkVersion=1.8.0.0");
            AssertReports("paypalFormatUriPath=v1/payments/sale/SALE-1");
        }

        [Test]
        public void Task_12_x_the_deployment_shape_is_the_one_the_recipe_intends()
        {
            foreach (var asm in All)
            {
                //IsPackagePluginFolder requires the containing directory's PARENT to be named
                //"Plugins" - the reason AppendTargetFrameworkToOutputPath=false is load-bearing.
                AssertReports("plugin:" + asm + ".deployDirParent=Plugins");
                //Runtime-read content: PluginManager parses Description.txt, the admin list shows
                //logo.jpg. Under the Razor SDK these need Content Include, not Update - an
                //Update silently matches nothing and the plugin ships without Description.txt.
                AssertReports("plugin:" + asm + ".descriptionTxtDeployed=True");
                AssertReports("plugin:" + asm + ".logoDeployed=True");
                //The .cshtml files are inside the dll now; a deployed copy is dead weight, and
                //with Link metadata it would land at a doubly-nested wrong path. The .config
                //files are gone (web.config/app.config deleted, and no .dll.config emitted).
                AssertReports("plugin:" + asm + ".deployedCshtmlCount=0");
                AssertReports("plugin:" + asm + ".deployedConfigCount=0");
            }
        }

        [Test]
        public void Task_12_4_and_12_3_the_ported_route_providers_produce_3_90s_patterns()
        {
            //Two of the five have an IRouteProvider, and their PATTERNS are external contracts -
            //PayPal is configured with these URLs, and PayPalStandardPaymentProcessor composes
            //them by hand for the return/cancel/notify parameters it sends to PayPal.
            //
            //A plugin's routes reach the LIVE endpoint set only when the plugin is INSTALLED
            //(RoutePublisher keeps 3.90's filter - §83.6), so this test asserts the two
            //directions separately and reports which one applies.
            var installed = ValueOf("plugin:" + PayPalStandard + ".installed") == "True";
            TestContext.WriteLine("Payments.PayPalStandard installed=" + installed);

            var expected = new Dictionary<string, string>
            {
                { "Plugins/PaymentPayPalStandard/PDTHandler", "PaymentPayPalStandard.PDTHandler" },
                { "Plugins/PaymentPayPalStandard/IPNHandler", "PaymentPayPalStandard.IPNHandler" },
                { "Plugins/PaymentPayPalStandard/CancelOrder", "PaymentPayPalStandard.CancelOrder" },
                { "Plugins/PaymentPayPalDirect/Webhook", "PaymentPayPalDirect.WebhookEventsHandler" }
            };

            foreach (var kvp in expected)
            {
                AssertReports("liveEndpoint:" + kvp.Key + ".actions=" +
                    (installed ? kvp.Value : "<none>"));
            }
        }

        #endregion

        #region Group B - installed store required

        [Test]
        public void Task_12_x_a_payment_methods_PaymentInfo_view_RENDERS_through_the_Html_Action_bridge()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore(SkipNoDatabase);

            //The end-to-end proof of deferral 7.3-1's contract, and the thing that had never been
            //exercised with a real payment plugin: reach the child action the way
            //Views/Checkout/PaymentInfo.cshtml reaches it, i.e. by the controller/action names
            //IPaymentMethod hands out, and check the view's own markup came back.
            //
            //Payments.CheckMoneyOrder is used because its PaymentInfo view needs no card fields
            //and no session state - it renders the DescriptionText setting - so a 200 with the
            //view's markup is unambiguous.
            var response = _client.GetAsync("/PaymentCheckMoneyOrder/PaymentInfo").Result;
            var body = response.Content.ReadAsStringAsync().Result;
            TestContext.WriteLine("GET /PaymentCheckMoneyOrder/PaymentInfo -> " +
                (int)response.StatusCode);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "the payment method's PaymentInfo action did not render. Body:\n" +
                body.Substring(0, Math.Min(2000, body.Length)));

            //the view's own markup
            StringAssert.Contains("cellpadding=\"0\"", body);
            //Layout = "" honoured and no _ViewStart applied - the reason the views were NOT
            //relocated under /Views/ (§83.1).
            Assert.IsFalse(body.Contains("<html"),
                "a plugin PaymentInfo view rendered inside a layout; Layout = \"\" was not " +
                "honoured or a _ViewStart leaked in. Body:\n" +
                body.Substring(0, Math.Min(2000, body.Length)));
        }

        [Test]
        public void Deferral_7_3_2_the_PurchaseOrder_PaymentInfo_view_survives_a_GET_with_no_form_body()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                Assert.Ignore(SkipNoDatabase);

            //THE DEFECT TASKS 12.2/12.3/12.5 FIXED, asserted over HTTP. 3.90 read Request.Form
            //unconditionally in PaymentInfo to repopulate the field after a failed validation
            //round-trip. System.Web returned an empty collection for a request with no form body;
            //ASP.NET Core THROWS InvalidOperationException. This action is rendered as a child
            //action on the GET of /checkout/paymentinfo, so without the HasFormContentType guard
            //it is an HTTP 500 on the checkout page every time.
            var response = _client.GetAsync("/PaymentPurchaseOrder/PaymentInfo").Result;
            var body = response.Content.ReadAsStringAsync().Result;
            TestContext.WriteLine("GET /PaymentPurchaseOrder/PaymentInfo -> " +
                (int)response.StatusCode);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "reading Request.Form on a request with no form body threw - the " +
                "HasFormContentType guard is missing or ineffective. Body:\n" +
                body.Substring(0, Math.Min(2000, body.Length)));
            StringAssert.Contains("PurchaseOrderNumber", body);
        }

        #endregion
    }

    /// <summary>
    /// Deferral 7.3-2 — <c>ProcessPaymentRequest.CustomValues</c> through the session JSON
    /// bridge, exercised against the REAL <c>SessionExtensions</c> helper and the REAL
    /// <c>PaymentExtensions.SerializeCustomValues</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this fixture is not part of <see cref="PaymentPluginTests"/> and needs no host.</b>
    /// The question deferral 7.3-2 asks is about a value's journey, not about a request:
    /// <c>Payments.PurchaseOrder.GetPaymentInfo</c> puts a form value into
    /// <c>CustomValues</c>; <c>CheckoutController</c> serialises the whole
    /// <c>ProcessPaymentRequest</c> into <c>ISession</c> as JSON; <c>Confirm</c> reads it back and
    /// <c>OrderProcessingService.PlaceOrder</c> persists the custom values with
    /// <c>SerializeCustomValues</c>, which calls <c>value.ToString()</c>. Everything on that path
    /// except the session store itself is pure, so the test drives it directly with a
    /// <c>DefaultHttpContext</c> and a real session — no database, no server, nothing mocked that
    /// matters.
    /// </para>
    /// <para>
    /// <b>The finding.</b> Of the five plugins, ONLY <c>Payments.PurchaseOrder</c> writes to
    /// <c>CustomValues</c> (one entry), and in 3.90 that entry is a <c>string</c>. On ASP.NET
    /// Core the form indexer returns <c>StringValues</c>, and boxing a <c>StringValues</c> into
    /// the <c>Dictionary&lt;string, object&gt;</c> would make <c>JsonSerializer</c> — which uses
    /// the RUNTIME type for an <c>object</c>-declared value — write a JSON ARRAY, which comes
    /// back as a <c>JsonElement</c> of kind <c>Array</c> and persists into the order as the
    /// literal text <c>["PO-1234"]</c>. The fix is the explicit <c>.ToString()</c> in
    /// <c>PaymentPurchaseOrderController.GetPaymentInfo</c>. Both shapes are measured below, so
    /// the fix is shown to be necessary rather than asserted to be correct.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class PaymentCustomValuesRoundTripTests
    {
        private const string Key = "PO number";
        private const string Value = "PO-1234";

        /// <summary>
        /// A minimal in-memory <see cref="ISession"/>. Nothing about the round-trip under test
        /// depends on the store's identity — <see cref="SessionExtensions"/> writes UTF-8 JSON
        /// bytes and reads them back — and using the real distributed-session implementation
        /// would drag a cache and a cookie into a question about serialization.
        /// </summary>
        private sealed class InMemorySession : ISession
        {
            private readonly Dictionary<string, byte[]> _store =
                new Dictionary<string, byte[]>(StringComparer.Ordinal);

            public bool IsAvailable { get { return true; } }
            public string Id { get { return "smoke"; } }
            public IEnumerable<string> Keys { get { return _store.Keys; } }
            public void Clear() { _store.Clear(); }
            public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken t =
                default(System.Threading.CancellationToken))
            { return System.Threading.Tasks.Task.CompletedTask; }
            public System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken t =
                default(System.Threading.CancellationToken))
            { return System.Threading.Tasks.Task.CompletedTask; }
            public void Remove(string key) { _store.Remove(key); }
            public void Set(string key, byte[] value) { _store[key] = value; }
            public bool TryGetValue(string key, out byte[] value)
            { return _store.TryGetValue(key, out value); }
        }

        private static ProcessPaymentRequest RoundTrip(object customValue)
        {
            var session = new InMemorySession();
            var request = new ProcessPaymentRequest();
            request.CustomValues.Add(Key, customValue);

            //exactly what CheckoutController.EnterPaymentInfo and .Confirm do
            session.Set("OrderPaymentInfo", request);
            return session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
        }

        [Test]
        public void Deferral_7_3_2_a_string_CustomValue_survives_the_session_and_persists_verbatim()
        {
            //This is the shape Payments.PurchaseOrder now produces, because of the explicit
            //.ToString() on the StringValues the form indexer returns.
            var recovered = RoundTrip(Value);

            Assert.IsNotNull(recovered, "the session round-trip lost the ProcessPaymentRequest");
            Assert.IsTrue(recovered.CustomValues.ContainsKey(Key),
                "the CustomValues key did not survive");

            //The type IS lost - that is deferral 7.3-2 and it is not fixable without
            //TypeNameHandling, which task 4.2 refused as a deserialization-gadget hazard. What
            //matters is that the loss is HARMLESS for a string.
            var value = recovered.CustomValues[Key];
            Assert.IsInstanceOf<JsonElement>(value,
                "expected the documented JsonElement fidelity limit; if this is now a string, " +
                "the serializer changed and deferral 7.3-2 should be re-read");
            Assert.AreEqual(JsonValueKind.String, ((JsonElement)value).ValueKind);

            //...and the only thing downstream does with it is ToString(), in
            //PaymentExtensions.SerializeCustomValues -> DictionarySerializer.WriteXml.
            Assert.AreEqual(Value, value.ToString(),
                "a string CustomValue did not round-trip verbatim");

            //The real persistence call, end to end: this is what lands in Order.CustomValuesXml
            //and what the customer's order page, the confirmation e-mail and the admin order
            //screen all display.
            var xml = recovered.SerializeCustomValues();
            StringAssert.Contains("<value>" + Value + "</value>", xml);
            Assert.AreEqual(Value, recovered.DeserializeCustomValues(xml)[Key],
                "the value did not survive the XML persistence round-trip");
        }

        [Test]
        public void Deferral_7_3_2_the_unfixed_StringValues_shape_CORRUPTS_the_stored_order_value()
        {
            //THE COUNTERFACTUAL, and the reason the .ToString() in
            //PaymentPurchaseOrderController.GetPaymentInfo is a fix rather than a flourish.
            //Without it, `form["PurchaseOrderNumber"]` boxes a StringValues into the
            //Dictionary<string, object>. StringValues implements IEnumerable<string>, and
            //System.Text.Json uses the RUNTIME type for an object-declared value, so it writes a
            //JSON ARRAY.
            var recovered = RoundTrip(new Microsoft.Extensions.Primitives.StringValues(Value));

            Assert.IsNotNull(recovered);
            var value = recovered.CustomValues[Key];
            Assert.IsInstanceOf<JsonElement>(value);
            Assert.AreEqual(JsonValueKind.Array, ((JsonElement)value).ValueKind,
                "if StringValues no longer serializes as an array, re-read deferral 7.3-2 - the " +
                "hazard this test pins may have changed shape");

            //...and this is the corruption: the order would have recorded the literal text
            //["PO-1234"] instead of PO-1234, permanently, on a money path.
            var xml = recovered.SerializeCustomValues();
            StringAssert.Contains("[\"" + Value + "\"]", xml);
            Assert.AreNotEqual(Value, recovered.DeserializeCustomValues(xml)[Key],
                "the unfixed shape did NOT corrupt the value, which would mean the fix in " +
                "PaymentPurchaseOrderController.GetPaymentInfo is unnecessary - re-read " +
                "deferral 7.3-2 before removing it");
        }

        [Test]
        public void Deferral_7_3_2_a_non_string_CustomValue_still_round_trips_by_ToString_for_primitives()
        {
            //Scope check, so the deferral can be re-scoped honestly rather than closed on a
            //single example. An int loses its CLR type too, but JsonElement.ToString() returns
            //the raw JSON number text, which is what DictionarySerializer would have written for
            //a boxed int anyway - so a primitive is safe. It is the COMPOSITE and
            //collection-shaped values that are not, which is what the StringValues test above
            //demonstrates.
            var recovered = RoundTrip(42);
            Assert.AreEqual("42", recovered.CustomValues[Key].ToString());
            StringAssert.Contains("<value>42</value>", recovered.SerializeCustomValues());
        }

        [Test]
        public void Deferral_7_3_2_the_helper_fails_soft_rather_than_throwing()
        {
            //SessionExtensions' recorded contract: an unreadable payload yields default(T) and a
            //lost stash restarts payment entry, which is what a 3.90 session expiry did. Asserted
            //because the alternative - an exception on the checkout confirm step - would be a
            //much worse failure than losing the stash.
            var session = new InMemorySession();
            session.Set("OrderPaymentInfo", Encoding.UTF8.GetBytes("this is not json"));
            Assert.IsNull(session.Get<ProcessPaymentRequest>("OrderPaymentInfo"));
        }
    }

    /// <summary>
    /// The canaries for tasks 12.1–12.5. Every one MUST fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Named so that <c>--filter "FullyQualifiedName~HarnessCanaryTests"</c> — the command
    /// build-environment.md documents — picks these up alongside the existing ten. A green suite
    /// that has never been shown to go red is not evidence.
    /// </para>
    /// <para>
    /// Each one guards a distinct mechanism the real assertions rest on: that the
    /// <c>/__smoke/payments</c> report is actually read and compared (a typo in the probe would
    /// otherwise make every <c>AssertReports</c> silently unsatisfiable — which it would, but a
    /// canary proves the failure is detected rather than swallowed), that the <c>getView:</c>
    /// half discriminates, and that the <c>CustomValues</c> round-trip helper really moves data
    /// rather than returning the same object.
    /// </para>
    /// </remarks>
    [TestFixture]
    [Explicit("Deliberately failing. Proves the payment-plugin assertions are able to fail.")]
    public class HarnessCanaryTestsPayments
    {
        [Test]
        public void CANARY_payments_probe_assertions_can_fail()
        {
            var factory = new NopWebApplicationFactory();
            try
            {
                using (var client = factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                }))
                {
                    var report = client.GetStringAsync(SmokeProbeMiddleware.Prefix + "payments").Result;
                    Assert.IsTrue(report.Split('\n').Select(l => l.Trim())
                            .Any(l => l == "plugin:Nop.Plugin.Payments.ThisPluginDoesNotExist.discovered=True"),
                        "CANARY: this must fail. If it passes, the exact-line matching every " +
                        "PaymentPluginTests assertion relies on is not working.\n" + report);
                }
            }
            finally
            {
                factory.Dispose();
            }
        }

        [Test]
        public void CANARY_payments_view_engine_assertions_can_fail()
        {
            var factory = new NopWebApplicationFactory();
            try
            {
                using (var client = factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                }))
                {
                    //The probe accepts a caller-supplied path, so the canary travels the same
                    //IRazorViewEngine.GetView code path with one that cannot exist.
                    var report = client.GetStringAsync(SmokeProbeMiddleware.Prefix +
                        "payments?getView=~/Plugins/Payments.CheckMoneyOrder/Views/NoSuchView.cshtml").Result;
                    Assert.IsTrue(report.Contains(
                            "getView:~/Plugins/Payments.CheckMoneyOrder/Views/NoSuchView.cshtml=True"),
                        "CANARY: this must fail. If it passes, the view-engine lookups the " +
                        "Content/Link assertions depend on are not discriminating.\n" + report);
                }
            }
            finally
            {
                factory.Dispose();
            }
        }

        [Test]
        public void CANARY_the_CustomValues_round_trip_really_serializes()
        {
            //If Set/Get were a pass-through, the "fixed" and "unfixed" tests would both pass for
            //the wrong reason - a string would stay a string because it never left the CLR. This
            //asserts the OPPOSITE of the truth: that the recovered value is still a string.
            var session = new PaymentCustomValuesRoundTripHarness();
            var recovered = session.RoundTrip("PO-1234");
            Assert.IsInstanceOf<string>(recovered.CustomValues["PO number"],
                "CANARY: this must fail. If it passes, the session round-trip is not actually " +
                "serializing and the deferral 7.3-2 tests are vacuous.");
        }
    }

    /// <summary>
    /// Exposes <see cref="PaymentCustomValuesRoundTripTests"/>' round-trip to the canary fixture
    /// without making the production-facing test class's internals public.
    /// </summary>
    internal sealed class PaymentCustomValuesRoundTripHarness
    {
        private sealed class Session : ISession
        {
            private readonly Dictionary<string, byte[]> _store =
                new Dictionary<string, byte[]>(StringComparer.Ordinal);
            public bool IsAvailable { get { return true; } }
            public string Id { get { return "canary"; } }
            public IEnumerable<string> Keys { get { return _store.Keys; } }
            public void Clear() { _store.Clear(); }
            public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken t =
                default(System.Threading.CancellationToken))
            { return System.Threading.Tasks.Task.CompletedTask; }
            public System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken t =
                default(System.Threading.CancellationToken))
            { return System.Threading.Tasks.Task.CompletedTask; }
            public void Remove(string key) { _store.Remove(key); }
            public void Set(string key, byte[] value) { _store[key] = value; }
            public bool TryGetValue(string key, out byte[] value)
            { return _store.TryGetValue(key, out value); }
        }

        public ProcessPaymentRequest RoundTrip(object customValue)
        {
            var session = new Session();
            var request = new ProcessPaymentRequest();
            request.CustomValues.Add("PO number", customValue);
            session.Set("OrderPaymentInfo", request);
            return session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
        }
    }
}
