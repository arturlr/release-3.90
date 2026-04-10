using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.News;

public class AddNewsCommentModel : BaseNopModel
{
    public string? CommentTitle { get; set; }
    public string? CommentText { get; set; }
}
