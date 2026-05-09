using System;
using System.Collections.Generic;
using Nop.Admin.Validators.Forums;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Forums
{
    public partial class ForumModel : BaseNopEntityModel
    {
        public ForumModel()
        {
            ForumGroups = new List<ForumGroupModel>();
        }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.Forum.Fields.ForumGroupId")]
        public int ForumGroupId { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.Forum.Fields.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.Forum.Fields.Description")]

        public string Description { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.Forum.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Forums.Forum.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public List<ForumGroupModel> ForumGroups { get; set; }
    }
}