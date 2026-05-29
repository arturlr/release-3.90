using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Represents filter attribute to validate customer password expiration
    /// </summary>
    public class ValidatePasswordAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                return;

            var actionName = context.RouteData.Values["action"]?.ToString();
            if (string.IsNullOrEmpty(actionName) || actionName.Equals("ChangePassword", StringComparison.InvariantCultureIgnoreCase))
                return;

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            if (string.IsNullOrEmpty(controllerName) || controllerName.Equals("Customer", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //get current customer
            var customer = EngineContext.Current.Resolve<IWorkContext>().CurrentCustomer;

            //check password expiration
            if (customer.PasswordIsExpired())
            {
                context.Result = new RedirectToRouteResult("CustomerChangePassword", null);
            }
        }
    }
}
