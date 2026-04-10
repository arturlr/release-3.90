using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Blogs;

public class BlogPostTagListModel : BaseNopModel
{
    public IList<BlogPostTagModel> Tags { get; set; } = [];

    public int GetFontSize(BlogPostTagModel tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        var weights = Tags.Select(t => (double)t.BlogPostCount).ToList();
        var mean = weights.Count == 0 ? 0 : weights.Average();
        var stdDev = weights.Count == 0 ? 0 : Math.Sqrt(weights.Average(w => (w - mean) * (w - mean)));

        var factor = stdDev == 0 ? 0 : (tag.BlogPostCount - mean) / stdDev;

        return factor > 2 ? 150 :
               factor > 1 ? 120 :
               factor > 0.5 ? 100 :
               factor > -0.5 ? 90 :
               factor > -1 ? 85 :
               factor > -2 ? 80 : 75;
    }
}
