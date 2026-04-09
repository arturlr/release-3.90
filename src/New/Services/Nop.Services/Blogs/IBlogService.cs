using Nop.Core;
using Nop.Core.Domain.Blogs;

namespace Nop.Services.Blogs;

public interface IBlogService
{
    Task DeleteBlogPostAsync(BlogPost blogPost);

    Task<BlogPost?> GetBlogPostByIdAsync(int blogPostId);

    Task<IList<BlogPost>> GetBlogPostsByIdsAsync(int[] blogPostIds);

    Task<IPagedList<BlogPost>> GetAllBlogPostsAsync(
        int storeId = 0, int languageId = 0,
        DateTime? dateFrom = null, DateTime? dateTo = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

    Task<IPagedList<BlogPost>> GetAllBlogPostsByTagAsync(
        int storeId = 0, int languageId = 0, string tag = "",
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

    Task<IList<BlogPostTag>> GetAllBlogPostTagsAsync(int storeId, int languageId, bool showHidden = false);

    Task InsertBlogPostAsync(BlogPost blogPost);

    Task UpdateBlogPostAsync(BlogPost blogPost);

    Task<IList<BlogComment>> GetAllCommentsAsync(
        int customerId = 0, int storeId = 0, int? blogPostId = null,
        bool? approved = null, DateTime? fromUtc = null, DateTime? toUtc = null,
        string? commentText = null);

    Task<BlogComment?> GetBlogCommentByIdAsync(int blogCommentId);

    Task<IList<BlogComment>> GetBlogCommentsByIdsAsync(int[] commentIds);

    Task<int> GetBlogCommentsCountAsync(BlogPost blogPost, int storeId = 0, bool? isApproved = null);

    Task DeleteBlogCommentAsync(BlogComment blogComment);

    Task DeleteBlogCommentsAsync(IList<BlogComment> blogComments);
}
