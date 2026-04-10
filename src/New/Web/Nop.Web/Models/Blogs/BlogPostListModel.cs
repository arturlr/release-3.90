using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Blogs;

public class BlogPostListModel : BaseNopModel
{
    public int WorkingLanguageId { get; set; }
    public BlogPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public IList<BlogPostModel> BlogPosts { get; set; } = [];
}
