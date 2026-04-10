using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Blogs;

public class BlogPostYearModel : BaseNopModel
{
    public int Year { get; set; }
    public IList<BlogPostMonthModel> Months { get; set; } = [];
}

public class BlogPostMonthModel : BaseNopModel
{
    public int Month { get; set; }
    public int BlogPostCount { get; set; }
}
