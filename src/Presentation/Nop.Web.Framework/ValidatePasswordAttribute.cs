using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Represents filter attribute to validate customer password expiration
    /// </summary>
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

            var actionName = string.Empty;
            if (filterContext.RouteData.Values.ContainsKey("action"))
                actionName = filterContext.RouteData.Values["action"].ToString();
            if (string.IsNullOrEmpty(actionName) || actionName.Equals("ChangePassword", StringComparison.InvariantCultureIgnoreCase))
                return;

            var controllerName = string.Empty;
            if (filterContext.RouteData.Values.ContainsKey("controller"))
                controllerName = filterContext.RouteData.Values["controller"].ToString();
            if (string.IsNullOrEmpty(controllerName) || controllerName.Equals("Customer", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //get current customer
            var customer = filterContext.HttpContext.RequestServices.GetService<IWorkContext>().CurrentCustomer;

            //check password expiration
            if (customer.PasswordIsExpired())
            {
                filterContext.Result = new RedirectToRouteResult("CustomerChangePassword", new RouteValueDictionary());
            }
        }
    }
}
