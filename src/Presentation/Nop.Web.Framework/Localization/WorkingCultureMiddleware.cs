using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Sets the thread culture for the request: the working language's culture in the public
    /// store, and the fixed Telerik-grid culture in the admin area.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 6.4: this is 3.90's <c>Nop.Web/Global.asax.cs</c> <c>SetWorkingCulture()</c>,
    /// which was invoked from <c>Application_AuthenticateRequest</c> with the source comment
    /// <i>"we don't do it in Application_BeginRequest because a user is not authenticated
    /// yet"</i>. That ordering constraint carries over verbatim: this middleware MUST be
    /// registered <b>after</b> <c>UseAuthentication()</c>, because it resolves
    /// <see cref="IWorkContext"/> whose <c>WorkingLanguage</c> depends on the authenticated
    /// customer.
    /// </para>
    /// <para>
    /// <b>Preserved:</b> the not-installed guard, the static-resource guard, the keep-alive
    /// guard, the "URL starts with &lt;store location&gt;admin" test that selects the admin
    /// branch, and <c>CommonHelper.SetTelerikCulture()</c> for it.
    /// </para>
    /// <para>
    /// <b>Changed:</b> <c>Thread.CurrentThread.CurrentCulture</c> /
    /// <c>CurrentUICulture</c> → <c>CultureInfo.CurrentCulture</c> /
    /// <c>CultureInfo.CurrentUICulture</c>. On .NET these are the async-local ambient culture
    /// and are what flows across <c>await</c> points; assigning the thread's culture directly
    /// would be lost the moment the request continued on another thread. (This is also what
    /// <c>CommonHelper.SetTelerikCulture()</c> in Nop.Core already does internally.)
    /// </para>
    /// <para>
    /// This is deliberately NOT ASP.NET Core's <c>UseRequestLocalization()</c>: that resolves
    /// the culture from its own provider chain, whereas nopCommerce's language resolution is
    /// <see cref="IWorkContext.WorkingLanguage"/> (URL SEO code → customer setting → store
    /// default → browser). Reproducing the 3.90 rule exactly is the point.
    /// </para>
    /// </remarks>
    public class WorkingCultureMiddleware
    {
        private readonly RequestDelegate _next;

        public WorkingCultureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            SetWorkingCulture(context);
            await _next(context);
        }

        protected virtual void SetWorkingCulture(HttpContext context)
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var webHelper = EngineContext.Current.Resolve<IWebHelper>();

            //ignore static resources
            if (webHelper.IsStaticResource(context.Request))
                return;

            //keep alive page requested (we ignore it to prevent creation of guest customer records)
            var keepAliveUrl = string.Format("{0}keepalive/index", webHelper.GetStoreLocation());
            var thisPageUrl = webHelper.GetThisPageUrl(false);
            if (thisPageUrl.StartsWith(keepAliveUrl, StringComparison.InvariantCultureIgnoreCase))
                return;

            if (thisPageUrl.StartsWith(string.Format("{0}admin", webHelper.GetStoreLocation()),
                StringComparison.InvariantCultureIgnoreCase))
            {
                //admin area

                //always set culture to 'en-US'
                //we set culture of admin area to 'en-US' because current implementation of Telerik grid 
                //doesn't work well in other cultures
                //e.g., editing decimal value in russian culture
                CommonHelper.SetTelerikCulture();
            }
            else
            {
                //public store
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var culture = new CultureInfo(workContext.WorkingLanguage.LanguageCulture);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }
    }
}
