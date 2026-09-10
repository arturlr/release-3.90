using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Data;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC authorization filters.
    /// </summary>
    /// <remarks>
    /// SECURITY-SENSITIVE FILE (XSRF). The gating conditions are unchanged: POST only, database
    /// installed, and <see cref="SecuritySettings.EnableXsrfProtectionForAdminArea"/> enabled.
    /// Only the mechanism of the token check changed.
    /// <list type="bullet">
    /// <item>3.90 instantiated <c>System.Web.Mvc.ValidateAntiForgeryTokenAttribute</c> and called
    /// its <c>OnAuthorization</c> inline. ASP.NET Core's
    /// <c>ValidateAntiForgeryTokenAttribute</c> is an <c>IFilterFactory</c> with no invocable
    /// <c>OnAuthorization</c>, so it cannot be reused that way. The check is now performed by
    /// <see cref="IAntiforgery.ValidateRequestAsync"/> - the very API the framework's own
    /// antiforgery filter uses.</item>
    /// <item><c>IAuthorizationFilter</c> -&gt; <see cref="IAsyncAuthorizationFilter"/>, because
    /// <see cref="IAntiforgery.ValidateRequestAsync"/> is asynchronous and the alternative
    /// (blocking on it) would be a gratuitous sync-over-async in the request pipeline. PUBLIC
    /// SIGNATURE CHANGE: <c>OnAuthorization(AuthorizationContext)</c> -&gt;
    /// <c>OnAuthorizationAsync(AuthorizationFilterContext)</c>. No in-tree subclass exists.</item>
    /// <item>Failure response: MVC5 let <c>HttpAntiForgeryException</c> propagate (a 500).
    /// ASP.NET Core's own filter converts a validation failure into a 400, which is what
    /// <see cref="BadRequestResult"/> reproduces here. Either way the action does not run.</item>
    /// </list>
    /// HOST REQUIREMENT (tasks 6.4 / 7.2): <see cref="IAntiforgery"/> must be registered.
    /// <c>services.AddControllersWithViews()</c> / <c>AddMvc()</c> registers it implicitly; if the
    /// host builds the MVC service graph piecemeal it must call <c>services.AddAntiforgery()</c>.
    /// If it is missing, <c>GetRequiredService</c> throws rather than silently skipping the
    /// check - deliberately fail-closed.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AdminAntiForgeryAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly bool _ignore;

        /// <summary>
        /// Anti-forgery security attribute
        /// </summary>
        /// <param name="ignore">Pass false in order to ignore this security validation</param>
        public AdminAntiForgeryAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            if (_ignore)
                return;

            //only POST requests
            if (!String.Equals(filterContext.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;
            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (!securitySettings.EnableXsrfProtectionForAdminArea)
                return;

            var antiforgery = filterContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(filterContext.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                filterContext.Result = new BadRequestResult();
            }
        }
    }
}
