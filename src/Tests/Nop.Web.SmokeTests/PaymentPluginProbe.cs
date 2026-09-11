using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Plugins;
using Nop.Services.Payments;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// The <c>/__smoke/payments</c> probe — tasks 12.1–12.5, the five Payments plugins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is a separate probe rather than five more names in
    /// <c>SmokeProbeMiddleware.WritePluginsProbe</c>.</b> Two reasons, one practical and one
    /// substantive. Practically, group 11 (<c>ExternalAuth.Facebook</c>,
    /// <c>Feed.GoogleShopping</c>) is editing <c>WritePluginsProbe</c> and
    /// <c>PluginViewRenderTests</c> concurrently with task 12; keeping this in its own file
    /// reduces the change to <c>SmokeProbeMiddleware</c> to a single <c>case</c> line.
    /// Substantively, the payment plugins have facts to report that no other plugin group has:
    /// the <c>IPaymentMethod</c> contract's action/controller triple (the reason the
    /// <c>Html.Action</c> bridge exists at all), a private third-party assembly that must be
    /// deployed, and two .NET Framework facades that must resolve inside the host.
    /// </para>
    /// <para>
    /// It reports; it asserts nothing. <c>PaymentPluginTests</c> does the asserting, with
    /// exact-line matches against this output, exactly as <c>PluginViewRenderTests</c> does for
    /// the <c>/__smoke/plugins</c> report.
    /// </para>
    /// </remarks>
    public static class PaymentPluginProbe
    {
        /// <summary>
        /// The five short names, i.e. the <c>Plugins\&lt;ShortName&gt;\</c> directory names and
        /// the prefix of every compiled Razor identifier these plugins own.
        /// </summary>
        public static readonly string[] ShortNames =
        {
            "Payments.CheckMoneyOrder",
            "Payments.Manual",
            "Payments.PayPalDirect",
            "Payments.PayPalStandard",
            "Payments.PurchaseOrder"
        };

        public static void Write(HttpContext context, StringBuilder sb)
        {
            var assemblyNames = ShortNames.Select(n => "Nop.Plugin." + n).ToArray();

            //--- (1) discovery, compatibility, load context and deployment shape --------------
            var descriptors = PluginManager.ReferencedPlugins;
            sb.AppendLine("nopVersion=" + NopVersion.CurrentVersion);
            var incompatible = PluginManager.IncompatiblePlugins;
            sb.AppendLine("incompatibleCount=" + (incompatible == null ? -1 : incompatible.Count()));
            if (incompatible != null)
                foreach (var name in incompatible)
                    sb.AppendLine("incompatible=" + name);

            if (descriptors != null)
            {
                foreach (var assemblyName in assemblyNames)
                {
                    var d = descriptors.FirstOrDefault(x => x.ReferencedAssembly != null &&
                        x.ReferencedAssembly.GetName().Name == assemblyName);
                    sb.AppendLine("plugin:" + assemblyName + ".discovered=" + (d != null));
                    if (d == null)
                        continue;

                    sb.AppendLine("plugin:" + assemblyName + ".systemName=" + d.SystemName);
                    sb.AppendLine("plugin:" + assemblyName + ".supportsCurrentVersion=" +
                        d.SupportedVersions.Contains(NopVersion.CurrentVersion,
                            StringComparer.InvariantCultureIgnoreCase));
                    sb.AppendLine("plugin:" + assemblyName + ".pluginType=" +
                        (d.PluginType == null ? "<null>" : d.PluginType.FullName));
                    //IPaymentMethod, not just IPlugin: a payment plugin the host cannot cast to
                    //IPaymentMethod is discovered, loaded and then invisible to IPaymentService.
                    sb.AppendLine("plugin:" + assemblyName + ".assignableToIPaymentMethod=" +
                        (d.PluginType != null && typeof(IPaymentMethod).IsAssignableFrom(d.PluginType)));
                    sb.AppendLine("plugin:" + assemblyName + ".loadContextIsDefault=" +
                        ReferenceEquals(
                            System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(d.ReferencedAssembly),
                            System.Runtime.Loader.AssemblyLoadContext.Default));
                    sb.AppendLine("plugin:" + assemblyName + ".assemblyVersion=" +
                        d.ReferencedAssembly.GetName().Version);
                    sb.AppendLine("plugin:" + assemblyName + ".installed=" + d.Installed);

                    if (d.OriginalAssemblyFile != null && d.OriginalAssemblyFile.Directory != null)
                    {
                        var dir = d.OriginalAssemblyFile.Directory;
                        sb.AppendLine("plugin:" + assemblyName + ".deployDir=" + dir.Name);
                        sb.AppendLine("plugin:" + assemblyName + ".deployDirParent=" +
                            (dir.Parent == null ? "<null>" : dir.Parent.Name));
                        //Private="false" + NopPluginDoNotDeployHostAssemblies (§83.3).
                        var strayNopDlls = dir.GetFiles("Nop.*.dll", SearchOption.AllDirectories)
                            .Where(f => !string.Equals(f.Name, assemblyName + ".dll",
                                StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name)
                            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        sb.AppendLine("plugin:" + assemblyName + ".strayNopDlls=" +
                            (strayNopDlls.Count == 0 ? "<none>" : string.Join("|", strayNopDlls)));
                        //Task 12.3: EVERY dll other than the plugin's own. For four of the five
                        //this must be empty; for Payments.PayPalDirect it must be exactly
                        //PayPal.dll, and the fact that it is NOT empty is the whole point - the
                        //recipe alone does not deploy it (CopyLocalLockFileAssemblies is false
                        //for a library), so this line is what would have caught the defect.
                        var otherDlls = dir.GetFiles("*.dll", SearchOption.AllDirectories)
                            .Where(f => !string.Equals(f.Name, assemblyName + ".dll",
                                StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name)
                            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        sb.AppendLine("plugin:" + assemblyName + ".otherDlls=" +
                            (otherDlls.Count == 0 ? "<none>" : string.Join("|", otherDlls)));
                        sb.AppendLine("plugin:" + assemblyName + ".deployedCshtmlCount=" +
                            dir.GetFiles("*.cshtml", SearchOption.AllDirectories).Length);
                        sb.AppendLine("plugin:" + assemblyName + ".deployedConfigCount=" +
                            dir.GetFiles("*.config", SearchOption.AllDirectories).Length);
                        sb.AppendLine("plugin:" + assemblyName + ".descriptionTxtDeployed=" +
                            File.Exists(Path.Combine(dir.FullName, "Description.txt")));
                        sb.AppendLine("plugin:" + assemblyName + ".logoDeployed=" +
                            File.Exists(Path.Combine(dir.FullName, "logo.jpg")));
                    }
                }
            }

            //--- (2) MVC application parts ----------------------------------------------------
            //Deferral 1.2's Razor half. All five ship .cshtml, so all five must contribute a
            //compiled-Razor part IN ADDITION to an AssemblyPart. Without
            //AddRazorSupportForMvc=true only the AssemblyPart appears and every view is
            //unresolvable, with no error anywhere.
            var partManager = context.RequestServices.GetRequiredService<ApplicationPartManager>();
            foreach (var assemblyName in assemblyNames)
                foreach (var part in partManager.ApplicationParts.Where(p => p.Name == assemblyName))
                    sb.AppendLine("part:" + assemblyName + "=" + part.GetType().Name);

            //--- (3) compiled Razor identifiers ------------------------------------------------
            var views = new ViewsFeature();
            partManager.PopulateFeature(views);
            var allViewPaths = views.ViewDescriptors
                .Select(v => v.RelativePath ?? string.Empty)
                .ToList();
            foreach (var shortName in ShortNames)
            {
                var prefix = "/Plugins/" + shortName + "/";
                var owned = allViewPaths
                    .Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                sb.AppendLine("identifiers:" + shortName + ".count=" + owned.Count);
                foreach (var p in owned)
                    sb.AppendLine("identifier=" + p);
            }

            //--- (4) the real view engine finds them, at the exact strings the controllers pass -
            var viewEngine = context.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine>();
            foreach (var path in ShortNames
                .SelectMany(n => new[]
                {
                    "~/Plugins/" + n + "/Views/Configure.cshtml",
                    "~/Plugins/" + n + "/Views/PaymentInfo.cshtml"
                })
                //the counterfactual: the project-relative path the views would have compiled as
                //without the Content/Link block. It MUST NOT resolve, or the Link metadata was
                //not what made the 3.90 paths work and the assertion above is vacuous.
                .Concat(new[] { "~/Views/Configure.cshtml", "~/Views/PaymentInfo.cshtml" }))
            {
                sb.AppendLine("getView:" + path + "=" + viewEngine.GetView(null, path, false).Success);
            }
            //Caller-supplied lookup, so a canary can travel this exact code path with a path
            //that cannot exist and prove the getView assertions are able to fail.
            var extraPath = context.Request.Query["getView"].ToString();
            if (!string.IsNullOrEmpty(extraPath))
                sb.AppendLine("getView:" + extraPath + "=" +
                    viewEngine.GetView(null, extraPath, false).Success);

            //--- (5) the IPaymentMethod contract, and the Html.Action bridge's target ----------
            //THIS is what distinguishes a payment plugin from every other kind. IPaymentMethod
            //hands the host an action name, a controller name and a RouteValueDictionary
            //(deferral 7.3-1), and Views/Checkout/PaymentInfo.cshtml renders it with
            //@Html.Action - one of the five dynamically-named call sites that is the entire
            //reason task 7.3 could not replace the bridge with view components. The probe
            //resolves each triple the way the bridge does, against the real
            //IActionDescriptorCollectionProvider, so "the bridge can find this plugin's
            //PaymentInfo action" is measured rather than assumed.
            var actionProvider = context.RequestServices
                .GetRequiredService<IActionDescriptorCollectionProvider>();
            var allActions = actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .ToList();

            if (descriptors != null)
                foreach (var assemblyName in assemblyNames)
                {
                    var d = descriptors.FirstOrDefault(x => x.ReferencedAssembly != null &&
                        x.ReferencedAssembly.GetName().Name == assemblyName);
                    if (d == null || d.PluginType == null)
                        continue;

                    //A payment method is instantiated here WITHOUT the container on purpose: the
                    //two route methods are pure and touch no injected service, so reflecting over
                    //an uninitialized instance reports the contract without needing an installed
                    //store. If a future plugin made them depend on state this would throw, and a
                    //throw is reported (the middleware catches and prints), not swallowed.
                    var instance = System.Runtime.CompilerServices.RuntimeHelpers
                        .GetUninitializedObject(d.PluginType) as IPaymentMethod;
                    if (instance == null)
                    {
                        sb.AppendLine("contract:" + assemblyName + ".instance=<null>");
                        continue;
                    }

                    string actionName, controllerName;
                    RouteValueDictionary routeValues;

                    instance.GetPaymentInfoRoute(out actionName, out controllerName, out routeValues);
                    sb.AppendLine("contract:" + assemblyName + ".paymentInfo=" +
                        controllerName + "/" + actionName);
                    sb.AppendLine("contract:" + assemblyName + ".paymentInfo.area=" +
                        FormatRouteValue(routeValues, "area"));
                    sb.AppendLine("contract:" + assemblyName + ".paymentInfo.resolves=" +
                        ResolvesToOneAction(allActions, controllerName, actionName));

                    instance.GetConfigurationRoute(out actionName, out controllerName, out routeValues);
                    sb.AppendLine("contract:" + assemblyName + ".configuration=" +
                        controllerName + "/" + actionName);
                    sb.AppendLine("contract:" + assemblyName + ".configuration.area=" +
                        FormatRouteValue(routeValues, "area"));
                    sb.AppendLine("contract:" + assemblyName + ".configuration.resolves=" +
                        ResolvesToOneAction(allActions, controllerName, actionName));
                }

            //--- (6) endpoints registered by the ported IRouteProviders ------------------------
            //Only two of the five have a route provider. As §83.6 records, a plugin's routes are
            //in the LIVE endpoint set only when the plugin is INSTALLED (RoutePublisher keeps
            //3.90's filter), so the count is reported alongside `installed` above rather than
            //being asserted unconditionally.
            var endpointSource = context.RequestServices.GetRequiredService<EndpointDataSource>();
            var liveEndpoints = endpointSource.Endpoints.OfType<RouteEndpoint>().ToList();
            foreach (var expected in new[]
            {
                "Plugins/PaymentPayPalStandard/PDTHandler",
                "Plugins/PaymentPayPalStandard/IPNHandler",
                "Plugins/PaymentPayPalStandard/CancelOrder",
                "Plugins/PaymentPayPalDirect/Webhook"
            })
            {
                sb.AppendLine("liveEndpoint:" + expected + ".actions=" + string.Join("|",
                    liveEndpoints
                        .Where(e => string.Equals(e.RoutePattern.RawText, expected,
                            StringComparison.OrdinalIgnoreCase))
                        .Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
                        .Where(x => x != null)
                        .Select(x => x.ControllerName + "." + x.ActionName)
                        .Distinct()
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .DefaultIfEmpty("<none>")));
            }

            //--- (7) the PayPal SDK's .NET Framework facades, resolved INSIDE the host ---------
            //Task 12.3's central risk, and the one thing a compile can never tell you. PayPal.dll
            //is a net451 assembly with AssemblyRefs to System.Web and System.Configuration.
            //Neither exists as an implementation on .NET: System.Web is a type-forwarding facade
            //in the shared framework (targets in System.Web.HttpUtility.dll) and
            //System.Configuration forwards to the OUT-OF-BAND
            //System.Configuration.ConfigurationManager package, which this host gets only
            //TRANSITIVELY through Microsoft.Data.SqlClient -> Azure.Identity (deferral 12.3-2).
            //Measured before the fix: the FIRST PayPal call - PaypalHelper.GetApiContext ->
            //new OAuthTokenCredential(...) - threw FileNotFoundException.
            foreach (var typeName in new[]
            {
                "System.Web.HttpUtility, System.Web",
                "System.Configuration.ConfigurationManager, System.Configuration",
                "System.Configuration.ConfigurationSection, System.Configuration",
                "System.Configuration.NameValueConfigurationCollection, System.Configuration"
            })
            {
                Type resolved = null;
                try { resolved = Type.GetType(typeName, false); }
                catch { /* reported as <null> below */ }
                sb.AppendLine("facadeType:" + typeName + "=" +
                    (resolved == null ? "<null>" : resolved.Assembly.GetName().Name));
            }

            //And the SDK assembly itself, loaded by PluginManager's shadow copy from the plugin
            //folder - reported by name so a missing PayPal.dll is a named failure, not a
            //mysterious one.
            var paypalAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(x => x.GetName().Name == "PayPal");
            sb.AppendLine("paypalSdkLoaded=" + (paypalAsm != null));
            if (paypalAsm != null)
            {
                sb.AppendLine("paypalSdkVersion=" + paypalAsm.GetName().Version);
                //The one call every SDK resource GET makes (SDKUtil.FormatURIPath -> the
                //System.Web facade). If the facade were broken this throws, and a throw here is
                //reported rather than hidden.
                try
                {
                    var sdkUtil = paypalAsm.GetType("PayPal.Util.SDKUtil");
                    var m = sdkUtil == null
                        ? null
                        : sdkUtil.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(x => x.Name == "FormatURIPath" &&
                                                 x.GetParameters().Length == 2);
                    sb.AppendLine("paypalFormatUriPath=" + (m == null
                        ? "<method not found>"
                        : Convert.ToString(m.Invoke(null,
                            new object[] { "v1/payments/sale/{0}", new object[] { "SALE-1" } }))));
                }
                catch (Exception ex)
                {
                    var e = ex;
                    while (e.InnerException != null) e = e.InnerException;
                    sb.AppendLine("paypalFormatUriPath=<threw " + e.GetType().Name + ">");
                }
            }
        }

        private static string FormatRouteValue(RouteValueDictionary routeValues, string key)
        {
            if (routeValues == null)
                return "<no dictionary>";
            object value;
            if (!routeValues.TryGetValue(key, out value))
                return "<absent>";
            return value == null ? "<null>" : Convert.ToString(value);
        }

        /// <summary>
        /// Reports whether exactly one non-area controller+action pair matches, the way
        /// <c>ChildActionExtensions</c> resolves an <c>Html.Action</c> call.
        /// </summary>
        private static string ResolvesToOneAction(List<ControllerActionDescriptor> allActions,
            string controllerName, string actionName)
        {
            var matches = allActions
                .Where(d => string.Equals(d.ControllerName, controllerName,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.ActionName, actionName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var types = matches
                .Select(d => d.ControllerTypeInfo.FullName)
                .Distinct()
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            return matches.Count == 0
                ? "<none>"
                : string.Join("|", types);
        }
    }
}
