using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Infrastructure;
using Nop.Services.Security;

namespace Nop.Web.Framework.Controllers
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
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

        private void HandleUnauthorizedRequest(AuthorizationFilterContext context)
        {
            context.Result = new UnauthorizedResult();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_dontValidate)
                return;

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!this.HasAdminAccess())
                this.HandleUnauthorizedRequest(context);
        }

        public virtual bool HasAdminAccess()
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            bool result = permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel);
            return result;
        }
    }
}
