using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Services.Security;

namespace Nop.Web.Framework.Controllers
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
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

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (_dontValidate)
                return;

            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            if (!this.HasAdminAccess(filterContext))
                this.HandleUnauthorizedRequest(filterContext);
        }

        public virtual bool HasAdminAccess(AuthorizationFilterContext filterContext)
        {
            var permissionService = filterContext.HttpContext.RequestServices.GetService<IPermissionService>();
            bool result = permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel);
            return result;
        }
    }
}
