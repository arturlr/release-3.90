using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.News;

namespace Nop.Services.News
{
    public interface INewsService
    {
        Task<NewsItem> GetNewsByIdAsync(int newsId);
        Task<IList<NewsItem>> GetAllNewsAsync(int pageIndex = 0, int pageSize = int.MaxValue);
        Task InsertNewsAsync(NewsItem news);
        Task UpdateNewsAsync(NewsItem news);
        Task DeleteNewsAsync(NewsItem news);
    }
}
