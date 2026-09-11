using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;

namespace Nop.Web.Framework
{
    /// <summary>
    /// <c>@Html.Action(...)</c> — renders a controller action inline in a view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 8.3 — PROMOTED FROM <c>Nop.Web</c> (deferral 7.3-1).</b> This file was created by
    /// task 7.3 as <c>Nop.Web/Extensions/ChildActionExtensions.cs</c>. <c>Nop.Admin</c> has its own
    /// <c>@Html.Action</c> call sites, and the three plugin contracts named below are implemented
    /// by every one of the 20 plugin projects, so the bridge belongs in the shared framework
    /// rather than being duplicated per presentation project. It is a <b>move, not a rewrite</b>:
    /// the body is unchanged apart from the namespace, and task 7.7 had already verified it
    /// against nopCommerce's real controllers (the home page's ~15 child actions all render).
    /// </para>
    /// <para>
    /// The namespace is <c>Nop.Web.Framework</c> rather than <c>Nop.Web.Framework.Mvc</c>,
    /// matching the sibling <c>HtmlExtensions.cs</c> in this folder, because
    /// <c>@using Nop.Web.Framework</c> is already present in every <c>_ViewImports.cshtml</c> in
    /// the solution — so <b>no view call site had to change</b>.
    /// </para>
    /// <para>
    /// <b>TASK 7.3 — WHY THIS EXISTS.</b> ASP.NET Core removed child actions:
    /// <c>System.Web.Mvc.ChildActionExtensions.Action</c>, <c>RenderAction</c> and
    /// <c>[ChildActionOnly]</c> have no counterpart, and the intended replacement is the View
    /// Component. Converting nopCommerce's 41 child actions to view components was considered
    /// and rejected, because <b>five of the 101 call sites name the controller and action
    /// dynamically, from data</b>:
    /// <list type="bullet">
    /// <item><c>@Html.Action(widget.ActionName, widget.ControllerName, widget.RouteValues)</c>
    /// — every <c>IWidgetPlugin</c>;</item>
    /// <item><c>@Html.Action(Model.PaymentInfoActionName, Model.PaymentInfoControllerName, …)</c>
    /// and <c>Model.ButtonPaymentMethodActionNames[i]</c> — every <c>IPaymentMethod</c>;</item>
    /// <item><c>@Html.Action(eam.ActionName, eam.ControllerName, eam.RouteValues)</c> — every
    /// <c>IExternalAuthenticationMethod</c>.</item>
    /// </list>
    /// A view component is selected by CLR type or component name at compile time; it cannot be
    /// selected from a controller/action pair discovered at runtime. Those three plugin contracts
    /// still expose action/controller/<c>RouteValueDictionary</c> triples — task 6.2 changed only
    /// the <c>RouteValueDictionary</c> namespace on them — and rewriting them into
    /// "return a view component name" is a plugin-contract change owned by tasks 10.x–15.x, not
    /// by 7.3. So a bridge is required no matter what, and once it exists, using it for all 101
    /// sites keeps the views byte-identical and avoids 41 speculative refactors.
    /// </para>
    /// <para>
    /// <b>HOW IT DIFFERS FROM MVC 5, deliberately.</b> This does NOT re-run the MVC pipeline. It
    /// looks the action up in the application's own <see cref="ActionDescriptor"/> collection,
    /// activates the controller from the container, binds the supplied route values to the
    /// method's parameters, invokes it, and renders the <see cref="ViewResult"/> /
    /// <see cref="PartialViewResult"/> it returns. Consequences:
    /// <list type="number">
    /// <item><b>Action filters do not run.</b> MVC 5 ran them for child actions. This is
    /// consistent with the decision already recorded for this migration: task 6.2 removed the
    /// <c>filterContext.IsChildAction</c> guard from eleven filters precisely because "view
    /// components do not execute the action filter pipeline at all". The filters concerned
    /// (<c>StoreClosed</c>, <c>PublicStoreAllowNavigation</c>, <c>CheckAffiliate</c>, …) are
    /// declared on <c>BasePublicController</c> and have already run for the parent request.</item>
    /// <item><b>The response is never touched.</b> Output is captured into a
    /// <see cref="StringWriter"/>, so unlike a re-entrant pipeline invocation this cannot
    /// corrupt the parent response's status code, headers or body.</item>
    /// <item><b>Model binding is route-values-only</b> — no query string, no form, no body. That
    /// is exactly what a child action received in MVC 5 from an explicit <c>routeValues</c>
    /// argument, and every nopCommerce child action takes only scalars, enums and nullable
    /// scalars (verified across all 41).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Sync-over-async.</b> Rendering a Razor view is asynchronous
    /// (<c>IView.RenderAsync</c>) but <c>@Html.Action(...)</c> must return a value, so the wait
    /// is blocking. This is the fourth such site in the migration and the trade-off is
    /// unchanged: ASP.NET Core installs no <c>SynchronizationContext</c>, so it cannot deadlock.
    /// Tasks 4.2 and 6.2 recorded the first six.
    /// </para>
    /// <para>
    /// <b>Note for the plugin tasks (10.x–15.x).</b> The five dynamically-named call sites listed
    /// above are the reason this bridge cannot be replaced wholesale by view components. If those
    /// three plugin contracts are ever changed to return a view-component name instead of an
    /// action/controller/<c>RouteValueDictionary</c> triple, this class can go.
    /// </para>
    /// <para>
    /// <b>AREA-AWARENESS — TASK 8.8. TWO REAL DEFECTS, MEASURED BY RENDERING A REAL ADMIN PAGE
    /// FOR THE FIRST TIME. The admin UI could not render a single page before this.</b>
    /// Task 8.3 promoted this class from <c>Nop.Web</c> to <c>Nop.Web.Framework</c> so
    /// <c>Nop.Admin</c>'s 69 <c>@Html.Action(...)</c> call sites could use it, but nothing
    /// exercised it across an area boundary until the 8.8 gate made the Admin area loadable
    /// (runtime deferral 8.4-1). Both defects came from one omission — the bridge ignored the
    /// ambient area — and both produced a 500, not a degradation:
    /// </para>
    /// <list type="number">
    /// <item><b>The child view was resolved through the NON-AREA location formats.</b>
    /// <c>@Html.Action("NopCommerceNews", "Home")</c> from
    /// <c>Areas/Admin/Views/Home/Index.cshtml</c> failed with <i>"the view 'NopCommerceNews' was
    /// not found. Searched: /Themes/DefaultClean/Views/Home/…, /Views/Home/…, /Views/Shared/…"</i>
    /// — i.e. it never looked under <c>/Areas/Admin/Views/</c>. Cause:
    /// <c>InvokeAction</c> built the child <see cref="RouteData"/> from the caller's route values
    /// plus <c>controller</c>/<c>action</c> only, dropping <c>area</c>, and the Razor view engine
    /// selects its location formats from <c>RouteData</c>. Fixed by putting the resolved area
    /// back on the child route data.</item>
    /// <item><b>The WRONG controller was selected when a name exists in both areas.</b>
    /// <c>@Html.Action("LanguageSelector", "Common", new { area = "Admin" })</c> from
    /// <c>Areas/Admin/Views/Shared/_AdminLayout.cshtml</c> — the layout of every admin page —
    /// invoked <c>Nop.Web.Controllers.CommonController</c> and died with <i>"The model item
    /// passed into the ViewDataDictionary is of type 'Nop.Web.Models.Common.LanguageSelectorModel',
    /// but this ViewDataDictionary instance requires a model item of type
    /// 'Nop.Admin.Models.Common.LanguageSelectorModel'"</i>. Cause: <c>FindAction</c> matched on
    /// action + controller NAME only, so the two same-named pairs
    /// (<c>Common.LanguageSelector</c> and <c>Widget.WidgetsByZone</c>) were ambiguous and the
    /// fewest-parameters tie-break decided arbitrarily. Note the call site was already passing
    /// the area explicitly and it made no difference, because the route values were never
    /// consulted. Fixed by preferring candidates in the caller's area.</item>
    /// </list>
    /// <para>
    /// Precedence is: an explicit <c>area</c> in the caller's route values, else the ambient area
    /// from <c>ViewContext.RouteData</c> — which is MVC 5's behaviour and what 68 of the 69 admin
    /// call sites (and all 101 storefront ones, none of which passes an area) rely on. Matching
    /// is a PREFERENCE, not a restriction: if no candidate is in the caller's area the search
    /// falls back to all areas, so a plugin reaching from an admin view into a storefront action
    /// still works exactly as it did before 8.8.
    /// </para>
    /// <para>
    /// <b>Failability, measured rather than asserted.</b> With both halves reverted — the pre-8.8
    /// state — <b>23 of the 29</b> tests in <c>Nop.Web.SmokeTests.AdminUiRenderTests</c> fail; the
    /// only 6 that pass are the ones that do not render an admin view. Reverting the child
    /// <c>RouteData</c> half alone fails 13. <b>Reverting the <c>FindAction</c> area preference
    /// alone fails NOTHING, and that is recorded rather than glossed:</b> with the area correctly
    /// on the child route data, <c>IActionDescriptorCollectionProvider</c> currently happens to
    /// enumerate the admin candidate first, so the ambiguous pick lands on the right controller by
    /// accident. MVC guarantees no such ordering, so the preference is kept as the thing that makes
    /// the outcome deterministic — but no test pins it independently, and a future reader should
    /// not assume one does.
    /// </para>
    /// </remarks>
    public static class ChildActionExtensions
    {
        #region Public overloads (the MVC 5 shapes, preserved exactly)

