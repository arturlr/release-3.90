using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Security;

namespace Nop.Web.Framework
{
    public class PublicStoreAllowNavigationAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        public PublicStoreAllowNavigationAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null || context.HttpContext == null)
                return;

            if (_ignore)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            var publicStoreAllowNavigation = permissionService.Authorize(StandardPermissionProvider.PublicStoreAllowNavigation);
            if (publicStoreAllowNavigation)
                return;

            context.Result = new UnauthorizedResult();
        }
    }
}
