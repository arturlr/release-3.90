using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Data;
using Nop.Core.Domain.Security;

namespace Nop.Web.Framework.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class PublicAntiForgeryAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        public PublicAntiForgeryAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            if (_ignore)
                return;

            if (!String.Equals(filterContext.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var securitySettings = filterContext.HttpContext.RequestServices.GetService<SecuritySettings>();
            if (!securitySettings.EnableXsrfProtectionForPublicStore)
                return;

            var antiforgery = filterContext.HttpContext.RequestServices.GetService<IAntiforgery>();
            antiforgery.ValidateRequestAsync(filterContext.HttpContext).GetAwaiter().GetResult();
        }
    }
}