        /// <summary>Invoke an action on the CURRENT controller.</summary>
        public static IHtmlContent Action(this IHtmlHelper helper, string actionName)
        {
            return Action(helper, actionName, null, (object)null);
        }

        /// <summary>Invoke an action on the CURRENT controller, with route values.</summary>
        public static IHtmlContent Action(this IHtmlHelper helper, string actionName, object routeValues)
        {
            return Action(helper, actionName, null, routeValues);
        }

        /// <summary>Invoke an action on the named controller.</summary>
        public static IHtmlContent Action(this IHtmlHelper helper, string actionName, string controllerName)
        {
            return Action(helper, actionName, controllerName, (object)null);
        }

        /// <summary>Invoke an action on the named controller, with route values.</summary>
        public static IHtmlContent Action(this IHtmlHelper helper, string actionName, string controllerName,
            object routeValues)
        {
            if (helper == null)
                throw new ArgumentNullException("helper");
            if (string.IsNullOrEmpty(actionName))
                throw new ArgumentNullException("actionName");

            var viewContext = helper.ViewContext;
            if (viewContext == null)
                throw new NopException("Html.Action requires a ViewContext");

            //"no controller name" means the current one, as in MVC 5
            if (string.IsNullOrEmpty(controllerName))
                controllerName = viewContext.RouteData.Values["controller"] as string;

