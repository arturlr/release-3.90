using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Themes
{
    /// <summary>
    /// Contributes nopCommerce's per-theme view locations to the ASP.NET Core Razor view engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type REPLACES <c>ThemeableRazorViewEngine</c> and
    /// <c>ThemeableVirtualPathProviderViewEngine</c>, both deleted by task 6.3. Those classes
    /// derived from <c>System.Web.Mvc.RazorViewEngine</c> /
    /// <c>VirtualPathProviderViewEngine</c>, neither of which has an ASP.NET Core counterpart:
    /// in ASP.NET Core the view engine is a sealed-by-convention framework service and view
    /// location is customised by contributing an <see cref="IViewLocationExpander"/> to
    /// <c>RazorViewEngineOptions.ViewLocationExpanders</c>, not by subclassing.
    /// </para>
    /// <para>
    /// Placeholder mapping differs from 3.90 and matters:
    /// MVC 5's nopCommerce formats used <c>{0}</c>=view, <c>{1}</c>=controller, <c>{2}</c>=theme
    /// for non-area formats and <c>{2}</c>=area, <c>{3}</c>=theme for area formats. ASP.NET Core
    /// fixes <c>{0}</c>=view, <c>{1}</c>=controller, <c>{2}</c>=area, so the theme name cannot be
    /// a positional placeholder — it is substituted into the format string here, at expansion
    /// time, which is the documented pattern for expanders.
    /// </para>
    /// <para>
    /// The theme name is written into <see cref="ViewLocationExpanderContext.Values"/> by
    /// <see cref="PopulateValues"/>. That is REQUIRED, not cosmetic: those values form part of
    /// the view-lookup cache key, so without it the first-resolved theme's view path would be
    /// cached and served to every other store/theme.
    /// </para>
    /// <para>
    /// Registration is host-side and is NOT done by this project — see runtime deferrals. It must
    /// be added by task 6.4/7.2:
    /// <code>
    /// services.Configure&lt;RazorViewEngineOptions&gt;(options =&gt;
    ///     options.ViewLocationExpanders.Add(new ThemeableViewLocationExpander()));
    /// </code>
    /// </para>
    /// </remarks>
    public partial class ThemeableViewLocationExpander : IViewLocationExpander
    {
        #region Constants

        /// <summary>
        /// Key under which the active theme name is stored in
        /// <see cref="ViewLocationExpanderContext.Values"/> (and therefore in the view-lookup
        /// cache key).
        /// </summary>
        public const string ThemeKey = "nop.themename";

        /// <summary>
        /// Name of the nopCommerce administration area.
        /// </summary>
        private const string AdminAreaName = "admin";

        #endregion

        #region Location formats

        //3.90 equivalents: ViewLocationFormats / PartialViewLocationFormats / MasterLocationFormats
        //of ThemeableRazorViewEngine. ASP.NET Core resolves views, partials and layouts through the
        //same location list, so the three arrays collapse into one. "{theme}" is substituted with
        //the working theme name; "{0}" = view name, "{1}" = controller name.
        private static readonly string[] ThemeableViewLocationFormats =
        {
            //themes
            "/Themes/{theme}/Views/{1}/{0}.cshtml",
            "/Themes/{theme}/Views/Shared/{0}.cshtml",

            //default
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml",

            //=================================================================================
            //TASK 10.x - RESTORED, REPOINTED. THIS PAIR IS 3.90's LAST TWO NON-AREA ENTRIES
            //AND REMOVING THEM WAS A REAL, MEASURED REGRESSION. DO NOT DELETE THEM AGAIN.
            //=================================================================================
            //3.90's ViewLocationFormats ended with
            //    "~/Administration/Views/{1}/{0}.cshtml"
            //    "~/Administration/Views/Shared/{0}.cshtml"
            //i.e. the admin views were reachable from a NON-AREA lookup, in that order.
            //
            //Task 8.2 REMOVED the pair, correctly observing that the /Administration/ paths could
            //never match a compiled identifier (deferral 8.1-4) - but treating them as therefore
            //pointless. They were not: the PATHS were dead, the ROLE was live. Task 8.2 could not
            //see it because nothing outside the Admin area rendered an admin view, and the first
            //thing that does is a plugin - nopCommerce plugin admin controllers are NOT in the
            //Admin area (3.90 routes them at Plugins/<Name>/<Action>, no area).
            //
            //MEASURED, on an installed store, with the first migrated plugin (task 10.2):
            //Nop.Admin's Areas/Admin/Views/Shared/_AdminPopupLayout.cshtml - the layout every
            //admin popup uses, including six plugins' - line 69 renders
            //    @await Html.PartialAsync("Notifications")
            //by BARE NAME, so it goes through these location formats with the CURRENT request's
            //controller and area. For a plugin controller the area is empty, so the area formats
            //are never consulted, and the lookup failed:
            //    InvalidOperationException: The partial view 'Notifications' was not found. The
            //    following locations were searched: /Themes/DefaultClean/Views/
            //    DiscountRulesHasOneProduct/Notifications.cshtml, /Themes/DefaultClean/Views/
            //    Shared/Notifications.cshtml, /Views/DiscountRulesHasOneProduct/..., ...
            //-> HTTP 500 on GET /Plugins/DiscountRulesHasOneProduct/ProductAddPopup.
            //
            //The pair is restored in 3.90's order and 3.90's POSITION (last, after every
            //storefront location), and repointed at the compiled identifiers - the same edit task
            //8.2 made to the area array. Position matters for safety as much as fidelity: any name
            //the storefront could already resolve still resolves first, so no storefront lookup
            //changes. It is deliberately NOT themed, exactly as in 3.90.
            //
            //Guarded by Nop.Web.SmokeTests:
            //  Task_8_2_the_expander_emits_no_Administration_location_deferral_8_1_4 (the paths)
            //  Task_10_x_the_non_area_formats_reach_the_admin_shared_views (this pair)
            //  Deferral_8_2_3_the_HasOneProduct_ProductAddPopup_RENDERS_inside_the_admin_popup_layout
            //See runtime-deferrals.md section 83.
            "/Areas/Admin/Views/{1}/{0}.cshtml",
            "/Areas/Admin/Views/Shared/{0}.cshtml"
        };

        //3.90 equivalents: AreaViewLocationFormats / AreaPartialViewLocationFormats /
        //AreaMasterLocationFormats. "{2}" = area name.
        private static readonly string[] ThemeableAreaViewLocationFormats =
        {
            //themes
            "/Areas/{2}/Themes/{theme}/Views/{1}/{0}.cshtml",
            "/Areas/{2}/Themes/{theme}/Views/Shared/{0}.cshtml",

            //default
            "/Areas/{2}/Views/{1}/{0}.cshtml",
            "/Areas/{2}/Views/Shared/{0}.cshtml"
        };

        //3.90's "little hack to get nop's admin area to be in /Administration/ instead of
        ///Nop/Admin/ or Areas/Admin/". GetPath() did:
        //    newLocations.Insert(0, "~/Administration/Views/{1}/{0}.cshtml");
        //    newLocations.Insert(0, "~/Administration/Views/Shared/{0}.cshtml");
        //i.e. two Insert(0, ...) calls, so the Shared entry ends up FIRST and the
        //controller-specific entry second.
        //
        //TASK 8.2 (runtime deferral 8.1-4): the /Administration/ PATHS are gone - the admin
        //views now live at, and compile as, /Areas/Admin/Views/... - but the ORDERING QUIRK IS
        //PRESERVED DELIBERATELY AND IS THE ONLY REASON THIS ARRAY STILL EXISTS. ASP.NET Core's
        //own area formats (and ThemeableAreaViewLocationFormats below) order these the other way
        //round: controller-specific first, Shared second. Prepending the pair in 3.90's order,
        //for the Admin area only, means a same-named Shared view still SHADOWS the
        //controller-specific one, exactly as it did in 3.90. Removing this array would compile,
        //render, and silently change which view wins.
        //
        //Also note the position: in 3.90 these two entries came BEFORE the themed area formats,
        //so admin views were never themeable. That is preserved too.
        private static readonly string[] AdminAreaSharedFirstLocationFormats =
        {
            "/Areas/{2}/Views/Shared/{0}.cshtml",
            "/Areas/{2}/Views/{1}/{0}.cshtml"
        };

        #endregion

        #region Utilities

        /// <summary>
        /// Get the working theme name.
        /// </summary>
        /// <remarks>
        /// Same seam as 3.90's <c>GetCurrentTheme()</c>: resolved through
        /// <see cref="EngineContext"/> rather than injected, because the expander is constructed
        /// during host configuration, before the container exists. Failures are swallowed so a
        /// not-yet-installed store (no settings, no theme) still resolves its views from the
        /// default, non-themed locations instead of throwing inside view lookup.
        /// </remarks>
        /// <returns>Theme system name, or <c>null</c> when it cannot be determined</returns>
        protected virtual string GetCurrentTheme()
        {
            try
            {
                return EngineContext.Current?.Resolve<IThemeContext>()?.WorkingThemeName;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Publish the working theme name so it becomes part of the view-lookup cache key.
        /// </summary>
        /// <param name="context">View location expander context</param>
        public virtual void PopulateValues(ViewLocationExpanderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //an empty string (not null) is used when there is no theme: null values are not
            //permitted in the cache-key dictionary
            context.Values[ThemeKey] = GetCurrentTheme() ?? string.Empty;
        }

        /// <summary>
        /// Prepend nopCommerce's themed/administration view locations to the framework defaults.
        /// </summary>
        /// <param name="context">View location expander context</param>
        /// <param name="viewLocations">Location formats already supplied by the view engine</param>
        /// <returns>Location formats to search, in order</returns>
        public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (viewLocations == null)
                throw new ArgumentNullException(nameof(viewLocations));

            context.Values.TryGetValue(ThemeKey, out var theme);

            var usingAreas = !string.IsNullOrEmpty(context.AreaName);

            var locations = new List<string>();

            if (usingAreas)
            {
                //the 3.90 Shared-before-controller ordering quirk, applied only to the Admin
                //area - as in 3.90, and see the remarks on the array itself
                if (context.AreaName.Equals(AdminAreaName, StringComparison.OrdinalIgnoreCase))
                    locations.AddRange(AdminAreaSharedFirstLocationFormats);

                locations.AddRange(ThemeableAreaViewLocationFormats);
            }

            locations.AddRange(ThemeableViewLocationFormats);

            //drop the themed locations when there is no theme: "/Themes//Views/..." would never
            //match a file and only costs a probe per lookup
            var expanded = locations
                .Where(location => !string.IsNullOrEmpty(theme) || !location.Contains("{theme}"))
                .Select(location => location.Replace("{theme}", theme));

            //framework defaults are kept as a final fallback so views that live only in the
            //conventional ASP.NET Core locations (including anything contributed by a Razor class
            //library or a plugin application part) still resolve
            return expanded.Concat(viewLocations);
        }

        #endregion
    }
}
