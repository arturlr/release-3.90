using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Nop.Web.Framework.Themes
{
    /// <summary>
    /// Replaces ThemeableRazorViewEngine with ASP.NET Core IViewLocationExpander
    /// </summary>
    public class ThemeableViewLocationExpander : IViewLocationExpander
    {
        private const string THEME_KEY = "nop.themename";

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var themeContext = (IThemeContext)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IThemeContext));
            if (themeContext != null)
                context.Values[THEME_KEY] = themeContext.WorkingThemeName;
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            string theme = null;
            if (context.Values.TryGetValue(THEME_KEY, out theme) && !string.IsNullOrEmpty(theme))
            {
                var themeLocations = new List<string>();
                themeLocations.Add("/Themes/" + theme + "/Views/{1}/{0}.cshtml");
                themeLocations.Add("/Themes/" + theme + "/Views/Shared/{0}.cshtml");

                if (context.AreaName != null)
                {
                    themeLocations.Insert(0, "/Areas/{2}/Themes/" + theme + "/Views/{1}/{0}.cshtml");
                    themeLocations.Insert(1, "/Areas/{2}/Themes/" + theme + "/Views/Shared/{0}.cshtml");
                }

                viewLocations = themeLocations.Concat(viewLocations);
            }
            return viewLocations;
        }
    }
}
