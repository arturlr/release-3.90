using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor;


namespace Nop.Web.Framework.Themes
{
    /// <summary>
    /// Themeable Razor view engine configuration.
    /// In ASP.NET Core, the Razor view engine uses IViewLocationExpander instead of custom view engine classes.
    /// This class provides the view location format strings for configuration purposes.
    /// Use ThemeableViewLocationExpander with RazorViewEngineOptions to register these locations.
    /// </summary>
    public class ThemeableRazorViewEngine
    {
        public string[] AreaViewLocationFormats { get; set; }
        public string[] AreaPartialViewLocationFormats { get; set; }
        public string[] ViewLocationFormats { get; set; }
        public string[] PartialViewLocationFormats { get; set; }
        public string[] FileExtensions { get; set; }

        public ThemeableRazorViewEngine()
        {
            AreaViewLocationFormats = new[]
            {
                //themes
                "/Areas/{2}/Themes/{3}/Views/{1}/{0}.cshtml",
                "/Areas/{2}/Themes/{3}/Views/Shared/{0}.cshtml",
                
                //default
                "/Areas/{2}/Views/{1}/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
            };

            AreaPartialViewLocationFormats = new[]
            {
                //themes
                "/Areas/{2}/Themes/{3}/Views/{1}/{0}.cshtml",
                "/Areas/{2}/Themes/{3}/Views/Shared/{0}.cshtml",
                
                //default
                "/Areas/{2}/Views/{1}/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml"
            };

            ViewLocationFormats = new[]
            {
                //themes
                "/Themes/{2}/Views/{1}/{0}.cshtml", 
                "/Themes/{2}/Views/Shared/{0}.cshtml",

                //default
                "/Views/{1}/{0}.cshtml", 
                "/Views/Shared/{0}.cshtml",

                //Admin
                "/Administration/Views/{1}/{0}.cshtml",
                "/Administration/Views/Shared/{0}.cshtml",
            };

            PartialViewLocationFormats = new[]
            {
                //themes
                "/Themes/{2}/Views/{1}/{0}.cshtml",
                "/Themes/{2}/Views/Shared/{0}.cshtml",

                //default
                "/Views/{1}/{0}.cshtml", 
                "/Views/Shared/{0}.cshtml", 

                //Admin
                "/Administration/Views/{1}/{0}.cshtml",
                "/Administration/Views/Shared/{0}.cshtml",
            };

            FileExtensions = new[] { "cshtml" };
        }
    }
}
