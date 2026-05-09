using Nop.Admin.Validators.Templates;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Templates
{
    public partial class CategoryTemplateModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.System.Templates.Category.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.System.Templates.Category.ViewPath")]

        public string ViewPath { get; set; }

        [NopResourceDisplayName("Admin.System.Templates.Category.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}