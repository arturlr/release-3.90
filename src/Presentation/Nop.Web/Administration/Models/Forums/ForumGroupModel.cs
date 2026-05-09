using System;
using Nop.Admin.Validators.Forums;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Forums
{
    public partial class ForumGroupModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.ContentManagement.Forums.ForumGroup.Fields.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.ForumGroup.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.ForumGroup.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }
    }
}