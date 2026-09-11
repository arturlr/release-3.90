using System.Collections.Generic;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.CheckMoneyOrder.Models
{
    /// <remarks>
    /// Task 12.1: <c>using System.Web.Mvc;</c> and the two <c>[AllowHtml]</c> attributes on
    /// <c>DescriptionText</c> are DELETED. <c>[AllowHtml]</c> has no ASP.NET Core counterpart -
    /// it existed only to opt OUT of ASP.NET request validation, a framework-level feature that
    /// does not exist at all in ASP.NET Core, so there is nothing left to opt out of. This is
    /// the same SECURITY-RELEVANT RELAXATION deferral 7.3-3 records at 99 Nop.Web sites and
    /// task 8.3 at 395 admin ones: every property now behaves as if it carried [AllowHtml].
    ///
    /// Here the practical exposure is UNCHANGED rather than widened, and it is worth being
    /// explicit about why: DescriptionText is a rich-text setting that was ALREADY marked
    /// [AllowHtml] in 3.90 (it is rendered with @Html.Raw on the checkout page), so it was
    /// never screened. It is administrator-authored - Configure carries [AdminAuthorize] - and
    /// storing HTML in it is the documented purpose of the field.
    /// </remarks>
    public class ConfigurationModel : BaseNopModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
    {
        public ConfigurationModel()
        {
            Locales = new List<ConfigurationLocalizedModel>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.DescriptionText")]
        public string DescriptionText { get; set; }
        public bool DescriptionText_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.ShippableProductRequired")]
        public bool ShippableProductRequired { get; set; }
        public bool ShippableProductRequired_OverrideForStore { get; set; }

        public IList<ConfigurationLocalizedModel> Locales { get; set; }

        #region Nested class

        public partial class ConfigurationLocalizedModel : ILocalizedModelLocal
        {
            public int LanguageId { get; set; }

            [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.DescriptionText")]
            public string DescriptionText { get; set; }
        }

        #endregion

    }
}