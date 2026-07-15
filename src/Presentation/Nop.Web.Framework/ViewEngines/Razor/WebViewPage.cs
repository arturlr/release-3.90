using Microsoft.AspNetCore.Mvc.Razor;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Themes;

namespace Nop.Web.Framework.ViewEngines.Razor
{
    public abstract class WebViewPage<TModel> : RazorPage<TModel>
    {
        private ILocalizationService _localizationService;
        private Localizer _localizer;

        public Localizer T
        {
            get
            {
                if (_localizer == null)
                {
                    if (DataSettingsHelper.DatabaseIsInstalled())
                    {
                        _localizationService = EngineContext.Current.Resolve<ILocalizationService>();
                    }

                    _localizer = (format, args) =>
                    {
                        var resFormat = _localizationService != null
                            ? _localizationService.GetResource(format)
                            : format;
                        if (string.IsNullOrEmpty(resFormat))
                            return new LocalizedString(format);
                        return new LocalizedString((args == null || args.Length == 0)
                            ? resFormat
                            : string.Format(resFormat, args));
                    };
                }
                return _localizer;
            }
        }

        public bool ShouldUseRtlTheme()
        {
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var supportRtl = workContext.WorkingLanguage.Rtl;
            if (supportRtl)
            {
                var themeProvider = EngineContext.Current.Resolve<IThemeProvider>();
                var themeContext = EngineContext.Current.Resolve<IThemeContext>();
                supportRtl = themeProvider.GetThemeConfiguration(themeContext.WorkingThemeName).SupportRtl;
            }
            return supportRtl;
        }
    }

    public abstract class WebViewPage : WebViewPage<dynamic>
    {
    }
}
