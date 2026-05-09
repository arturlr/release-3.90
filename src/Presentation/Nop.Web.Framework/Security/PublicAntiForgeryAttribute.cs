using System;
using Nop.Core.Data;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;


namespace Nop.Web.Framework.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class PublicAntiForgeryAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        /// <summary>
        /// Anti-forgery security attribute
        /// </summary>
        /// <param name="ignore">Pass false in order to ignore this security validation</param>
        public PublicAntiForgeryAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException(nameof(filterContext));

            if (_ignore)
                return;

            //only POST requests
            if (!String.Equals(filterContext.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;
            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (!securitySettings.EnableXsrfProtectionForPublicStore)
                return;
            
            var antiforgery = filterContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            antiforgery.ValidateRequestAsync(filterContext.HttpContext).GetAwaiter().GetResult();
        }
    }
}
