using System;
using System.Linq;
using Nop.Core.Infrastructure;
using Nop.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace Nop.Web.Framework.Controllers
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private readonly bool _dontValidate;


        public AdminAuthorizeAttribute()
            : this(false)
        {
        }

        public AdminAuthorizeAttribute(bool dontValidate)
        {
            this._dontValidate = dontValidate;
        }

        private void HandleUnauthorizedRequest(AuthorizationFilterContext filterContext)
        {
            filterContext.Result = new UnauthorizedResult();
        }

        private bool IsAdminPageRequested(AuthorizationFilterContext filterContext)
        {
            var controllerActionDescriptor = filterContext.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
            if (controllerActionDescriptor == null)
                return false;

            var adminAttributes = controllerActionDescriptor.MethodInfo
                .GetCustomAttributes(typeof(AdminAuthorizeAttribute), true)
                .Concat(controllerActionDescriptor.ControllerTypeInfo
                    .GetCustomAttributes(typeof(AdminAuthorizeAttribute), true))
                .OfType<AdminAuthorizeAttribute>();

            if (adminAttributes != null && adminAttributes.Any())
                return true;
            return false;
        }

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (_dontValidate)
                return;

            if (filterContext == null)
                throw new ArgumentNullException(nameof(filterContext));

            if (IsAdminPageRequested(filterContext))
            {
                if (!this.HasAdminAccess())
                    this.HandleUnauthorizedRequest(filterContext);
            }
        }

        public virtual bool HasAdminAccess()
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            bool result = permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel);
            return result;
        }
    }
}
