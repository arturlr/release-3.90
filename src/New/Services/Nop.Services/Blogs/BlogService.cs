using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Blogs;

public class BlogService : IBlogService
{
    private readonly IRepository<BlogPost> _blogPostRepository;
    private readonly IRepository<BlogComment> _blogCommentRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly CatalogSettings _catalogSettings;
    private readonly IEventPublisher _eventPublisher;

    public BlogService(
        IRepository<BlogPost> blogPostRepository,
        IRepository<BlogComment> blogCommentRepository,
        IRepository<StoreMapping> storeMappingRepository,
        CatalogSettings catalogSettings,
        IEventPublisher eventPublisher)
    {
        _blogPostRepository = blogPostRepository;
        _blogCommentRepository = blogCommentRepository;
        _storeMappingRepository = storeMappingRepository;
        _catalogSettings = catalogSettings;
        _eventPublisher = eventPublisher;
    }

    #region Blog posts

    public async Task DeleteBlogPostAsync(BlogPost blogPost)
    {
        ArgumentNullException.ThrowIfNull(blogPost);
        _blogPostRepository.Delete(blogPost);
        await _eventPublisher.EntityDeletedAsync(blogPost);
    }

    public Task<BlogPost?> GetBlogPostByIdAsync(int blogPostId)
    {
        return Task.FromResult(blogPostId == 0 ? null : _blogPostRepository.GetById(blogPostId));
    }

