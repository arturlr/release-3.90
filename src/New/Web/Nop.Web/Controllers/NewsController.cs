using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.News;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.News;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.News;

namespace Nop.Web.Controllers;

public class NewsController(
    INewsService newsService,
    IWorkContext workContext,
    IStoreContext storeContext,
    ILocalizationService localizationService,
    IWorkflowMessageService workflowMessageService,
    ICustomerActivityService customerActivityService,
    IStoreMappingService storeMappingService,
    ICustomerService customerService,
    IUrlRecordService urlRecordService,
    IDateTimeHelper dateTimeHelper,
    NewsSettings newsSettings) : BasePublicController
{
    // --- List ---

    public async Task<IActionResult> List(NewsPagingFilteringModel command)
    {
        if (!newsSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var pageSize = newsSettings.NewsArchivePageSize > 0 ? newsSettings.NewsArchivePageSize : 10;
        var pageIndex = (command.Page > 0 ? command.Page : 1) - 1;

        var newsItems = await newsService.GetAllNewsAsync(
            workContext.WorkingLanguage.Id, storeContext.CurrentStore.Id,
            pageIndex, pageSize);

        var model = new NewsItemListModel
        {
            WorkingLanguageId = workContext.WorkingLanguage.Id,
            PagingFilteringContext = { Page = pageIndex + 1, PageSize = pageSize }
        };

        foreach (var ni in newsItems)
            model.NewsItems.Add(await PrepareNewsItemModelAsync(ni, prepareComments: false));

        return View(model);
    }

    // --- Detail ---

    public async Task<IActionResult> NewsItem(int newsItemId)
    {
        if (!newsSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var newsItem = await newsService.GetNewsByIdAsync(newsItemId);
        if (newsItem == null ||
            !newsItem.Published ||
            (newsItem.StartDateUtc.HasValue && newsItem.StartDateUtc.Value >= DateTime.UtcNow) ||
            (newsItem.EndDateUtc.HasValue && newsItem.EndDateUtc.Value <= DateTime.UtcNow))
            return RedirectToAction("Index", "Home");

        if (!await storeMappingService.AuthorizeAsync(newsItem))
            return NotFound();

        var model = await PrepareNewsItemModelAsync(newsItem, prepareComments: true);
        return View(model);
    }

    // --- Add Comment ---

    [HttpPost, ActionName("NewsItem")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewsCommentAdd(int newsItemId, NewsItemModel model)
    {
        if (!newsSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var newsItem = await newsService.GetNewsByIdAsync(newsItemId);
        if (newsItem == null || !newsItem.Published || !newsItem.AllowComments)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (await IsGuestAsync(customer) && !newsSettings.AllowNotRegisteredUsersToLeaveComments)
            ModelState.AddModelError("", await localizationService.GetResourceAsync(
                "News.Comments.OnlyRegisteredUsersLeaveComments"));

        if (ModelState.IsValid)
        {
            var comment = new NewsComment
            {
                NewsItemId = newsItem.Id,
                CustomerId = customer.Id,
                CommentTitle = model.AddNewComment.CommentTitle,
                CommentText = model.AddNewComment.CommentText,
                IsApproved = !newsSettings.NewsCommentsMustBeApproved,
                StoreId = storeContext.CurrentStore.Id,
                CreatedOnUtc = DateTime.UtcNow,
            };
            await newsService.InsertNewsCommentAsync(comment);

            if (newsSettings.NotifyAboutNewNewsComments)
                await workflowMessageService.SendNewsCommentNotificationMessageAsync(comment, 0);

            customerActivityService.InsertActivity(customer,
                "PublicStore.AddNewsComment",
                await localizationService.GetResourceAsync("ActivityLog.PublicStore.AddNewsComment"));

            var seName = await newsItem.GetSeNameAsync(newsItem.LanguageId, urlRecordService,
                ensureTwoPublishedLanguages: false);

            TempData["nop.news.addcomment.result"] = comment.IsApproved
                ? await localizationService.GetResourceAsync("News.Comments.SuccessfullyAdded")
                : await localizationService.GetResourceAsync("News.Comments.SeeAfterApproving");

            return RedirectToAction("NewsItem", new { newsItemId = newsItem.Id, SeName = seName });
        }

        // Redisplay form on validation failure
        var redisplayModel = await PrepareNewsItemModelAsync(newsItem, prepareComments: true);
        return View("NewsItem", redisplayModel);
    }

    // --- Helpers ---

    private async Task<NewsItemModel> PrepareNewsItemModelAsync(NewsItem newsItem, bool prepareComments)
    {
        var seName = await newsItem.GetSeNameAsync(newsItem.LanguageId, urlRecordService,
            ensureTwoPublishedLanguages: false);

        var storeId = newsSettings.ShowNewsCommentsPerStore ? storeContext.CurrentStore.Id : 0;
        var commentCount = await newsService.GetNewsCommentsCountAsync(newsItem, storeId, isApproved: true);

        var model = new NewsItemModel
        {
            Id = newsItem.Id,
            MetaTitle = newsItem.MetaTitle,
            MetaDescription = newsItem.MetaDescription,
            MetaKeywords = newsItem.MetaKeywords,
            SeName = seName,
            Title = newsItem.Title,
            Short = newsItem.Short,
            Full = newsItem.Full,
            AllowComments = newsItem.AllowComments,
            CreatedOn = dateTimeHelper.ConvertToUserTime(newsItem.StartDateUtc ?? newsItem.CreatedOnUtc, DateTimeKind.Utc),
            NumberOfComments = commentCount,
        };

        if (prepareComments)
        {
            var comments = await newsService.GetAllCommentsAsync(
                storeId: storeId, newsItemId: newsItem.Id, approved: true);

            foreach (var nc in comments)
            {
                model.Comments.Add(new NewsCommentModel
                {
                    Id = nc.Id,
                    CustomerId = nc.CustomerId,
                    CommentTitle = nc.CommentTitle,
                    CommentText = nc.CommentText,
                    CreatedOn = dateTimeHelper.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc),
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
