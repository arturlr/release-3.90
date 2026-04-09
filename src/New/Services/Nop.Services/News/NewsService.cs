using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.News;

public class NewsService : INewsService
{
    private readonly IRepository<NewsItem> _newsItemRepository;
    private readonly IRepository<NewsComment> _newsCommentRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly CatalogSettings _catalogSettings;
    private readonly IEventPublisher _eventPublisher;

    public NewsService(
        IRepository<NewsItem> newsItemRepository,
        IRepository<NewsComment> newsCommentRepository,
        IRepository<StoreMapping> storeMappingRepository,
        CatalogSettings catalogSettings,
        IEventPublisher eventPublisher)
    {
        _newsItemRepository = newsItemRepository;
        _newsCommentRepository = newsCommentRepository;
        _storeMappingRepository = storeMappingRepository;
        _catalogSettings = catalogSettings;
        _eventPublisher = eventPublisher;
    }

    #region News items

    public async Task DeleteNewsAsync(NewsItem newsItem)
    {
        ArgumentNullException.ThrowIfNull(newsItem);
        _newsItemRepository.Delete(newsItem);
        await _eventPublisher.EntityDeletedAsync(newsItem);
    }

    public Task<NewsItem?> GetNewsByIdAsync(int newsId)
    {
        return Task.FromResult(newsId == 0 ? null : _newsItemRepository.GetById(newsId));
    }

    public Task<IList<NewsItem>> GetNewsByIdsAsync(int[] newsIds)
    {
        IList<NewsItem> result = _newsItemRepository.Table
            .Where(n => newsIds.Contains(n.Id))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IPagedList<NewsItem>> GetAllNewsAsync(
        int languageId = 0, int storeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var query = _newsItemRepository.Table;

        if (languageId > 0)
            query = query.Where(n => n.LanguageId == languageId);

        if (!showHidden)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(n => n.Published);
            query = query.Where(n => !n.StartDateUtc.HasValue || n.StartDateUtc <= utcNow);
            query = query.Where(n => !n.EndDateUtc.HasValue || n.EndDateUtc >= utcNow);
        }

        if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
        {
            query = from n in query
                    join sm in _storeMappingRepository.Table
                        on new { c1 = n.Id, c2 = "NewsItem" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into n_sm
                    from sm in n_sm.DefaultIfEmpty()
                    where !n.LimitedToStores || storeId == sm.StoreId
                    select n;

            query = from n in query
                    group n by n.Id into nGroup
                    orderby nGroup.Key
                    select nGroup.First();
        }

        query = query.OrderByDescending(n => n.StartDateUtc ?? n.CreatedOnUtc);

        IPagedList<NewsItem> result = new PagedList<NewsItem>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public async Task InsertNewsAsync(NewsItem newsItem)
    {
        ArgumentNullException.ThrowIfNull(newsItem);
        _newsItemRepository.Insert(newsItem);
        await _eventPublisher.EntityInsertedAsync(newsItem);
    }

    public async Task UpdateNewsAsync(NewsItem newsItem)
    {
        ArgumentNullException.ThrowIfNull(newsItem);
        _newsItemRepository.Update(newsItem);
        await _eventPublisher.EntityUpdatedAsync(newsItem);
    }

    #endregion

    #region News comments

    public Task<IList<NewsComment>> GetAllCommentsAsync(
        int customerId = 0, int storeId = 0, int? newsItemId = null,
        bool? approved = null, DateTime? fromUtc = null, DateTime? toUtc = null,
        string? commentText = null)
    {
        var query = _newsCommentRepository.Table;

        if (approved.HasValue)
            query = query.Where(c => c.IsApproved == approved);
        if (newsItemId > 0)
            query = query.Where(c => c.NewsItemId == newsItemId);
        if (customerId > 0)
            query = query.Where(c => c.CustomerId == customerId);
        if (storeId > 0)
            query = query.Where(c => c.StoreId == storeId);
        if (fromUtc.HasValue)
            query = query.Where(c => fromUtc.Value <= c.CreatedOnUtc);
        if (toUtc.HasValue)
            query = query.Where(c => toUtc.Value >= c.CreatedOnUtc);
        if (!string.IsNullOrEmpty(commentText))
            query = query.Where(c => c.CommentText!.Contains(commentText) || c.CommentTitle!.Contains(commentText));

        query = query.OrderBy(c => c.CreatedOnUtc);

        IList<NewsComment> result = query.ToList();
        return Task.FromResult(result);
    }

    public Task<NewsComment?> GetNewsCommentByIdAsync(int newsCommentId)
    {
        return Task.FromResult(newsCommentId == 0 ? null : _newsCommentRepository.GetById(newsCommentId));
    }

    public Task<IList<NewsComment>> GetNewsCommentsByIdsAsync(int[] commentIds)
    {
        if (commentIds == null || commentIds.Length == 0)
            return Task.FromResult<IList<NewsComment>>([]);

        var comments = _newsCommentRepository.Table
            .Where(nc => commentIds.Contains(nc.Id))
            .ToList();

        IList<NewsComment> sorted = commentIds
            .Select(id => comments.Find(x => x.Id == id))
            .Where(c => c != null)
            .ToList()!;

        return Task.FromResult(sorted);
    }

    public Task<int> GetNewsCommentsCountAsync(NewsItem newsItem, int storeId = 0, bool? isApproved = null)
    {
        ArgumentNullException.ThrowIfNull(newsItem);

        var query = _newsCommentRepository.Table.Where(c => c.NewsItemId == newsItem.Id);

        if (storeId > 0)
            query = query.Where(c => c.StoreId == storeId);
        if (isApproved.HasValue)
            query = query.Where(c => c.IsApproved == isApproved.Value);

        return Task.FromResult(query.Count());
    }

    public async Task DeleteNewsCommentAsync(NewsComment newsComment)
    {
        ArgumentNullException.ThrowIfNull(newsComment);
        _newsCommentRepository.Delete(newsComment);
        await _eventPublisher.EntityDeletedAsync(newsComment);
    }

    public async Task DeleteNewsCommentsAsync(IList<NewsComment> newsComments)
    {
        ArgumentNullException.ThrowIfNull(newsComments);
        foreach (var newsComment in newsComments)
        {
            await DeleteNewsCommentAsync(newsComment);
        }
    }

    #endregion
}
