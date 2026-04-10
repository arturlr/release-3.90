using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Customers;
using Nop.Services.Blogs;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Blogs;

namespace Nop.Web.Controllers;

public class BlogController(
    IBlogService blogService,
    IWorkContext workContext,
    IStoreContext storeContext,
    ILocalizationService localizationService,
    IWorkflowMessageService workflowMessageService,
    ICustomerActivityService customerActivityService,
    IStoreMappingService storeMappingService,
    ICustomerService customerService,
    IUrlRecordService urlRecordService,
    IDateTimeHelper dateTimeHelper,
    BlogSettings blogSettings) : BasePublicController
{
    // --- List / Tag / Month ---

    public async Task<IActionResult> List(BlogPagingFilteringModel command)
    {
        if (!blogSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var model = await PrepareBlogPostListModelAsync(command);
        return View("List", model);
    }

    public async Task<IActionResult> BlogByTag(BlogPagingFilteringModel command)
    {
        if (!blogSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var model = await PrepareBlogPostListModelAsync(command);
        return View("List", model);
    }

    public async Task<IActionResult> BlogByMonth(BlogPagingFilteringModel command)
    {
        if (!blogSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var model = await PrepareBlogPostListModelAsync(command);
        return View("List", model);
    }

    // --- Detail ---

    public async Task<IActionResult> BlogPost(int blogPostId)
    {
        if (!blogSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var blogPost = await blogService.GetBlogPostByIdAsync(blogPostId);
        if (blogPost == null ||
            (blogPost.StartDateUtc.HasValue && blogPost.StartDateUtc.Value >= DateTime.UtcNow) ||
            (blogPost.EndDateUtc.HasValue && blogPost.EndDateUtc.Value <= DateTime.UtcNow))
            return RedirectToAction("Index", "Home");

        if (!await storeMappingService.AuthorizeAsync(blogPost))
            return NotFound();

        var model = await PrepareBlogPostModelAsync(blogPost, prepareComments: true);
        return View(model);
    }

    // --- Add Comment ---

    [HttpPost, ActionName("BlogPost")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlogCommentAdd(int blogPostId, BlogPostModel model)
    {
        if (!blogSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var blogPost = await blogService.GetBlogPostByIdAsync(blogPostId);
        if (blogPost == null || !blogPost.AllowComments)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (await IsGuestAsync(customer) && !blogSettings.AllowNotRegisteredUsersToLeaveComments)
            ModelState.AddModelError("", await localizationService.GetResourceAsync(
                "Blog.Comments.OnlyRegisteredUsersLeaveComments"));

        if (ModelState.IsValid)
        {
            var comment = new BlogComment
            {
                BlogPostId = blogPost.Id,
                CustomerId = customer.Id,
                CommentText = model.AddNewComment.CommentText,
                IsApproved = !blogSettings.BlogCommentsMustBeApproved,
                StoreId = storeContext.CurrentStore.Id,
                CreatedOnUtc = DateTime.UtcNow,
            };
            await blogService.InsertBlogCommentAsync(comment);

            if (blogSettings.NotifyAboutNewBlogComments)
                await workflowMessageService.SendBlogCommentNotificationMessageAsync(comment, 0);

            customerActivityService.InsertActivity(customer,
                "PublicStore.AddBlogComment",
                await localizationService.GetResourceAsync("ActivityLog.PublicStore.AddBlogComment"));

            var seName = await blogPost.GetSeNameAsync(blogPost.LanguageId, urlRecordService,
                ensureTwoPublishedLanguages: false);

            TempData["nop.blog.addcomment.result"] = comment.IsApproved
                ? await localizationService.GetResourceAsync("Blog.Comments.SuccessfullyAdded")
                : await localizationService.GetResourceAsync("Blog.Comments.SeeAfterApproving");

            return RedirectToAction("BlogPost", new { blogPostId = blogPost.Id, SeName = seName });
        }

        // Redisplay form on validation failure
        var redisplayModel = await PrepareBlogPostModelAsync(blogPost, prepareComments: true);
        return View("BlogPost", redisplayModel);
    }

    // --- Helpers ---

    private async Task<BlogPostListModel> PrepareBlogPostListModelAsync(BlogPagingFilteringModel command)
    {
        var model = new BlogPostListModel
        {
            WorkingLanguageId = workContext.WorkingLanguage.Id,
            PagingFilteringContext = { Tag = command.Tag, Month = command.Month }
        };

        var pageSize = blogSettings.PostsPageSize > 0 ? blogSettings.PostsPageSize : 10;
        var pageIndex = (command.Page > 0 ? command.Page : 1) - 1;

        IPagedList<BlogPost> blogPosts;
        if (!string.IsNullOrEmpty(command.Tag))
        {
            blogPosts = await blogService.GetAllBlogPostsByTagAsync(
                storeContext.CurrentStore.Id, workContext.WorkingLanguage.Id,
                command.Tag, pageIndex, pageSize);
        }
        else
        {
            blogPosts = await blogService.GetAllBlogPostsAsync(
                storeContext.CurrentStore.Id, workContext.WorkingLanguage.Id,
                command.GetFromMonth(), command.GetToMonth(),
                pageIndex, pageSize);
        }

        model.PagingFilteringContext.Page = pageIndex + 1;
        model.PagingFilteringContext.PageSize = pageSize;

        foreach (var bp in blogPosts)
            model.BlogPosts.Add(await PrepareBlogPostModelAsync(bp, prepareComments: false));

        return model;
    }

    private async Task<BlogPostModel> PrepareBlogPostModelAsync(BlogPost blogPost, bool prepareComments)
    {
        var seName = await blogPost.GetSeNameAsync(blogPost.LanguageId, urlRecordService,
            ensureTwoPublishedLanguages: false);

        var storeId = blogSettings.ShowBlogCommentsPerStore ? storeContext.CurrentStore.Id : 0;
        var commentCount = await blogService.GetBlogCommentsCountAsync(blogPost, storeId, isApproved: true);

        var model = new BlogPostModel
        {
            Id = blogPost.Id,
            MetaTitle = blogPost.MetaTitle,
            MetaDescription = blogPost.MetaDescription,
            MetaKeywords = blogPost.MetaKeywords,
            SeName = seName,
            Title = blogPost.Title,
            Body = blogPost.Body,
            BodyOverview = blogPost.BodyOverview,
            AllowComments = blogPost.AllowComments,
            CreatedOn = dateTimeHelper.ConvertToUserTime(blogPost.StartDateUtc ?? blogPost.CreatedOnUtc, DateTimeKind.Utc),
            Tags = BlogExtensions.ParseTags(blogPost).ToList(),
            NumberOfComments = commentCount,
        };

        if (prepareComments)
        {
            var comments = await blogService.GetAllCommentsAsync(
                storeId: storeId, blogPostId: blogPost.Id, approved: true);

            foreach (var bc in comments)
            {
                model.Comments.Add(new BlogCommentModel
                {
                    Id = bc.Id,
                    CustomerId = bc.CustomerId,
                    CommentText = bc.CommentText,
                    CreatedOn = dateTimeHelper.ConvertToUserTime(bc.CreatedOnUtc, DateTimeKind.Utc),
                });
            }
        }

        return model;
    }

    private async Task<bool> IsGuestAsync(Customer customer)
    {
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole == null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(guestRole.Id);
    }
}
