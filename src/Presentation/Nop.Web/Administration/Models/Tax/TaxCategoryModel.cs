using Nop.Admin.Validators.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Tax
{
    public partial class TaxCategoryModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Configuration.Tax.Categories.Fields.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Tax.Categories.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}