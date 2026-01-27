using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.News;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class BlogController : BasePublicController
    {
        private readonly INewsService _newsService;

        public BlogController(
            IWorkContext workContext,
            INewsService newsService) : base(workContext)
        {
            _newsService = newsService;
        }

        // GET: /Blog
        public async Task<IActionResult> List(int page = 0)
        {
            var posts = await _newsService.GetAllNewsAsync(page, 10);
            return View(posts);
        }

        // GET: /Blog/BlogPost/5
        public async Task<IActionResult> BlogPost(int blogPostId)
        {
            var post = await _newsService.GetNewsByIdAsync(blogPostId);
            if (post == null || !post.Published)
                return NotFound();

            return View(post);
        }

        // POST: /Blog/BlogCommentAdd
        [HttpPost]
        public IActionResult BlogCommentAdd(int blogPostId, string comment)
        {
            // TODO: Add comment
            return Json(new { success = true, message = "Comment added" });
        }
    }
}
