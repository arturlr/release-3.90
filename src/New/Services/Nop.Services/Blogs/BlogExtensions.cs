using Nop.Core.Domain.Blogs;

namespace Nop.Services.Blogs;

public static class BlogExtensions
{
    public static string[] ParseTags(BlogPost blogPost)
    {
        ArgumentNullException.ThrowIfNull(blogPost);

        if (string.IsNullOrEmpty(blogPost.Tags))
            return [];

        return blogPost.Tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .ToArray();
    }
}
