using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Seo;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.News;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.News;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class NewsController(
    INewsService newsService,
    ILanguageService languageService,
    IDateTimeHelper dateTimeHelper,
    ICustomerService customerService,
    IUrlRecordService urlRecordService,
    IStoreService storeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    SeoSettings seoSettings) : BaseAdminController
{
    #region News items

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var model = new NewsItemListModel();
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> NewsItemList(DataSourceRequest command, NewsItemListModel model)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var newsItems = await newsService.GetAllNewsAsync(
            0, model.SearchStoreId,
            command.Page - 1, command.PageSize, showHidden: true);

        var languages = await languageService.GetAllLanguagesAsync(showHidden: true);
        var langDict = languages.ToDictionary(l => l.Id, l => l.Name);

        var gridModel = new DataSourceResult
        {
            Data = await Task.WhenAll(newsItems.Select(async n => new NewsItemGridModel
            {
                Id = n.Id,
                Title = n.Title,
                LanguageName = langDict.GetValueOrDefault(n.LanguageId, "Unknown"),
                Published = n.Published,
                ApprovedComments = await newsService.GetNewsCommentsCountAsync(n, isApproved: true),
                NotApprovedComments = await newsService.GetNewsCommentsCountAsync(n, isApproved: false),
                StartDate = n.StartDateUtc.HasValue ? dateTimeHelper.ConvertToUserTime(n.StartDateUtc.Value, DateTimeKind.Utc) : null,
                EndDate = n.EndDateUtc.HasValue ? dateTimeHelper.ConvertToUserTime(n.EndDateUtc.Value, DateTimeKind.Utc) : null,
                CreatedOn = dateTimeHelper.ConvertToUserTime(n.CreatedOnUtc, DateTimeKind.Utc)
            })),
            Total = newsItems.TotalCount
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var model = new NewsItemModel { Published = true, AllowComments = true };
        await PrepareNewsItemModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewsItemModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var newsItem = MapModelToEntity(model, new NewsItem());
            newsItem.CreatedOnUtc = DateTime.UtcNow;

            await newsService.InsertNewsAsync(newsItem);

            var seName = await newsItem.ValidateSeNameAsync(
                model.SeName, newsItem.Title ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(newsItem, seName, newsItem.LanguageId);

            customerActivityService.InsertActivity("AddNewNews", $"Added a new news item (ID = {newsItem.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = newsItem.Id });

            return RedirectToAction("List");
        }

        await PrepareNewsItemModelDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var newsItem = await newsService.GetNewsByIdAsync(id);
        if (newsItem is null)
            return RedirectToAction("List");

        var model = MapEntityToModel(newsItem);
        model.SeName = await urlRecordService.GetActiveSlugAsync(newsItem.Id, "NewsItem", newsItem.LanguageId);

        await PrepareNewsItemModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(NewsItemModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var newsItem = await newsService.GetNewsByIdAsync(model.Id);
        if (newsItem is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            MapModelToEntity(model, newsItem);
            await newsService.UpdateNewsAsync(newsItem);

            var seName = await newsItem.ValidateSeNameAsync(
                model.SeName, newsItem.Title ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(newsItem, seName, newsItem.LanguageId);

            customerActivityService.InsertActivity("EditNews", $"Edited a news item (ID = {newsItem.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = newsItem.Id });

            return RedirectToAction("List");
        }

        await PrepareNewsItemModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var newsItem = await newsService.GetNewsByIdAsync(id);
        if (newsItem is null)
            return RedirectToAction("List");

        await newsService.DeleteNewsAsync(newsItem);

        customerActivityService.InsertActivity("DeleteNews", $"Deleted a news item (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Comments

    public IActionResult Comments(int? filterByNewsItemId)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var model = new NewsCommentListModel
        {
            FilterByNewsItemId = filterByNewsItemId
        };

        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "All", Value = "0" });
        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "Approved only", Value = "1" });
        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "Disapproved only", Value = "2" });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> CommentList(DataSourceRequest command, NewsCommentListModel model)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var createdFrom = model.CreatedOnFrom.HasValue
            ? (DateTime?)dateTimeHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, dateTimeHelper.CurrentTimeZone)
            : null;
        var createdTo = model.CreatedOnTo.HasValue
            ? (DateTime?)dateTimeHelper.ConvertToUtcTime(model.CreatedOnTo.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : null;

        bool? approved = model.SearchApprovedId > 0 ? model.SearchApprovedId == 1 : null;

        var comments = await newsService.GetAllCommentsAsync(
            newsItemId: model.FilterByNewsItemId,
            approved: approved,
            fromUtc: createdFrom,
            toUtc: createdTo,
            commentText: model.SearchText);

        var stores = (await storeService.GetAllStoresAsync()).ToDictionary(s => s.Id, s => s.Name);

        var pagedComments = comments
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize);

        var gridData = new List<NewsCommentModel>();
        foreach (var c in pagedComments)
        {
            var newsItem = await newsService.GetNewsByIdAsync(c.NewsItemId);
            var customer = await customerService.GetCustomerByIdAsync(c.CustomerId);

            gridData.Add(new NewsCommentModel
            {
                Id = c.Id,
                NewsItemId = c.NewsItemId,
                NewsItemTitle = newsItem?.Title,
                CustomerId = c.CustomerId,
                CustomerInfo = customer?.Email ?? "Guest",
                CommentTitle = c.CommentTitle,
                CommentText = Nop.Core.Html.HtmlHelper.FormatText(c.CommentText ?? string.Empty, false, true, false, false, false, false),
                IsApproved = c.IsApproved,
                StoreId = c.StoreId,
                StoreName = stores.GetValueOrDefault(c.StoreId, "Deleted"),
                CreatedOn = dateTimeHelper.ConvertToUserTime(c.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return Json(new DataSourceResult { Data = gridData, Total = comments.Count });
    }

    [HttpPost]
    public async Task<IActionResult> CommentUpdate(NewsCommentModel model)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var comment = await newsService.GetNewsCommentByIdAsync(model.Id);
        if (comment is null)
            return Json(new { Result = false });

        comment.IsApproved = model.IsApproved;
        await newsService.UpdateNewsCommentAsync(comment);

        customerActivityService.InsertActivity("EditNewsComment", $"Edited a news comment (ID = {model.Id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<IActionResult> CommentDelete(int id)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        var comment = await newsService.GetNewsCommentByIdAsync(id);
        if (comment is null)
            return Json(new { Result = false });

        await newsService.DeleteNewsCommentAsync(comment);

        customerActivityService.InsertActivity("DeleteNewsComment", $"Deleted a news comment (ID = {id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelectedComments(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        if (selectedIds is not null)
        {
            var comments = await newsService.GetNewsCommentsByIdsAsync(selectedIds.ToArray());
            await newsService.DeleteNewsCommentsAsync(comments);

            foreach (var c in comments)
                customerActivityService.InsertActivity("DeleteNewsComment", $"Deleted a news comment (ID = {c.Id})");
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> ApproveSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        if (selectedIds is not null)
        {
            var comments = await newsService.GetNewsCommentsByIdsAsync(selectedIds.ToArray());
            foreach (var c in comments.Where(c => !c.IsApproved))
            {
                c.IsApproved = true;
                await newsService.UpdateNewsCommentAsync(c);
                customerActivityService.InsertActivity("EditNewsComment", $"Approved news comment (ID = {c.Id})");
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> DisapproveSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageNews"))
            return Forbid();

        if (selectedIds is not null)
        {
            var comments = await newsService.GetNewsCommentsByIdsAsync(selectedIds.ToArray());
            foreach (var c in comments.Where(c => c.IsApproved))
            {
                c.IsApproved = false;
                await newsService.UpdateNewsCommentAsync(c);
                customerActivityService.InsertActivity("EditNewsComment", $"Disapproved news comment (ID = {c.Id})");
            }
        }

        return Json(new { Result = true });
    }

    #endregion
}
