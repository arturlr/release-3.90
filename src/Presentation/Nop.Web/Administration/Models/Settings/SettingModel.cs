using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Admin.Validators.Settings;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Settings
{
    public partial class SettingModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Configuration.Settings.AllSettings.Fields.Name")]
        
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.AllSettings.Fields.Value")]
        
        public string Value { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.AllSettings.Fields.StoreName")]
        public string Store { get; set; }
        public int StoreId { get; set; }
    }
}