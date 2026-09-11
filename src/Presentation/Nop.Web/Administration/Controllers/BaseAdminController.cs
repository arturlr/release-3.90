using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Converters;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Admin.Controllers
{
    //TASK 8.2 - [Area("Admin")] replaces 3.90's AdminAreaRegistration (deleted). It is declared
    //ONCE, here, rather than on 54 files: Microsoft.AspNetCore.Mvc.AreaAttribute derives from
    //RouteValueAttribute, which is declared [AttributeUsage(..., Inherited = true)], so every
    //controller that derives from this one inherits it. All 54 concrete admin controllers do
    //(verified by inspection of every `class X : ...` declaration under Controllers/).
    //
    //THE INHERITANCE WAS VERIFIED BY EXECUTION, not assumed: a throwaway .NET 10 probe built a
    //controller deriving from an abstract base carrying [Area("Admin")], resolved
    //IActionDescriptorCollectionProvider, and asserted the concrete controller's
    //ActionDescriptor.RouteValues["area"] == "Admin". See runtime-deferrals.md section 50.
    //
    //Two consequences worth knowing:
    //  * the area value is what makes the admin Razor views resolvable at all - the view-location
    //    expander only contributes /Areas/{2}/... locations for an AREA lookup, so a controller
    //    that somehow does not inherit this attribute will fail view lookup, not routing.
    //  * a NEW admin controller that does not derive from BaseAdminController needs its own
    //    [Area("Admin")].
    //
    //TASK 8.3 - the five class-level filters below are UNCHANGED. All five were ported to
    //ASP.NET Core authorization/action filters by task 6.2 in Nop.Web.Framework, and
    //AdminAuthorizeAttribute in particular records why it is NOT expressed as a policy-based
    //[Authorize]: it performs no principal or role test at all, it queries nopCommerce's
    //database-backed IPermissionService keyed off IWorkContext.CurrentCustomer. Expressing that
    //as a named policy would require the HOST to register the policy, which a class library
    //cannot do, and would fail OPEN if a host forgot. See AdminAuthorizeAttribute's remarks.
    //Verified for this task: [AdminAuthorize] here is the ONLY authorization attribute anywhere
    //under Administration/ - no admin controller or action carries [Authorize] or a custom
    //AuthorizeAttribute - so there was no further authorization surface for 8.3 to port.
    [Area("Admin")]
    [NopHttpsRequirement(SslRequirement.Yes)]
    [AdminValidateIpAddress]
    [AdminAuthorize]
    [AdminAntiForgery]
    [AdminVendorValidation]
    public abstract partial class BaseAdminController : BaseController
    {
        /// <summary>
        /// Put the work context into admin mode before the action runs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TASK 8.3 — REPLACES <c>Initialize(System.Web.Routing.RequestContext)</c>.</b> 3.90
        /// overrode <c>System.Web.Mvc.Controller.Initialize</c>, a controller lifecycle hook that
        /// ran after the controller was constructed and before the action executed, purely to do
        /// <c>EngineContext.Current.Resolve&lt;IWorkContext&gt;().IsAdmin = true</c>.
        /// </para>
        /// <para>
        /// ASP.NET Core has <b>no</b> such hook: <c>ControllerBase</c> has no <c>Initialize</c>,
        /// there is no <c>RequestContext</c>, and <c>ControllerContext</c> is assigned by
        /// property injection rather than through a virtual method. The equivalent seam is the
        /// action filter, and <c>Microsoft.AspNetCore.Mvc.Controller</c> already implements
        /// <see cref="IActionFilter"/>, so <see cref="OnActionExecuting"/> is invoked for every
        /// action on every derived controller with no registration required.
        /// </para>
        /// <para>
        /// <b>Ordering, and why it is still correct.</b> Controller-implemented action filters run
        /// at order <c>int.MinValue + 10</c>, i.e. before user filters but <i>after</i>
        /// authorization filters. So <c>IsAdmin</c> is now set slightly later than in 3.90:
        /// <c>[AdminAuthorize]</c>, <c>[AdminValidateIpAddress]</c> and
        /// <c>[AdminVendorValidation]</c> see <c>IsAdmin == false</c> where 3.90 set it in
        /// <c>Initialize</c>. Verified harmless: none of the three reads <c>IWorkContext.IsAdmin</c>
        /// — they read <c>CurrentCustomer</c>, <c>CurrentVendor</c> and
        /// <c>IPermissionService</c>. <c>IsAdmin</c> is consumed by
        /// <c>WebWorkContext.CurrentCustomer</c>/<c>WorkingLanguage</c> and by the admin views,
        /// all of which run after the action filter pipeline has begun.
        /// </para>
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //set work context to admin mode
            EngineContext.Current.Resolve<IWorkContext>().IsAdmin = true;

            base.OnActionExecuting(context);
        }

        //TASK 8.3 - THE OnException OVERRIDE WAS REMOVED, and this is a deliberate, recorded
        //behaviour change rather than an omission.
        //
        //3.90 overrode System.Web.Mvc.Controller.OnException(ExceptionContext) to call
        //LogException(filterContext.Exception) and then base.OnException(...), whose default
        //implementation is a no-op. So its only effect was to LOG the exception; it never marked
        //it handled, so the exception went on to reach Global.asax's Application_Error, which
        //called LogException on it AGAIN. 3.90 therefore wrote two Log rows for every admin
        //exception.
        //
        //ASP.NET Core's Controller has no OnException: it implements IActionFilter,
        //IAsyncActionFilter and IResultFilter, but NOT IExceptionFilter, and MVC's
        //controller-as-filter plumbing (ControllerActionFilter / ControllerResultFilter) has no
        //exception counterpart. Implementing IExceptionFilter on this class would therefore
        //compile and never be invoked - a silent no-op, which is the failure class this
        //migration's deferral register exists to prevent.
        //
        //Nothing is lost. Task 7.2's NopErrorLoggingMiddleware (runtime-deferrals.md section 23)
        //observes every unhandled exception in the pipeline, logs it through the same
        //Nop.Services.Logging path this override used, and rethrows - so an admin exception is
        //still logged, exactly once instead of twice. BaseController.LogException remains
        //available for any admin action that wants to log an exception it handles itself.

        /// <summary>
        /// Access denied view
        /// </summary>
        /// <returns>Access denied view</returns>
        /// <remarks>
        /// Task 8.3: <c>this.Request.RawUrl</c> -&gt;
        /// <see cref="UriHelper.GetEncodedPathAndQuery(HttpRequest)"/>, which yields
        /// <c>PathBase + Path + QueryString</c> — the same shape <c>RawUrl</c> produced. The same
        /// substitution task 6.2 made in <c>WebWorkContext</c> and task 7.3 made in
        /// <c>BackwardCompatibility1XController</c>.
        /// </remarks>
        protected virtual ActionResult AccessDeniedView()
        {
            //return new ChallengeResult();
            return RedirectToAction("AccessDenied", "Security", new { pageUrl = Request.GetEncodedPathAndQuery() });
        }

        /// <summary>
        /// Access denied json data for kendo grid
        /// </summary>
        /// <returns>Access denied json data</returns>
        protected JsonResult AccessDeniedKendoGridJson()
        {
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            
            return ErrorForKendoGridJson(localizationService.GetResource("Admin.AccessDenied.Description"));
        }

        /// <summary>
        /// Save selected TAB name
        /// </summary>
        /// <param name="tabName">Tab name to save; empty to automatically detect it</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <remarks>
        /// Task 8.3: <c>Request.Form["selected-tab-name"]</c> needs a
        /// <see cref="HttpRequest.HasFormContentType"/> guard — System.Web returned an empty
        /// collection for a non-form request where ASP.NET Core throws
        /// <c>InvalidOperationException</c>. The same guard task 6.2 added to five filters. The
        /// indexer also yields <c>StringValues</c> rather than <c>string</c>, hence the explicit
        /// local type.
        /// </remarks>
        protected virtual void SaveSelectedTabName(string tabName = "", bool persistForTheNextRequest = true)
        {
            //keep this method synchronized with
            //"GetSelectedTabName" method of \Nop.Web.Framework\HtmlExtensions.cs
            if (string.IsNullOrEmpty(tabName))
            {
                if (Request.HasFormContentType)
                {
                    string formValue = Request.Form["selected-tab-name"];
                    tabName = formValue;
                }
            }
            
            if (!string.IsNullOrEmpty(tabName))
            {
                const string dataKey = "nop.selected-tab-name";
                if (persistForTheNextRequest)
                {
                    TempData[dataKey] = tabName;
                }
                else
                {
                    ViewData[dataKey] = tabName;
                }
            }
        }

        /// <summary>
        /// Replaces <c>HttpRequest</c>'s <c>this[string]</c> indexer, which does not exist in
        /// ASP.NET Core.
        /// </summary>
        /// <remarks>
        /// System.Web's indexer searched QueryString, then Form, then Cookies, then
        /// ServerVariables, and returned <c>null</c> for a missing key. This searches Query then
        /// Form, which is the only part the admin controllers use, and preserves the null return.
        /// The <see cref="HttpRequest.HasFormContentType"/> guard is required because ASP.NET Core
        /// throws <c>InvalidOperationException</c> on <c>Request.Form</c> for a non-form request
        /// where System.Web returned an empty collection — the same guard task 6.2 added to five
        /// filters and task 7.3 added to <c>Nop.Web</c>'s three valums-uploader actions.
        /// </remarks>
        protected virtual string GetRequestValue(string key)
        {
            var query = Request.Query[key];
            if (!StringValues.IsNullOrEmpty(query))
                return query;

            if (Request.HasFormContentType)
            {
                var form = Request.Form[key];
                if (!StringValues.IsNullOrEmpty(form))
                    return form;
            }

            return null;
        }

        /// <summary>
        /// Replaces <c>HttpRequest.Files</c>, which moved to <c>Request.Form.Files</c>.
        /// </summary>
        /// <remarks>
        /// Guarded for the same reason as <see cref="GetRequestValue"/>: reading
        /// <c>Request.Form</c> on a request that is not a form post throws in ASP.NET Core, where
        /// System.Web's <c>GetRequestFiles()</c> simply came back empty. An empty
        /// <see cref="FormFileCollection"/> reproduces that, so the <c>.Count == 0</c> and
        /// <c>[0] == null</c> tests the admin controllers already perform keep working.
        /// </remarks>
        protected virtual IFormFileCollection GetRequestFiles()
        {
            if (!Request.HasFormContentType)
                return new FormFileCollection();

            return Request.Form.Files;
        }

        /// <summary>
        /// Creates a <see cref="JsonResult"/> that serializes <paramref name="data"/> to JSON.
        /// </summary>
        /// <param name="data">The object graph to serialize.</param>
        /// <remarks>
        /// <para>
        /// <b>TASK 8.3 — the MVC 5 four-argument override collapses onto
        /// <c>ControllerBase.Json(object)</c>.</b> 3.90 overrode
        /// <c>Json(object, string contentType, Encoding contentEncoding, JsonRequestBehavior)</c>,
        /// which is the single overload every other <c>Json(...)</c> overload funnelled through in
        /// MVC 5. ASP.NET Core has no such overload — it has <c>Json(object)</c> and
        /// <c>Json(object, object serializerSettings)</c>, both <c>virtual</c> — and both
        /// <c>ContentEncoding</c> and <c>JsonRequestBehavior</c> are gone from the platform.
        /// Overriding <c>Json(object)</c> preserves the important property: <b>every</b>
        /// <c>return Json(x)</c> in the 54 admin controllers still goes through the ISO-date
        /// converter, with no per-call-site change.
        /// </para>
        /// <para>
        /// <b>Preserved.</b> The <c>AdminAreaSettings.UseIsoDateTimeConverterInJson</c> branch and
        /// the <see cref="IsoDateTimeConverter"/> it selects — the "Json fix issue with dates in
        /// KendoUI grid" — are byte-for-byte 3.90's. <c>ConverterJsonResult</c> (task 6.2)
        /// serializes with Newtonsoft explicitly, so the payload stays PascalCase and
        /// ISO-formatted regardless of host formatter configuration. When the setting is off the
        /// plain <see cref="JsonResult"/> path is taken, which task 6.4 configured with
        /// <c>PropertyNamingPolicy = null</c> — also PascalCase, which is what every Kendo grid
        /// script and <c>DataSourceResult</c>'s <c>Data</c>/<c>Total</c>/<c>Errors</c>/
        /// <c>ExtraData</c> fields require.
        /// </para>
        /// <para>
        /// <b>Dropped, and why each is a no-op.</b>
        /// <list type="bullet">
        /// <item><c>MaxJsonLength = int.MaxValue</c> — this raised
        /// <c>JavaScriptSerializer</c>'s 4 MB cap, which was MVC 5's default. Neither
        /// System.Text.Json nor Newtonsoft imposes any length cap, so there is no ceiling left to
        /// raise. The comment 3.90 attached to it (avoid exceptions when an entity returns a large
        /// text value) is satisfied by the platform.</item>
        /// <item><c>contentType</c> — no caller passed one; <see cref="JsonResult.ContentType"/>
        /// still exists for anything that needs it.</item>
        /// <item><c>contentEncoding</c> — there is no <c>Response.ContentEncoding</c> in
        /// ASP.NET Core; bodies are UTF-8. Recorded by task 6.2 on
        /// <c>ConverterJsonResult</c>.</item>
        /// <item><c>JsonRequestBehavior</c> — ASP.NET Core has no JSON-hijacking guard and no
        /// <c>DenyGet</c> default. Task 6.2 (section 11.23) already recorded this as a
        /// relaxation relative to 3.90; the mitigation is obsolete for modern browsers.</item>
        /// </list>
        /// </para>
        /// </remarks>
        public override JsonResult Json(object data)
        {
            //Json fix issue with dates in KendoUI grid
            //use json with IsoDateTimeConverter
            if (EngineContext.Current.Resolve<AdminAreaSettings>().UseIsoDateTimeConverterInJson)
                return new ConverterJsonResult(new IsoDateTimeConverter()) { Value = data };

            return new JsonResult(data);
        }
    }
}
