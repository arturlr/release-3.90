using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Admin.Validators.Settings;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Settings
{
    public partial class ReturnRequestReasonModel : BaseNopEntityModel, ILocalizedModel<ReturnRequestReasonLocalizedModel>
    {
        public ReturnRequestReasonModel()
        {
            Locales = new List<ReturnRequestReasonLocalizedModel>();
        }

        [NopResourceDisplayName("Admin.Configuration.Settings.Order.ReturnRequestReasons.Name")]
        
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Order.ReturnRequestReasons.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<ReturnRequestReasonLocalizedModel> Locales { get; set; }
    }

    public partial class ReturnRequestReasonLocalizedModel : ILocalizedModelLocal
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Order.ReturnRequestReasons.Name")]
        
        public string Name { get; set; }

    }
}