using System.IO;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Themes;
using Microsoft.AspNetCore.Mvc.Razor;


namespace Nop.Web.Framework.ViewEngines.Razor
{
    /// <summary>
    /// Web view page - ASP.NET Core Razor page base class
    /// </summary>
    /// <typeparam name="TModel">Model</typeparam>
    public abstract class WebViewPage<TModel> : RazorPage<TModel>
    {
        private ILocalizationService _localizationService;
        private Localizer _localizer;

        /// <summary>
        /// Gets the HttpRequest for the current view
        /// </summary>
        public Microsoft.AspNetCore.Http.HttpRequest Request => ViewContext?.HttpContext?.Request;

        /// <summary>
        /// Provides access to HttpContext.Current equivalent
        /// </summary>
        public Microsoft.AspNetCore.Http.HttpContext HttpContext => ViewContext?.HttpContext;

        /// <summary>
        /// Get a localized resources
        /// </summary>
        public Localizer T
        {
            get
            {
                if (_localizer == null)
                {
                    //default localizer
                    _localizer = (format, args) =>
                                     {
                                         var resFormat = _localizationService?.GetResource(format);
                                         if (string.IsNullOrEmpty(resFormat))
                                         {
                                             return new LocalizedString(format);
                                         }
                                         return
                                             new LocalizedString((args == null || args.Length == 0)
                                                                     ? resFormat
                                                                     : string.Format(resFormat, args));
                                     };
                }
                return _localizer;
            }
        }

        public override void BeginContext(int position, int length, bool isLiteral)
        {
            // no-op - for compatibility
        }

        public override void EndContext()
        {
            // no-op - for compatibility
        }

        public override void EnsureRenderedBodyOrSections()
        {
            // no-op - for compatibility
        }

        public override async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                _localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Return a value indicating whether the working language and theme support RTL (right-to-left)
        /// </summary>
        /// <returns></returns>
        public bool ShouldUseRtlTheme()
        {
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var supportRtl = workContext.WorkingLanguage.Rtl;
            if (supportRtl)
            {
                //ensure that the active theme also supports it
                var themeProvider = EngineContext.Current.Resolve<IThemeProvider>();
                var themeContext = EngineContext.Current.Resolve<IThemeContext>();
                supportRtl = themeProvider.GetThemeConfiguration(themeContext.WorkingThemeName).SupportRtl;
            }
            return supportRtl;
        }
    }

    /// <summary>
    /// Web view page
    /// </summary>
    public abstract class WebViewPage : WebViewPage<dynamic>
    {
    }
}