    public Task<IList<BlogPost>> GetBlogPostsByIdsAsync(int[] blogPostIds)
    {
        IList<BlogPost> result = _blogPostRepository.Table
            .Where(bp => blogPostIds.Contains(bp.Id))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IPagedList<BlogPost>> GetAllBlogPostsAsync(
        int storeId = 0, int languageId = 0,
        DateTime? dateFrom = null, DateTime? dateTo = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var query = _blogPostRepository.Table;

        if (dateFrom.HasValue)
            query = query.Where(b => dateFrom.Value <= (b.StartDateUtc ?? b.CreatedOnUtc));
        if (dateTo.HasValue)
            query = query.Where(b => dateTo.Value >= (b.StartDateUtc ?? b.CreatedOnUtc));
        if (languageId > 0)
            query = query.Where(b => b.LanguageId == languageId);

        if (!showHidden)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(b => !b.StartDateUtc.HasValue || b.StartDateUtc <= utcNow);
            query = query.Where(b => !b.EndDateUtc.HasValue || b.EndDateUtc >= utcNow);
        }

        if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
        {
            query = from bp in query
                    join sm in _storeMappingRepository.Table
                        on new { c1 = bp.Id, c2 = "BlogPost" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into bp_sm
                    from sm in bp_sm.DefaultIfEmpty()
                    where !bp.LimitedToStores || storeId == sm.StoreId
                    select bp;

            query = from bp in query
                    group bp by bp.Id into bpGroup
                    orderby bpGroup.Key
                    select bpGroup.First();
        }

        query = query.OrderByDescending(b => b.StartDateUtc ?? b.CreatedOnUtc);

        IPagedList<BlogPost> result = new PagedList<BlogPost>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public async Task<IPagedList<BlogPost>> GetAllBlogPostsByTagAsync(
        int storeId = 0, int languageId = 0, string tag = "",
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        tag = tag.Trim();

        var blogPostsAll = await GetAllBlogPostsAsync(storeId: storeId, languageId: languageId, showHidden: showHidden);
        var taggedBlogPosts = new List<BlogPost>();
        foreach (var blogPost in blogPostsAll)
        {
            var tags = BlogExtensions.ParseTags(blogPost);
            if (tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                taggedBlogPosts.Add(blogPost);
        }

        return new PagedList<BlogPost>(taggedBlogPosts, pageIndex, pageSize);
    }

    public async Task<IList<BlogPostTag>> GetAllBlogPostTagsAsync(int storeId, int languageId, bool showHidden = false)
    {
        var blogPosts = await GetAllBlogPostsAsync(storeId: storeId, languageId: languageId, showHidden: showHidden);
        var blogPostTags = new List<BlogPostTag>();

        foreach (var blogPost in blogPosts)
        {
            var tags = BlogExtensions.ParseTags(blogPost);
            foreach (var tag in tags)
            {
                var found = blogPostTags.Find(bpt => bpt.Name!.Equals(tag, StringComparison.OrdinalIgnoreCase));
                if (found == null)
                {
                    blogPostTags.Add(new BlogPostTag { Name = tag, BlogPostCount = 1 });
                }
                else
                {
                    found.BlogPostCount++;
                }
            }
        }

        return blogPostTags;
    }

    public async Task InsertBlogPostAsync(BlogPost blogPost)
    {
        ArgumentNullException.ThrowIfNull(blogPost);
        _blogPostRepository.Insert(blogPost);
        await _eventPublisher.EntityInsertedAsync(blogPost);
    }

    public async Task UpdateBlogPostAsync(BlogPost blogPost)
    {
        ArgumentNullException.ThrowIfNull(blogPost);
        _blogPostRepository.Update(blogPost);
        await _eventPublisher.EntityUpdatedAsync(blogPost);
    }

    #endregion

    #region Blog comments

    public Task<IList<BlogComment>> GetAllCommentsAsync(
        int customerId = 0, int storeId = 0, int? blogPostId = null,
        bool? approved = null, DateTime? fromUtc = null, DateTime? toUtc = null,
        string? commentText = null)
    {
        var query = _blogCommentRepository.Table;

        if (approved.HasValue)
            query = query.Where(c => c.IsApproved == approved);
        if (blogPostId > 0)
            query = query.Where(c => c.BlogPostId == blogPostId);
        if (customerId > 0)
            query = query.Where(c => c.CustomerId == customerId);
        if (storeId > 0)
            query = query.Where(c => c.StoreId == storeId);
        if (fromUtc.HasValue)
            query = query.Where(c => fromUtc.Value <= c.CreatedOnUtc);
        if (toUtc.HasValue)
            query = query.Where(c => toUtc.Value >= c.CreatedOnUtc);
        if (!string.IsNullOrEmpty(commentText))
            query = query.Where(c => c.CommentText!.Contains(commentText));

        query = query.OrderBy(c => c.CreatedOnUtc);

        IList<BlogComment> result = query.ToList();
        return Task.FromResult(result);
    }

    public Task<BlogComment?> GetBlogCommentByIdAsync(int blogCommentId)
    {
        return Task.FromResult(blogCommentId == 0 ? null : _blogCommentRepository.GetById(blogCommentId));
    }

    public Task<IList<BlogComment>> GetBlogCommentsByIdsAsync(int[] commentIds)
    {
        if (commentIds == null || commentIds.Length == 0)
            return Task.FromResult<IList<BlogComment>>(new List<BlogComment>());

        var comments = _blogCommentRepository.Table
            .Where(bc => commentIds.Contains(bc.Id))
            .ToList();

        // sort by passed identifiers
        IList<BlogComment> sorted = commentIds
            .Select(id => comments.Find(x => x.Id == id))
            .Where(c => c != null)
            .ToList()!;

        return Task.FromResult(sorted);
    }

    public Task<int> GetBlogCommentsCountAsync(BlogPost blogPost, int storeId = 0, bool? isApproved = null)
    {
        ArgumentNullException.ThrowIfNull(blogPost);

        var query = _blogCommentRepository.Table.Where(c => c.BlogPostId == blogPost.Id);

        if (storeId > 0)
            query = query.Where(c => c.StoreId == storeId);
        if (isApproved.HasValue)
            query = query.Where(c => c.IsApproved == isApproved.Value);

        return Task.FromResult(query.Count());
    }

    public async Task InsertBlogCommentAsync(BlogComment blogComment)
    {
        ArgumentNullException.ThrowIfNull(blogComment);
        _blogCommentRepository.Insert(blogComment);
        await _eventPublisher.EntityInsertedAsync(blogComment);
    }

    public async Task DeleteBlogCommentAsync(BlogComment blogComment)
    {
        ArgumentNullException.ThrowIfNull(blogComment);
        _blogCommentRepository.Delete(blogComment);
        await _eventPublisher.EntityDeletedAsync(blogComment);
    }

    public async Task DeleteBlogCommentsAsync(IList<BlogComment> blogComments)
    {
        ArgumentNullException.ThrowIfNull(blogComments);
        foreach (var blogComment in blogComments)
        {
            await DeleteBlogCommentAsync(blogComment);
        }
    }

    #endregion
}
