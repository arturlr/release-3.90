using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Forums;
using Nop.Services.Seo;
using Nop.Web.Models.Boards;

namespace Nop.Web.Controllers;

public partial class BoardsController
{
    // --- Post Create ---

    public async Task<IActionResult> PostCreate(int id, int? quote)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(id);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToCreatePostAsync(customer, forumTopic))
            return Challenge();

        var forum = await forumService.GetForumByIdAsync(forumTopic.ForumId);
        var model = new EditForumPostModel
        {
            ForumTopicId = forumTopic.Id,
            ForumName = forum?.Name,
            ForumTopicSubject = forumTopic.Subject,
            ForumTopicSeName = SeoExtensions.GetSeName(forumTopic.Subject, false, false),
            ForumEditor = forumSettings.ForumEditor,
            IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer),
            Subscribed = false
        };

        if (quote.HasValue)
        {
            var quotePost = await forumService.GetPostByIdAsync(quote.Value);
            if (quotePost is { TopicId: var topicId } && topicId == forumTopic.Id)
                model.Text = $"[quote={quotePost.CustomerId}]{quotePost.Text}[/quote]";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostCreate(EditForumPostModel model)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(model.ForumTopicId);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToCreatePostAsync(customer, forumTopic))
            return Challenge();

        if (ModelState.IsValid)
        {
            try
            {
                var text = TruncateText(model.Text, forumSettings.PostMaxLength);
                var nowUtc = DateTime.UtcNow;

                var forumPost = new ForumPost
                {
                    TopicId = forumTopic.Id,
                    CustomerId = customer.Id,
                    Text = text,
                    IPAddress = webHelper.GetCurrentIpAddress(),
                    CreatedOnUtc = nowUtc,
                    UpdatedOnUtc = nowUtc
                };
                await forumService.InsertPostAsync(forumPost, true);

                await ManageSubscriptionAsync(customer, 0, forumPost.TopicId, model.Subscribed);

                var pageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10;
                var pageIndex = await forumService.CalculateTopicPageIndexAsync(forumPost.TopicId, pageSize, forumPost.Id);
                var topicSeName = SeoExtensions.GetSeName(forumTopic.Subject, false, false);

                var url = pageIndex > 0
                    ? Url.Action(nameof(Topic), new { id = forumPost.TopicId, page = pageIndex + 1 })
                    : Url.Action(nameof(Topic), new { id = forumPost.TopicId });

                return Redirect($"{url}#{forumPost.Id}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        var forum = await forumService.GetForumByIdAsync(forumTopic.ForumId);
        model.ForumName = forum?.Name;
        model.ForumTopicSubject = forumTopic.Subject;
        model.ForumTopicSeName = SeoExtensions.GetSeName(forumTopic.Subject, false, false);
        model.ForumEditor = forumSettings.ForumEditor;
        model.IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer);
        return View(model);
    }

    // --- Post Edit ---

    public async Task<IActionResult> PostEdit(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumPost = await forumService.GetPostByIdAsync(id);
        if (forumPost == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToEditPostAsync(customer, forumPost))
            return Challenge();

        var forumTopic = await forumService.GetTopicByIdAsync(forumPost.TopicId);
        var forum = forumTopic != null ? await forumService.GetForumByIdAsync(forumTopic.ForumId) : null;

        var model = new EditForumPostModel
        {
            IsEdit = true,
            Id = forumPost.Id,
            ForumTopicId = forumPost.TopicId,
            Text = forumPost.Text,
            ForumName = forum?.Name,
            ForumTopicSubject = forumTopic?.Subject,
            ForumTopicSeName = SeoExtensions.GetSeName(forumTopic?.Subject ?? "", false, false),
            ForumEditor = forumSettings.ForumEditor,
            IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer)
        };

        if (model.IsCustomerAllowedToSubscribe)
        {
            var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, 0, forumPost.TopicId, 0, 1);
            model.Subscribed = subs.Any();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostEdit(EditForumPostModel model)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumPost = await forumService.GetPostByIdAsync(model.Id);
        if (forumPost == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToEditPostAsync(customer, forumPost))
            return Challenge();

        var forumTopic = await forumService.GetTopicByIdAsync(forumPost.TopicId);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        if (ModelState.IsValid)
        {
            try
            {
                var text = TruncateText(model.Text, forumSettings.PostMaxLength);
                var nowUtc = DateTime.UtcNow;

                forumPost.Text = text;
                forumPost.UpdatedOnUtc = nowUtc;
                await forumService.UpdatePostAsync(forumPost);

                await ManageSubscriptionAsync(customer, 0, forumPost.TopicId, model.Subscribed);

                var pageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10;
                var pageIndex = await forumService.CalculateTopicPageIndexAsync(forumPost.TopicId, pageSize, forumPost.Id);

                var url = pageIndex > 0
                    ? Url.Action(nameof(Topic), new { id = forumPost.TopicId, page = pageIndex + 1 })
                    : Url.Action(nameof(Topic), new { id = forumPost.TopicId });

                return Redirect($"{url}#{forumPost.Id}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        var forum2 = await forumService.GetForumByIdAsync(forumTopic.ForumId);
        model.IsEdit = true;
        model.ForumName = forum2?.Name;
        model.ForumTopicSubject = forumTopic.Subject;
        model.ForumTopicSeName = SeoExtensions.GetSeName(forumTopic.Subject, false, false);
        model.ForumEditor = forumSettings.ForumEditor;
        model.IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer);
        return View(model);
    }

    // --- Post Delete (AJAX) ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostDelete(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return Json(new { redirect = Url.Action("Index", "Home") });

        var forumPost = await forumService.GetPostByIdAsync(id);
        if (forumPost == null)
            return Json(new { redirect = Url.Action(nameof(Index)) });

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToDeletePostAsync(customer, forumPost))
            return Forbid();

        var topicId = forumPost.TopicId;
        var forumTopic = await forumService.GetTopicByIdAsync(topicId);
        var forumId = forumTopic?.ForumId ?? 0;

        await forumService.DeletePostAsync(forumPost);

        // topic may have been deleted if this was the only post
        forumTopic = await forumService.GetTopicByIdAsync(topicId);
        if (forumTopic == null)
            return Json(new { redirect = Url.Action(nameof(Forum), new { id = forumId }) });

        return Json(new { redirect = Url.Action(nameof(Topic), new { id = forumTopic.Id }) });
    }

    // --- Search ---

    public async Task<IActionResult> Search(string? searchterms, bool? adv, string? forumId,
        string? within, string? limitDays, int page = 1)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var model = new ForumSearchModel
        {
            ShowAdvancedSearch = adv ?? false,
            SearchTerms = searchterms,
            ForumList = [new SelectListItem { Text = await localizationService.GetResourceAsync("Forum.SearchAllForums"), Value = "0" }],
            LimitList = PrepareLimitDaysList(),
            WithinList = PrepareWithinList(),
            PostsPageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10,
            AllowPostVoting = forumSettings.AllowPostVoting
        };

        // populate forum dropdown
        var groups = await forumService.GetAllForumGroupsAsync();
        foreach (var group in groups)
        {
            var forums = await forumService.GetAllForumsByGroupIdAsync(group.Id);
            foreach (var f in forums)
                model.ForumList.Add(new SelectListItem { Text = $"{group.Name} >> {f.Name}", Value = f.Id.ToString() });
        }

        int.TryParse(forumId, out var forumIdParsed);
        int.TryParse(within, out var withinParsed);
        int.TryParse(limitDays, out var limitDaysParsed);

        model.ForumIdSelected = forumIdParsed;
        model.WithinSelected = withinParsed;
        model.LimitDaysSelected = limitDaysParsed;

        if (!string.IsNullOrWhiteSpace(searchterms))
        {
            var minLength = forumSettings.ForumSearchTermMinimumLength;
            if (searchterms.Trim().Length < minLength)
            {
                model.Error = string.Format(
                    await localizationService.GetResourceAsync("Forum.SearchTermMinimumLengthIsNCharacters"),
                    minLength);
            }
            else
            {
                var searchType = (ForumSearchType)withinParsed;
                var pageSize = forumSettings.SearchResultsPageSize > 0 ? forumSettings.SearchResultsPageSize : 25;

                var topics = await forumService.GetAllTopicsAsync(forumIdParsed,
                    keywords: searchterms, searchType: searchType,
                    limitDays: limitDaysParsed, pageIndex: page - 1, pageSize: pageSize);

                model.TopicPageSize = topics.PageSize;
                model.TopicTotalRecords = topics.TotalCount;
                model.TopicPageIndex = topics.PageIndex;

                foreach (var topic in topics)
                    model.ForumTopics.Add(PrepareForumTopicRowModel(topic));

                model.SearchResultsVisible = topics.Any();
                model.NoResultsVisible = !topics.Any();
            }
        }

        return View(model);
    }

    // --- Customer Forum Subscriptions ---

    public async Task<IActionResult> CustomerForumSubscriptions(int? page)
    {
        if (!forumSettings.AllowCustomersToManageSubscriptions)
            return RedirectToAction("Info", "Customer");

        var customer = workContext.CurrentCustomer;
        var pageSize = forumSettings.ForumSubscriptionsPageSize > 0 ? forumSettings.ForumSubscriptionsPageSize : 10;
        var pageIndex = (page ?? 1) - 1;

        var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, pageIndex: pageIndex, pageSize: pageSize);

        var model = new CustomerForumSubscriptionsModel
        {
            PageIndex = subs.PageIndex,
            PageSize = subs.PageSize,
            TotalRecords = subs.TotalCount
        };

        foreach (var sub in subs)
        {
            var subModel = new CustomerForumSubscriptionsModel.ForumSubscriptionModel
            {
                Id = sub.Id,
                ForumId = sub.ForumId,
                ForumTopicId = sub.TopicId
            };

            if (sub.TopicId > 0)
            {
                var topic = await forumService.GetTopicByIdAsync(sub.TopicId);
                if (topic != null)
                {
                    subModel.TopicSubscription = true;
                    subModel.Title = topic.Subject;
                    subModel.Slug = SeoExtensions.GetSeName(topic.Subject, false, false);
                }
            }
            else if (sub.ForumId > 0)
            {
                var forum = await forumService.GetForumByIdAsync(sub.ForumId);
                if (forum != null)
                {
                    subModel.Title = forum.Name;
                    subModel.Slug = SeoExtensions.GetSeName(forum.Name, false, false);
                }
            }

            model.ForumSubscriptions.Add(subModel);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerForumSubscriptionsDelete([FromForm] IEnumerable<int> subscriptionIds)
    {
        var customer = workContext.CurrentCustomer;

        foreach (var subId in subscriptionIds)
        {
            var sub = await forumService.GetSubscriptionByIdAsync(subId);
            if (sub != null && sub.CustomerId == customer.Id)
                await forumService.DeleteSubscriptionAsync(sub);
        }

        return RedirectToAction(nameof(CustomerForumSubscriptions));
    }

    // --- Search helpers ---

    private static List<SelectListItem> PrepareLimitDaysList()
    {
        return
        [
            new SelectListItem { Text = "All", Value = "0" },
            new SelectListItem { Text = "1 day", Value = "1" },
            new SelectListItem { Text = "7 days", Value = "7" },
            new SelectListItem { Text = "2 weeks", Value = "14" },
            new SelectListItem { Text = "1 month", Value = "30" },
            new SelectListItem { Text = "3 months", Value = "92" },
            new SelectListItem { Text = "6 months", Value = "183" },
            new SelectListItem { Text = "1 year", Value = "365" }
        ];
    }

    private static List<SelectListItem> PrepareWithinList()
    {
        return
        [
            new SelectListItem { Text = "All", Value = ((int)ForumSearchType.All).ToString() },
            new SelectListItem { Text = "Topic titles only", Value = ((int)ForumSearchType.TopicTitlesOnly).ToString() },
            new SelectListItem { Text = "Post text only", Value = ((int)ForumSearchType.PostTextOnly).ToString() }
        ];
    }
}
