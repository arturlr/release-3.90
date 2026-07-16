using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Themes;

namespace Nop.Web.Framework.ViewEngines.Razor
{
    /// <summary>
    /// Extension methods for Razor views that replicate the old WebViewPage functionality.
    /// These are available in views via @using Nop.Web.Framework.ViewEngines.Razor in _ViewImports.
    /// </summary>
    public static class RazorPageExtensions
    {
        public static bool ShouldUseRtlTheme(this RazorPageBase page)
        {
            try
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
            catch
            {
                return false;
            }
        }

        public static LocalizedString T(this RazorPageBase page, string format, params object[] args)
        {
            try
            {
                if (DataSettingsHelper.DatabaseIsInstalled())
                {
                    var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
                    var resFormat = localizationService.GetResource(format);
                    if (string.IsNullOrEmpty(resFormat))
                        return new LocalizedString(format);
                    return new LocalizedString((args == null || args.Length == 0)
                        ? resFormat
                        : string.Format(resFormat, args));
                }
            }
            catch { }
            return new LocalizedString(format);
        }
    }
}
