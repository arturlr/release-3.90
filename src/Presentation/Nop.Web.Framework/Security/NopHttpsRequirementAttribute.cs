using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class NopHttpsRequirementAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        public NopHttpsRequirementAttribute(SslRequirement sslRequirement)
        {
            this.SslRequirement = sslRequirement;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (securitySettings.ForceSslForAllPages)
                this.SslRequirement = SslRequirement.Yes;

            switch (this.SslRequirement)
            {
                case SslRequirement.Yes:
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (!currentConnectionSecured)
                        {
                            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                            if (storeContext.CurrentStore.SslEnabled)
                            {
                                string url = webHelper.GetThisPageUrl(true, true);
                                context.Result = new RedirectResult(url, true);
                            }
                        }
                    }
                    break;
                case SslRequirement.No:
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (currentConnectionSecured)
                        {
                            string url = webHelper.GetThisPageUrl(true, false);
                            context.Result = new RedirectResult(url, true);
                        }
                    }
                    break;
                case SslRequirement.NoMatter:
                    break;
                default:
                    throw new NopException("Not supported SslProtected parameter");
            }
        }

        public SslRequirement SslRequirement { get; set; }
    }
}
