using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Admin.Validators.Templates;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Templates
{
    public partial class TopicTemplateModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.System.Templates.Topic.Name")]
        
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.System.Templates.Topic.ViewPath")]
        
        public string ViewPath { get; set; }

        [NopResourceDisplayName("Admin.System.Templates.Topic.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}