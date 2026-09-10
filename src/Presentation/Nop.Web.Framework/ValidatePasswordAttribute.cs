using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Represents filter attribute to validate customer password expiration
    /// </summary>
    /// <remarks>
    /// Task 6.2: ported to ASP.NET Core MVC filters.
    /// <list type="bullet">
    /// <item><c>filterContext.ActionDescriptor.ActionName</c> -&gt; ASP.NET Core's
    /// <c>ActionDescriptor</c> has no <c>ActionName</c>; only the derived
    /// <c>ControllerActionDescriptor</c> does, hence the cast.</item>
    /// <item><c>new UrlHelper(filterContext.RequestContext)</c> -&gt;
    /// <c>IUrlHelperFactory.GetUrlHelper(ActionContext)</c> - <c>ActionExecutingContext</c>
    /// derives from <c>ActionContext</c> in ASP.NET Core, so the filter context is passed
    /// straight through.</item>
    /// <item>the <c>IsChildAction</c> guard is removed (View Components do not execute
    /// action filters).</item>
    /// </list>
    /// Behaviour preserved verbatim, including the pre-existing 3.90 defect on the
    /// controller-name test: <c>filterContext.Controller.ToString()</c> yields the
    /// controller's full type name (e.g. "Nop.Web.Controllers.CustomerController"), which
    /// never equals the literal "Customer", so the CustomerController exemption has never
    /// actually fired. Not silently "fixed" here - see the task 6.2 report.
    /// </remarks>
    public class ValidatePasswordAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Called by the ASP.NET Core MVC framework before the action method executes
        /// </summary>
        /// <param name="filterContext">The filter context</param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null || filterContext.HttpContext.Request == null)
                return;

            var controllerActionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            var actionName = controllerActionDescriptor != null ? controllerActionDescriptor.ActionName : null;
            if (string.IsNullOrEmpty(actionName) || actionName.Equals("ChangePassword", StringComparison.InvariantCultureIgnoreCase))
                return;

            var controllerName = filterContext.Controller != null ? filterContext.Controller.ToString() : null;
            if (string.IsNullOrEmpty(controllerName) || controllerName.Equals("Customer", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //get current customer
            var customer = EngineContext.Current.Resolve<IWorkContext>().CurrentCustomer;

            //check password expiration
            if (customer.PasswordIsExpired())
            {
                var urlHelper = filterContext.HttpContext.RequestServices
                    .GetRequiredService<IUrlHelperFactory>()
                    .GetUrlHelper(filterContext);
                var changePasswordUrl = urlHelper.RouteUrl("CustomerChangePassword");
                filterContext.Result = new RedirectResult(changePasswordUrl);
            }
        }
    }
}
