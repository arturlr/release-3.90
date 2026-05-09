using System;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;


namespace Nop.Web.Framework
{
    /// <summary>
    /// Represents filter attribute to validate customer password expiration
    /// </summary>
    public class ValidatePasswordAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Called by the ASP.NET MVC framework before the action method executes
        /// </summary>
        /// <param name="filterContext">The filter context</param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null || filterContext.HttpContext.Request == null)
                return;

            var controllerActionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            var actionName = controllerActionDescriptor?.ActionName ?? string.Empty;
            if (string.IsNullOrEmpty(actionName) || actionName.Equals("ChangePassword", StringComparison.InvariantCultureIgnoreCase))
                return;

            var controllerName = controllerActionDescriptor?.ControllerName ?? string.Empty;
            if (string.IsNullOrEmpty(controllerName) || controllerName.Equals("Customer", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //get current customer
            var customer = EngineContext.Current.Resolve<IWorkContext>().CurrentCustomer;

            //check password expiration
            if (customer.PasswordIsExpired())
            {
                var urlHelperFactory = filterContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
                var urlHelper = urlHelperFactory.GetUrlHelper(filterContext);
                var changePasswordUrl = urlHelper.RouteUrl("CustomerChangePassword");
                filterContext.Result = new RedirectResult(changePasswordUrl);
            }
        }
    }
}
