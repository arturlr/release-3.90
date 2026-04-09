using Nop.Core;
using Nop.Core.Domain.News;

namespace Nop.Services.News;

public interface INewsService
{
    Task DeleteNewsAsync(NewsItem newsItem);

    Task<NewsItem?> GetNewsByIdAsync(int newsId);

    Task<IList<NewsItem>> GetNewsByIdsAsync(int[] newsIds);

    Task<IPagedList<NewsItem>> GetAllNewsAsync(
        int languageId = 0, int storeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

    Task InsertNewsAsync(NewsItem newsItem);

    Task UpdateNewsAsync(NewsItem newsItem);

    Task<IList<NewsComment>> GetAllCommentsAsync(
        int customerId = 0, int storeId = 0, int? newsItemId = null,
        bool? approved = null, DateTime? fromUtc = null, DateTime? toUtc = null,
        string? commentText = null);

    Task<NewsComment?> GetNewsCommentByIdAsync(int newsCommentId);

    Task<IList<NewsComment>> GetNewsCommentsByIdsAsync(int[] commentIds);

    Task<int> GetNewsCommentsCountAsync(NewsItem newsItem, int storeId = 0, bool? isApproved = null);

    Task DeleteNewsCommentAsync(NewsComment newsComment);

    Task DeleteNewsCommentsAsync(IList<NewsComment> newsComments);
}
