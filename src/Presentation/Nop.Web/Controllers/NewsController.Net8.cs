using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.News;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class NewsController : BasePublicController
    {
        private readonly INewsService _newsService;

        public NewsController(
            IWorkContext workContext,
            INewsService newsService) : base(workContext)
        {
            _newsService = newsService;
        }

        // GET: /News
        public async Task<IActionResult> List(int page = 0)
        {
            var news = await _newsService.GetAllNewsAsync(page, 10);
            return View(news);
        }

        // GET: /News/NewsItem/5
        public async Task<IActionResult> NewsItem(int newsItemId)
        {
            var newsItem = await _newsService.GetNewsByIdAsync(newsItemId);
            if (newsItem == null || !newsItem.Published)
                return NotFound();

            return View(newsItem);
        }

        // GET: /News/Rss
        public IActionResult Rss()
        {
            return Content("RSS Feed", "application/rss+xml");
        }
    }
}
