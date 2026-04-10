using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Forums;

public class ForumGroupModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class ForumModel : BaseNopEntityModel
{
    public int ForumGroupId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedOn { get; set; }

    public List<SelectListItem> AvailableForumGroups { get; set; } = [];
}
