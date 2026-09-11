using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Widgets.GoogleAnalytics.Models
{
    // Task 15.2:
    //   using System.Web.Mvc; -> using Microsoft.AspNetCore.Mvc.Rendering; (for SelectListItem).
    //   [AllowHtml] DELETED from GoogleId/TrackingScript/EcommerceScript/EcommerceDetailScript.
    //     System.Web.Mvc.AllowHtmlAttribute has NO ASP.NET Core counterpart - ASP.NET Core does
    //     not request-validate input, so there is nothing to opt out of. This is the same
    //     recorded, security-relevant relaxation as deferral 7.3-3 (the tracking-script fields
    //     legitimately hold <script> markup, which is exactly why 3.90 marked them [AllowHtml]).
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        public ConfigurationModel()
        {
            AvailableZones = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.ContentManagement.Widgets.ChooseZone")]
        public string ZoneId { get; set; }
        public IList<SelectListItem> AvailableZones { get; set; }
        public bool ZoneId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.GoogleAnalytics.GoogleId")]
        public string GoogleId { get; set; }
        public bool GoogleId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.GoogleAnalytics.TrackingScript")]
        //tracking code
        public string TrackingScript { get; set; }
        public bool TrackingScript_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.GoogleAnalytics.EcommerceScript")]
        public string EcommerceScript { get; set; }
        public bool EcommerceScript_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.GoogleAnalytics.EcommerceDetailScript")]
        public string EcommerceDetailScript { get; set; }
        public bool EcommerceDetailScript_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.GoogleAnalytics.IncludingTax")]
        public bool IncludingTax { get; set; }
        public bool IncludingTax_OverrideForStore { get; set; }

    }
}