            var values = ToRouteValues(routeValues);

            //=========================================================================
            //TASK 8.8 - THE AREA. Both bugs this fixes were found by rendering a real
            //admin page for the first time; see the AREA-AWARENESS block in the class
            //remarks. An explicit `area` in the caller's route values wins (exactly one
            //call site in the solution does that: _AdminLayout.cshtml's LanguageSelector);
            //otherwise the AMBIENT area is inherited, which is what MVC 5 did and what
            //the other 68 admin call sites rely on.
            //=========================================================================
            object explicitArea;
            var areaName = values.TryGetValue("area", out explicitArea)
                ? Convert.ToString(explicitArea)
                : Convert.ToString(viewContext.RouteData.Values["area"]);

            return InvokeAction(viewContext, actionName, controllerName, values, areaName ?? string.Empty);
        }

        /// <summary>
        /// MVC 5's <c>Html.RenderAction</c> — writes directly to the output instead of returning.
        /// </summary>
        /// <remarks>
        /// Retained for source compatibility with plugins; no view in <c>Nop.Web</c> uses it
        /// (verified: zero <c>Html.RenderAction</c> occurrences under Views/ and Themes/).
        /// </remarks>
        public static void RenderAction(this IHtmlHelper helper, string actionName, string controllerName = null,
            object routeValues = null)
        {
            var content = Action(helper, actionName, controllerName, routeValues);
            content.WriteTo(helper.ViewContext.Writer, HtmlEncoder.Default);
        }

