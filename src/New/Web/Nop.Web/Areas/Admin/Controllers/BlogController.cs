using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Seo;
using Nop.Services.Blogs;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Blogs;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class BlogController(
    IBlogService blogService,
    ILanguageService languageService,
    IDateTimeHelper dateTimeHelper,
    ICustomerService customerService,
    IUrlRecordService urlRecordService,
    IStoreService storeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    SeoSettings seoSettings) : BaseAdminController
{
    #region Blog posts

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var model = new BlogPostListModel();
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> BlogPostList(DataSourceRequest command, BlogPostListModel model)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var blogPosts = await blogService.GetAllBlogPostsAsync(
            model.SearchStoreId, 0, null, null,
            command.Page - 1, command.PageSize, showHidden: true);

        var languages = await languageService.GetAllLanguagesAsync(showHidden: true);
        var langDict = languages.ToDictionary(l => l.Id, l => l.Name);

        var gridModel = new DataSourceResult
        {
            Data = await Task.WhenAll(blogPosts.Select(async bp => new BlogPostGridModel
            {
                Id = bp.Id,
                Title = bp.Title,
                LanguageName = langDict.GetValueOrDefault(bp.LanguageId, "Unknown"),
                ApprovedComments = await blogService.GetBlogCommentsCountAsync(bp, isApproved: true),
                NotApprovedComments = await blogService.GetBlogCommentsCountAsync(bp, isApproved: false),
                StartDate = bp.StartDateUtc.HasValue ? dateTimeHelper.ConvertToUserTime(bp.StartDateUtc.Value, DateTimeKind.Utc) : null,
                EndDate = bp.EndDateUtc.HasValue ? dateTimeHelper.ConvertToUserTime(bp.EndDateUtc.Value, DateTimeKind.Utc) : null,
                CreatedOn = dateTimeHelper.ConvertToUserTime(bp.CreatedOnUtc, DateTimeKind.Utc)
            })),
            Total = blogPosts.TotalCount
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var model = new BlogPostModel { AllowComments = true };
        await PrepareBlogPostModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BlogPostModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var blogPost = MapModelToEntity(model, new BlogPost());
            blogPost.CreatedOnUtc = DateTime.UtcNow;

            await blogService.InsertBlogPostAsync(blogPost);

            var seName = await blogPost.ValidateSeNameAsync(
                model.SeName, blogPost.Title ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(blogPost, seName, blogPost.LanguageId);

            customerActivityService.InsertActivity("AddNewBlogPost", $"Added a new blog post (ID = {blogPost.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = blogPost.Id });

            return RedirectToAction("List");
        }

        await PrepareBlogPostModelDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var blogPost = await blogService.GetBlogPostByIdAsync(id);
        if (blogPost is null)
            return RedirectToAction("List");

        var model = MapEntityToModel(blogPost);
        model.SeName = await urlRecordService.GetActiveSlugAsync(blogPost.Id, "BlogPost", blogPost.LanguageId);

        await PrepareBlogPostModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BlogPostModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var blogPost = await blogService.GetBlogPostByIdAsync(model.Id);
        if (blogPost is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            MapModelToEntity(model, blogPost);
            await blogService.UpdateBlogPostAsync(blogPost);

            var seName = await blogPost.ValidateSeNameAsync(
                model.SeName, blogPost.Title ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(blogPost, seName, blogPost.LanguageId);

            customerActivityService.InsertActivity("EditBlogPost", $"Edited a blog post (ID = {blogPost.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = blogPost.Id });

            return RedirectToAction("List");
        }

        await PrepareBlogPostModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var blogPost = await blogService.GetBlogPostByIdAsync(id);
        if (blogPost is null)
            return RedirectToAction("List");

        await blogService.DeleteBlogPostAsync(blogPost);

        customerActivityService.InsertActivity("DeleteBlogPost", $"Deleted a blog post (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Comments

    public IActionResult Comments(int? filterByBlogPostId)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var model = new BlogCommentListModel
        {
            FilterByBlogPostId = filterByBlogPostId
        };

        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "All", Value = "0" });
        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "Approved only", Value = "1" });
        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "Disapproved only", Value = "2" });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> CommentList(DataSourceRequest command, BlogCommentListModel model)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var createdFrom = model.CreatedOnFrom.HasValue
            ? (DateTime?)dateTimeHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, dateTimeHelper.CurrentTimeZone)
            : null;
        var createdTo = model.CreatedOnTo.HasValue
            ? (DateTime?)dateTimeHelper.ConvertToUtcTime(model.CreatedOnTo.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : null;

        bool? approved = model.SearchApprovedId > 0 ? model.SearchApprovedId == 1 : null;

        var comments = await blogService.GetAllCommentsAsync(
            blogPostId: model.FilterByBlogPostId,
            approved: approved,
            fromUtc: createdFrom,
            toUtc: createdTo,
            commentText: model.SearchText);

        var stores = (await storeService.GetAllStoresAsync()).ToDictionary(s => s.Id, s => s.Name);

        var pagedComments = comments
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize);

        var gridData = new List<BlogCommentModel>();
        foreach (var c in pagedComments)
        {
            var blogPost = await blogService.GetBlogPostByIdAsync(c.BlogPostId);
            var customer = await customerService.GetCustomerByIdAsync(c.CustomerId);

            gridData.Add(new BlogCommentModel
            {
                Id = c.Id,
                BlogPostId = c.BlogPostId,
                BlogPostTitle = blogPost?.Title,
                CustomerId = c.CustomerId,
                CustomerInfo = customer?.Email ?? "Guest",
                Comment = Nop.Core.Html.HtmlHelper.FormatText(c.CommentText ?? string.Empty, false, true, false, false, false, false),
                IsApproved = c.IsApproved,
                StoreId = c.StoreId,
                StoreName = stores.GetValueOrDefault(c.StoreId, "Deleted"),
                CreatedOn = dateTimeHelper.ConvertToUserTime(c.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return Json(new DataSourceResult { Data = gridData, Total = comments.Count });
    }

    [HttpPost]
    public async Task<IActionResult> CommentUpdate(BlogCommentModel model)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var comment = await blogService.GetBlogCommentByIdAsync(model.Id);
        if (comment is null)
            return Json(new { Result = false });

        comment.IsApproved = model.IsApproved;
        await blogService.UpdateBlogCommentAsync(comment);

        customerActivityService.InsertActivity("EditBlogComment", $"Edited a blog comment (ID = {model.Id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<IActionResult> CommentDelete(int id)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        var comment = await blogService.GetBlogCommentByIdAsync(id);
        if (comment is null)
            return Json(new { Result = false });

        await blogService.DeleteBlogCommentAsync(comment);

        customerActivityService.InsertActivity("DeleteBlogComment", $"Deleted a blog comment (ID = {id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelectedComments(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        if (selectedIds is not null)
        {
            var comments = await blogService.GetBlogCommentsByIdsAsync(selectedIds.ToArray());
            await blogService.DeleteBlogCommentsAsync(comments);

            foreach (var c in comments)
                customerActivityService.InsertActivity("DeleteBlogComment", $"Deleted a blog comment (ID = {c.Id})");
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> ApproveSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        if (selectedIds is not null)
        {
            var comments = await blogService.GetBlogCommentsByIdsAsync(selectedIds.ToArray());
            foreach (var c in comments.Where(c => !c.IsApproved))
            {
                c.IsApproved = true;
                await blogService.UpdateBlogCommentAsync(c);
                customerActivityService.InsertActivity("EditBlogComment", $"Approved blog comment (ID = {c.Id})");
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> DisapproveSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageBlog"))
            return Forbid();

        if (selectedIds is not null)
        {
            var comments = await blogService.GetBlogCommentsByIdsAsync(selectedIds.ToArray());
            foreach (var c in comments.Where(c => c.IsApproved))
            {
                c.IsApproved = false;
                await blogService.UpdateBlogCommentAsync(c);
                customerActivityService.InsertActivity("EditBlogComment", $"Disapproved blog comment (ID = {c.Id})");
            }
        }

        return Json(new { Result = true });
    }

    #endregion
}
