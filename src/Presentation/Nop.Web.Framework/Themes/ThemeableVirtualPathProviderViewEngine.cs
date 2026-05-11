using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;


namespace Nop.Web.Framework.Themes
{
    /// <summary>
    /// Themeable view location expander for ASP.NET Core Razor view engine.
    /// Replaces the old VirtualPathProviderViewEngine approach from MVC 5.
    /// </summary>
    public class ThemeableViewLocationExpander : IViewLocationExpander
    {
        private const string ThemeKey = "nop.ThemeName";

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            try
            {
                var themeContext = EngineContext.Current.Resolve<IThemeContext>();
                context.Values[ThemeKey] = themeContext.WorkingThemeName;
            }
            catch
            {
                context.Values[ThemeKey] = "DefaultClean";
            }
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            context.Values.TryGetValue(ThemeKey, out string theme);

            if (!string.IsNullOrEmpty(theme))
            {
                // Add theme-specific view locations before the default ones
                var themeLocations = new List<string>
                {
                    $"/Themes/{theme}/Views/{{1}}/{{0}}.cshtml",
                    $"/Themes/{theme}/Views/Shared/{{0}}.cshtml"
                };

                viewLocations = themeLocations.Concat(viewLocations);
            }

            // Add admin area locations
            string areaName = context.AreaName;
            if (!string.IsNullOrEmpty(areaName) && areaName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminLocations = new List<string>
                {
                    "/Administration/Views/{1}/{0}.cshtml",
                    "/Administration/Views/Shared/{0}.cshtml"
                };
                viewLocations = adminLocations.Concat(viewLocations);
            }

            return viewLocations;
        }
    }

    #region Legacy support classes

    /// <summary>
    /// View location helper (retained for compatibility)
    /// </summary>
    public class ViewLocation
    {
        protected readonly string _virtualPathFormatString;

        public ViewLocation(string virtualPathFormatString)
        {
            _virtualPathFormatString = virtualPathFormatString;
        }

        public virtual string Format(string viewName, string controllerName, string areaName, string theme)
        {
            return string.Format(CultureInfo.InvariantCulture, _virtualPathFormatString, viewName, controllerName, theme);
        }
    }

    /// <summary>
    /// Area-aware view location helper (retained for compatibility)
    /// </summary>
    public class AreaAwareViewLocation : ViewLocation
    {
        public AreaAwareViewLocation(string virtualPathFormatString)
            : base(virtualPathFormatString)
        {
        }

        public override string Format(string viewName, string controllerName, string areaName, string theme)
        {
            return string.Format(CultureInfo.InvariantCulture, _virtualPathFormatString, viewName, controllerName, areaName, theme);
        }
    }

    #endregion
}
