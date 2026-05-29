using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain;
using Nop.Core.Infrastructure;
using Nop.Services.Security;
using Nop.Services.Topics;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Store closed attribute
    /// </summary>
    public class StoreClosedAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        public StoreClosedAttribute(bool ignore = false)
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

            var storeInformationSettings = EngineContext.Current.Resolve<StoreInformationSettings>();
            if (!storeInformationSettings.StoreClosed)
                return;

            //access to a closed store?
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (permissionService.Authorize(StandardPermissionProvider.AccessClosedStore))
                return;

            context.Result = new RedirectToRouteResult("StoreClosed", null);
        }
    }
}
