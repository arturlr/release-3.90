using System.Threading.Tasks;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Provides properties and methods for defining a localized route.
    /// In ASP.NET Core, this is implemented as an IRouter for compatibility.
    /// </summary>
    public class LocalizedRoute : IRouter
    {
        #region Fields

        private bool? _seoFriendlyUrlsForLanguagesEnabled;
        private readonly IRouter _target;

        #endregion

        #region Constructors

        public LocalizedRoute(IRouter target)
        {
            _target = target;
        }

        #endregion

        #region Methods

        public virtual async Task RouteAsync(RouteContext context)
        {
            if (DataSettingsHelper.DatabaseIsInstalled() && this.SeoFriendlyUrlsForLanguagesEnabled)
            {
                var request = context.HttpContext.Request;
                var path = request.Path.Value;
                var applicationPath = request.PathBase.Value;
                if (string.IsNullOrEmpty(applicationPath))
                    applicationPath = "/";

                if (path.IsLocalizedUrl(applicationPath, false))
                {
                    // Remove language SEO code from the path for routing
                    var newPath = path.RemoveLanguageSeoCodeFromRawUrl(applicationPath);
                    if (string.IsNullOrEmpty(newPath))
                        newPath = "/";
                    
                    // Update the path for downstream routing
                    request.Path = newPath;
                }
            }

            await _target.RouteAsync(context);
        }

        public virtual VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            var data = _target.GetVirtualPath(context);

            if (data != null && DataSettingsHelper.DatabaseIsInstalled() && this.SeoFriendlyUrlsForLanguagesEnabled)
            {
                var request = context.HttpContext.Request;
                var path = request.Path.Value;
                var applicationPath = request.PathBase.Value;
                if (string.IsNullOrEmpty(applicationPath))
                    applicationPath = "/";

                if (path.IsLocalizedUrl(applicationPath, true))
                {
                    var seoCode = path.GetLanguageSeoCodeFromUrl(applicationPath, true);
                    data.VirtualPath = string.Concat(seoCode, "/", data.VirtualPath);
                }
            }
            return data;
        }

        public virtual void ClearSeoFriendlyUrlsCachedValue()
        {
            _seoFriendlyUrlsForLanguagesEnabled = null;
        }

        #endregion

        #region Properties

        protected bool SeoFriendlyUrlsForLanguagesEnabled
        {
            get
            {
                if (!_seoFriendlyUrlsForLanguagesEnabled.HasValue)
                    _seoFriendlyUrlsForLanguagesEnabled = EngineContext.Current.Resolve<LocalizationSettings>().SeoFriendlyUrlsForLanguagesEnabled;

                return _seoFriendlyUrlsForLanguagesEnabled.Value;
            }
        }

        #endregion
    }
}
