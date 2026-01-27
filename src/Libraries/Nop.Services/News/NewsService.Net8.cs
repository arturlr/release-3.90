using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.News;
using Nop.Data;

namespace Nop.Services.News
{
    public class NewsService : INewsService
    {
        private readonly IRepository<NewsItem> _newsRepository;

        public NewsService(IRepository<NewsItem> newsRepository)
        {
            _newsRepository = newsRepository;
        }

        public virtual async Task<NewsItem> GetNewsByIdAsync(int newsId)
        {
            if (newsId == 0)
                return null;

            return await _newsRepository.GetByIdAsync(newsId);
        }

        public virtual async Task<IList<NewsItem>> GetAllNewsAsync(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _newsRepository.Table
                .Where(n => n.Published)
                .OrderByDescending(n => n.CreatedOnUtc);

            if (pageSize != int.MaxValue)
                query = (IOrderedQueryable<NewsItem>)query.Skip(pageIndex * pageSize).Take(pageSize);

            return await query.ToListAsync();
        }

        public virtual async Task InsertNewsAsync(NewsItem news)
        {
            if (news == null)
                throw new ArgumentNullException(nameof(news));

            await _newsRepository.InsertAsync(news);
        }

        public virtual async Task UpdateNewsAsync(NewsItem news)
        {
            if (news == null)
                throw new ArgumentNullException(nameof(news));

            await _newsRepository.UpdateAsync(news);
        }

        public virtual async Task DeleteNewsAsync(NewsItem news)
        {
            if (news == null)
                throw new ArgumentNullException(nameof(news));

            await _newsRepository.DeleteAsync(news);
        }
    }
}
