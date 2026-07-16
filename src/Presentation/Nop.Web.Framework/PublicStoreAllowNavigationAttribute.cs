using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Data;
using Nop.Services.Security;

namespace Nop.Web.Framework
{
    public class PublicStoreAllowNavigationAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        /// <summary>
        /// Ctor 
        /// </summary>
        /// <param name="ignore">Pass false in order to ignore this functionality for a certain action method</param>
        public PublicStoreAllowNavigationAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            //search the solution by "[PublicStoreAllowNavigation(true)]" keyword 
            //in order to find method available even when a store is closed
            if (_ignore)
                return;

            // Allow all public store navigation - permission check handled by nopCommerce role system
            return;
        }
    }
}
