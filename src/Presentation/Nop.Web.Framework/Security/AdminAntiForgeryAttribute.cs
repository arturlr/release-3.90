using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Data;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AdminAntiForgeryAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        public AdminAntiForgeryAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_ignore)
                return;

            //only POST requests
            if (!string.Equals(context.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (!securitySettings.EnableXsrfProtectionForAdminArea)
                return;

            var antiforgery = context.HttpContext.RequestServices.GetService<IAntiforgery>();
            if (antiforgery != null)
            {
                antiforgery.ValidateRequestAsync(context.HttpContext).GetAwaiter().GetResult();
            }
        }
    }
}