        #endregion

        #region Utilities

        private static RouteValueDictionary ToRouteValues(object routeValues)
        {
            if (routeValues == null)
                return new RouteValueDictionary();

            //RouteValueDictionary is what the three plugin contracts hand us; anything else is
            //an anonymous object, which RouteValueDictionary reflects over.
            var existing = routeValues as RouteValueDictionary;
            return existing != null ? new RouteValueDictionary(existing) : new RouteValueDictionary(routeValues);
        }

        private static IHtmlContent InvokeAction(ViewContext viewContext, string actionName, string controllerName,
            RouteValueDictionary routeValues, string areaName)
        {
            var services = viewContext.HttpContext.RequestServices;

            var descriptor = FindAction(services, actionName, controllerName, areaName);
            if (descriptor == null)
                throw new NopException(string.Format(
                    "Html.Action could not find an action '{0}' on controller '{1}' in area '{2}'. In ASP.NET Core an action is only discoverable if its controller is part of an application part.",
                    actionName, controllerName, areaName));

            //route values the invoked action sees: the caller's, plus controller/action.
            //Anything the action does NOT declare as a parameter is still visible through
            //RouteData, which is what MVC 5 gave a child action too.
            var childRouteValues = new RouteValueDictionary(routeValues);
            childRouteValues["controller"] = descriptor.ControllerName;
            childRouteValues["action"] = descriptor.ActionName;

            //TASK 8.8 - the area must be on the child RouteData, or the Razor view engine
            //resolves the child view through the NON-AREA location formats and an admin view
            //is never found. The value is taken from the descriptor rather than from
            //`areaName`, so it is right even when FindAction fell back across areas.
            string resolvedArea;
            if (descriptor.RouteValues.TryGetValue("area", out resolvedArea) &&
                !string.IsNullOrEmpty(resolvedArea))
                childRouteValues["area"] = resolvedArea;
            else
                childRouteValues.Remove("area");

            var routeData = new RouteData();
            foreach (var kvp in childRouteValues)
                routeData.Values[kvp.Key] = kvp.Value;
            foreach (var router in viewContext.RouteData.Routers)
                routeData.Routers.Add(router);

            var actionContext = new ActionContext(viewContext.HttpContext, routeData, descriptor);

            var controller = CreateController(services, descriptor, actionContext);
            var result = ExecuteAction(controller, descriptor, childRouteValues);

            return RenderResult(services, actionContext, controller, descriptor, result);
        }

