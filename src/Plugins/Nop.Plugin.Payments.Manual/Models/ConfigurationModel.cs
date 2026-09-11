using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Manual.Models
{
    /// <remarks>
    /// Task 12.2: <c>using System.Web.Mvc;</c> -&gt;
    /// <c>using Microsoft.AspNetCore.Mvc.Rendering;</c>. <see cref="SelectList"/> exists in
    /// ASP.NET Core with the same shape but lives in the Rendering namespace, not in the MVC
    /// root - the same split task 10.1 recorded for <c>SelectListItem</c>. No attribute on this
    /// model needed removing (it carries no <c>[AllowHtml]</c>).
    /// </remarks>
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Manual.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Manual.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        public int TransactModeId { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Manual.Fields.TransactMode")]
        public SelectList TransactModeValues { get; set; }
        public bool TransactModeId_OverrideForStore { get; set; }
    }
}