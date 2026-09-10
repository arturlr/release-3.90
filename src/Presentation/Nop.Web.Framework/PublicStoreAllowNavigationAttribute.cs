using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Security;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC filters.
    /// </summary>
    /// <remarks>
    /// SECURITY-RELEVANT MAPPING - <c>System.Web.Mvc.HttpUnauthorizedResult</c> -&gt;
    /// <see cref="ChallengeResult"/>.
    /// The deny decision itself is unchanged: the permission test is identical and access is
    /// still refused whenever <see cref="StandardPermissionProvider.PublicStoreAllowNavigation"/>
    /// is not granted. Only the HTTP shape of the refusal is mapped.
    /// In 3.90 <c>HttpUnauthorizedResult</c> emitted a bare 401 which
    /// <c>FormsAuthenticationModule</c> then rewrote into a 302 to the configured login URL on
    /// the way out. ASP.NET Core has no such outbound module: a bare
    /// <c>UnauthorizedResult</c> would return 401 with no redirect, changing observable
    /// behaviour, whereas <see cref="ChallengeResult"/> hands the refusal to the registered
    /// authentication handler, which is what reproduces the 3.90 redirect-to-login.
    /// Runtime deferral: until task 6.4/7.2 registers the cookie authentication handler
    /// (deferral 7.13), <see cref="ChallengeResult"/> throws
    /// <c>InvalidOperationException</c> instead of redirecting. That is a loud failure, not a
    /// bypass - the request is still refused - and it was chosen over a silent 401 precisely
    /// so the missing host wiring cannot go unnoticed.
    /// </remarks>
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

            HttpRequest request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            var controllerActionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            string actionName = controllerActionDescriptor != null ? controllerActionDescriptor.ActionName : null;
            if (String.IsNullOrEmpty(actionName))
                return;

            string controllerName = filterContext.Controller != null ? filterContext.Controller.ToString() : null;
            if (String.IsNullOrEmpty(controllerName))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;
            
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            var publicStoreAllowNavigation = permissionService.Authorize(StandardPermissionProvider.PublicStoreAllowNavigation);
            if (publicStoreAllowNavigation)
                return;

            filterContext.Result = new ChallengeResult();
        }
    }
}
