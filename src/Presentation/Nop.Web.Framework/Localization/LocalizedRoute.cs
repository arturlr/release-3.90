using Microsoft.AspNetCore.Routing;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Provides properties and methods for defining a localized route.
    /// In ASP.NET Core, this is used as a marker and for SEO code caching.
    /// </summary>
    public class LocalizedRoute
    {
        private bool? _seoFriendlyUrlsForLanguagesEnabled;

        public virtual void ClearSeoFriendlyUrlsCachedValue()
        {
            _seoFriendlyUrlsForLanguagesEnabled = null;
        }

        public bool SeoFriendlyUrlsForLanguagesEnabled
        {
            get
            {
                if (!_seoFriendlyUrlsForLanguagesEnabled.HasValue)
                    _seoFriendlyUrlsForLanguagesEnabled = EngineContext.Current.Resolve<LocalizationSettings>().SeoFriendlyUrlsForLanguagesEnabled;
                return _seoFriendlyUrlsForLanguagesEnabled.Value;
            }
        }
    }
}
