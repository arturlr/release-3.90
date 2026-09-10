using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;

namespace Nop.Web.Framework.Security.Honeypot
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC authorization filters.
    /// <c>FilterAttribute, IAuthorizationFilter</c> -&gt; <c>Attribute,
    /// Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter</c>; <c>AuthorizationContext</c>
    /// -&gt; <see cref="AuthorizationFilterContext"/>.
    /// <c>Request.Form[name]</c> now requires a <c>HasFormContentType</c> guard: System.Web
    /// returned an empty collection for a non-form request whereas ASP.NET Core throws
    /// <c>InvalidOperationException</c>. The guard preserves the original outcome (no honeypot
    /// value found =&gt; not a bot) rather than turning every GET into a 500.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class HoneypotValidatorAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (securitySettings.HoneypotEnabled)
            {
                var request = filterContext.HttpContext.Request;
                string inputValue = request.HasFormContentType
                    ? (string)request.Form[securitySettings.HoneypotInputName]
                    : null;

                var isBot = !String.IsNullOrWhiteSpace(inputValue);
                if (isBot)
                {
                    var logger = EngineContext.Current.Resolve<ILogger>();
                    logger.Warning("A bot detected. Honeypot.");

                    //filterContext.Result = new ChallengeResult();
                    var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                    string url = webHelper.GetThisPageUrl(true);
                    filterContext.Result = new RedirectResult(url);
                }
            }
        }
    }
}
