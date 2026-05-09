using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Settings
{
    public partial class SortOptionModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Configuration.Settings.Catalog.SortOptions.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Catalog.SortOptions.IsActive")]
        public bool IsActive { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Catalog.SortOptions.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}