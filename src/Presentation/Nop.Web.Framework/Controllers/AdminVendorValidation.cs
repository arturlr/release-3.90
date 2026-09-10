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
    /// <remarks>
    /// Task 6.2: ported to ASP.NET Core MVC authorization filters. SECURITY-SENSITIVE: the deny
    /// condition (customer is in the Vendor role but has no active vendor record) is unchanged.
    /// <c>FilterAttribute, IAuthorizationFilter</c> -&gt; <c>Attribute,
    /// Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter</c>; <c>AuthorizationContext</c>
    /// -&gt; <see cref="AuthorizationFilterContext"/>; <c>HttpUnauthorizedResult</c> -&gt;
    /// <see cref="ChallengeResult"/> (rationale on <c>PublicStoreAllowNavigationAttribute</c>);
    /// the <c>IsChildAction</c> guard is removed (View Components do not execute filters).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
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

            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            if (!workContext.CurrentCustomer.IsVendor())
                return;

            //ensure that this user has active vendor record associated
            if (workContext.CurrentVendor == null)
                filterContext.Result = new ChallengeResult();
        }
    }
}
