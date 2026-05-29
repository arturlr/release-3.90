using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Attribute to ensure that users with "Vendor" customer role has appropriate vendor account associated (and active)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class AdminVendorValidation : ActionFilterAttribute, IAuthorizationFilter
    {
        private readonly bool _ignore;

        public AdminVendorValidation(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_ignore)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            if (!workContext.CurrentCustomer.IsVendor())
                return;

            if (workContext.CurrentVendor == null)
                context.Result = new UnauthorizedResult();
        }
    }
}
