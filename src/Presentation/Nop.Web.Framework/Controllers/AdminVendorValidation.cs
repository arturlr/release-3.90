using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Attribute to ensure that users with "Vendor" customer role has appropriate vendor account associated (and active)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class AdminVendorValidation : Attribute, IAuthorizationFilter
    {
        private readonly bool _ignore;

        public AdminVendorValidation(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            if (_ignore)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var workContext = filterContext.HttpContext.RequestServices.GetService<IWorkContext>();
            if (!workContext.CurrentCustomer.IsVendor())
                return;

            //ensure that this user has active vendor record associated
            if (workContext.CurrentVendor == null)
                filterContext.Result = new UnauthorizedResult();
        }
    }
}