        /// <summary>
        /// Look the action up in the application's own action descriptors. Preferring the
        /// overload whose parameters the supplied route values can satisfy reproduces MVC 5's
        /// action-method selection closely enough for the call sites that exist.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TASK 8.8 — the <paramref name="areaName"/> filter is not optional.</b> Two
        /// controller+action pairs exist in BOTH areas: <c>Common.LanguageSelector</c> and
        /// <c>Widget.WidgetsByZone</c>. Before this filter existed, an area-agnostic name match
        /// made them ambiguous and the tie-break picked whichever had fewer parameters, so
        /// <c>Areas/Admin/Views/Shared/_AdminLayout.cshtml</c> — the layout of every admin page —
        /// invoked <b>Nop.Web</b>'s <c>CommonController.LanguageSelector</c> and died with
        /// <i>"The model item passed into the ViewDataDictionary is of type
        /// 'Nop.Web.Models.Common.LanguageSelectorModel', but this ViewDataDictionary instance
        /// requires a model item of type 'Nop.Admin.Models.Common.LanguageSelectorModel'"</i>.
        /// </para>
        /// <para>
        /// The fallback to an area-agnostic search is deliberate and preserves the pre-8.8
        /// behaviour for anything that legitimately reaches across areas: a plugin's admin view
        /// invoking a storefront action, or vice versa. Only the <i>preference</i> changed.
        /// </para>
        /// <para>
        /// <b>NOT INDEPENDENTLY PINNED BY A TEST, and that is measured.</b> Reverting this
        /// preference on its own makes no test in <c>AdminUiRenderTests</c> fail, because with the
        /// area present on the child route data the descriptor collection currently happens to
        /// enumerate the admin candidate first and the ambiguous pick is right by accident. MVC
        /// guarantees no ordering, so this preference is what makes the outcome deterministic
        /// rather than lucky — do not remove it on the grounds that nothing goes red.
        /// </para>
        /// </remarks>
        private static ControllerActionDescriptor FindAction(IServiceProvider services, string actionName,
            string controllerName, string areaName)
        {
            var provider = services.GetRequiredService<IActionDescriptorCollectionProvider>();

            var candidates = provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(x => string.Equals(x.ActionName, actionName, StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrEmpty(controllerName) ||
                             string.Equals(x.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            //prefer the candidates in the CALLER's area; fall back to all of them if none match
            var inArea = candidates.Where(x => AreaOf(x) == (areaName ?? string.Empty)).ToList();
            if (inArea.Count > 0)
                candidates = inArea;

            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

            //prefer the overload with the fewest parameters that is still unambiguous - the
            //nopCommerce child actions have no overloads, so this only ever breaks ties
            //introduced by [HttpGet]/[HttpPost] pairs on the same name.
            return candidates.OrderBy(x => x.Parameters.Count).First();
        }

        private static string AreaOf(ControllerActionDescriptor descriptor)
        {
            string area;
            if (descriptor.RouteValues != null && descriptor.RouteValues.TryGetValue("area", out area))
                return area ?? string.Empty;
            return string.Empty;
        }

        private static object CreateController(IServiceProvider services, ControllerActionDescriptor descriptor,
            ActionContext actionContext)
        {
            var controllerType = descriptor.ControllerTypeInfo.AsType();

            //task 6.4 called AddControllersAsServices() and registered every controller in the
            //nopCommerce Autofac container, so the container is the right source. ActivatorUtilities
            //is the fallback for a controller that is not registered.
            var controller = services.GetService(controllerType)
                            ?? ActivatorUtilities.CreateInstance(services, controllerType);

            //ASP.NET Core normally populates these through IControllerPropertyActivator, which only
            //runs inside the MVC pipeline. Setting them by hand is what makes PartialView(model),
            //Url.RouteUrl(...) and TempData work inside the invoked action.
            var asController = controller as Controller;
            if (asController != null)
            {
                asController.ControllerContext = new ControllerContext(actionContext);
                asController.ViewData = new ViewDataDictionary(
                    services.GetRequiredService<IModelMetadataProvider>(), actionContext.ModelState);
                asController.TempData = services.GetRequiredService<ITempDataDictionaryFactory>()
                    .GetTempData(actionContext.HttpContext);
                asController.Url = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(actionContext);
            }
            else
            {
                var asBase = controller as ControllerBase;
                if (asBase != null)
                    asBase.ControllerContext = new ControllerContext(actionContext);
            }

            return controller;
        }

        private static IActionResult ExecuteAction(object controller, ControllerActionDescriptor descriptor,
            RouteValueDictionary routeValues)
        {
            var parameters = descriptor.MethodInfo.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                args[i] = BindParameter(parameters[i], routeValues);

            var returned = descriptor.MethodInfo.Invoke(controller, args);

            //support async actions even though no nopCommerce child action is async today
            var task = returned as Task;
            if (task != null)
            {
                task.GetAwaiter().GetResult();
                var resultProperty = task.GetType().GetProperty("Result");
                returned = resultProperty != null ? resultProperty.GetValue(task) : null;
            }

            return returned as IActionResult;
        }

        /// <summary>
        /// Convert a route value to the parameter's CLR type, falling back to the parameter's
        /// declared default and then to <c>default(T)</c>.
        /// </summary>
        /// <remarks>
        /// Deliberately narrow: scalars, strings, enums and their nullable forms. Every
        /// nopCommerce child action parameter is one of those. A complex type gets the
        /// parameter default rather than a half-bound instance, which fails visibly rather than
        /// silently producing wrong output.
        /// </remarks>
        private static object BindParameter(ParameterInfo parameter, RouteValueDictionary routeValues)
        {
            object raw;
            if (!routeValues.TryGetValue(parameter.Name, out raw) || raw == null)
                return parameter.HasDefaultValue ? parameter.DefaultValue : DefaultOf(parameter.ParameterType);

            var target = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            if (target.IsInstanceOfType(raw))
                return raw;

            try
            {
                if (target.IsEnum)
                {
                    var asString = raw as string;
                    return asString != null
                        ? Enum.Parse(target, asString, true)
                        : Enum.ToObject(target, raw);
                }

                if (target == typeof(string))
                    return Convert.ToString(raw, CultureInfo.InvariantCulture);

                if (target == typeof(Guid))
                    return Guid.Parse(Convert.ToString(raw, CultureInfo.InvariantCulture));

                var converter = TypeDescriptor.GetConverter(target);
                if (converter != null && converter.CanConvertFrom(raw.GetType()))
                    return converter.ConvertFrom(null, CultureInfo.InvariantCulture, raw);

                return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            }
            catch
            {
                //an unconvertible value behaves as "not supplied", which is what MVC 5's model
                //binder did (it left the parameter at its default and added a model error)
                return parameter.HasDefaultValue ? parameter.DefaultValue : DefaultOf(parameter.ParameterType);
            }
        }

        private static object DefaultOf(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static IHtmlContent RenderResult(IServiceProvider services, ActionContext actionContext,
            object controller, ControllerActionDescriptor descriptor, IActionResult result)
        {
            if (result == null || result is EmptyResult)
                return HtmlString.Empty;

            //Content("") is how several nopCommerce child actions say "render nothing"
            var contentResult = result as ContentResult;
            if (contentResult != null)
                return new HtmlString(contentResult.Content ?? string.Empty);

            string viewName = null;
            ViewDataDictionary viewData = null;
            var partial = result as PartialViewResult;
            if (partial != null)
            {
                viewName = partial.ViewName;
                viewData = partial.ViewData;
            }
            else
            {
                var view = result as ViewResult;
                if (view != null)
                {
                    viewName = view.ViewName;
                    viewData = view.ViewData;
                }
                else
                {
                    throw new NopException(string.Format(
                        "Html.Action('{0}', '{1}') returned {2}, which cannot be rendered inline in a view. Only ViewResult, PartialViewResult, ContentResult and EmptyResult are supported.",
                        descriptor.ActionName, descriptor.ControllerName, result.GetType().Name));
                }
            }

            //a null ViewName means "same name as the action", exactly as in MVC 5
            if (string.IsNullOrEmpty(viewName))
                viewName = descriptor.ActionName;

            if (viewData == null)
            {
                var asController = controller as Controller;
                viewData = asController != null
                    ? asController.ViewData
                    : new ViewDataDictionary(services.GetRequiredService<IModelMetadataProvider>(),
                        actionContext.ModelState);
            }

            var viewEngine = services.GetRequiredService<ICompositeViewEngine>();
            //isMainPage: false is what makes _ViewStart NOT run, matching a partial render
            var viewEngineResult = viewEngine.FindView(actionContext, viewName, false);
            if (!viewEngineResult.Success)
                throw new NopException(string.Format(
                    "Html.Action('{0}', '{1}'): the view '{2}' was not found. Searched: {3}",
                    descriptor.ActionName, descriptor.ControllerName, viewName,
                    string.Join(", ", viewEngineResult.SearchedLocations ?? new List<string>())));

            var tempDataFactory = services.GetRequiredService<ITempDataDictionaryFactory>();
            var htmlHelperOptions = services.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<MvcViewOptions>>().Value.HtmlHelperOptions;

            using (var writer = new StringWriter())
            {
                var childViewContext = new ViewContext(
                    actionContext,
                    viewEngineResult.View,
                    viewData,
                    tempDataFactory.GetTempData(actionContext.HttpContext),
                    writer,
                    htmlHelperOptions);

                //sync-over-async - see the class remarks
                viewEngineResult.View.RenderAsync(childViewContext).GetAwaiter().GetResult();

                return new HtmlString(writer.ToString());
            }
        }

        #endregion
    }
}
