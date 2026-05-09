using System;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace Nop.Web.Framework.Security.Honeypot
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class HoneypotValidatorAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException(nameof(filterContext));

            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (securitySettings.HoneypotEnabled)
            {
                string inputValue = filterContext.HttpContext.Request.Form[securitySettings.HoneypotInputName];

                var isBot = !String.IsNullOrWhiteSpace(inputValue);
                if (isBot)
                {
                    var logger = EngineContext.Current.Resolve<ILogger>();
                    logger.Warning("A bot detected. Honeypot.");

                    var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                    string url = webHelper.GetThisPageUrl(true);
                    filterContext.Result = new RedirectResult(url);
                }
            }
        }
    }
}
