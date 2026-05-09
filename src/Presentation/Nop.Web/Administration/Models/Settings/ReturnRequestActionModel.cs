using System.Collections.Generic;
using Nop.Admin.Validators.Settings;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Settings
{
    public partial class ReturnRequestActionModel : BaseNopEntityModel, ILocalizedModel<ReturnRequestActionLocalizedModel>
    {
        public ReturnRequestActionModel()
        {
            Locales = new List<ReturnRequestActionLocalizedModel>();
        }

        [NopResourceDisplayName("Admin.Configuration.Settings.Order.ReturnRequestActions.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Order.ReturnRequestActions.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<ReturnRequestActionLocalizedModel> Locales { get; set; }
    }

    public partial class ReturnRequestActionLocalizedModel : ILocalizedModelLocal
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Order.ReturnRequestActions.Name")]

        public string Name { get; set; }

    }
}