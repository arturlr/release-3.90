using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Blogs;

public class AddBlogCommentModel : BaseNopModel
{
    public string? CommentText { get; set; }
}
