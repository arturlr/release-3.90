using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Services.Seo;
using Nop.Web.Models.Boards;

namespace Nop.Web.Controllers;

public partial class BoardsController
{
    // --- Topic (view) ---

    public async Task<IActionResult> Topic(int id, int page = 1)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(id, increaseViews: true);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        var pageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10;
        var posts = await forumService.GetAllPostsAsync(forumTopic.Id, ascSort: true,
            pageIndex: page - 1, pageSize: pageSize);

        if (!posts.Any() && page > 1)
            return RedirectToAction(nameof(Topic), new { id = forumTopic.Id });

        var customer = workContext.CurrentCustomer;
        var topicSeName = SeoExtensions.GetSeName(forumTopic.Subject, false, false);

        var isSubscribed = false;
        if (await forumService.IsCustomerAllowedToSubscribeAsync(customer))
        {
            var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, 0, forumTopic.Id, 0, 1);
            isSubscribed = subs.Any();
        }

        var model = new ForumTopicPageModel
        {
            Id = forumTopic.Id,
            Subject = forumTopic.Subject,
            SeName = topicSeName,
            IsCustomerAllowedToEditTopic = await forumService.IsCustomerAllowedToEditTopicAsync(customer, forumTopic),
            IsCustomerAllowedToDeleteTopic = await forumService.IsCustomerAllowedToDeleteTopicAsync(customer, forumTopic),
            IsCustomerAllowedToMoveTopic = await forumService.IsCustomerAllowedToMoveTopicAsync(customer, forumTopic),
            IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer),
            WatchTopicText = isSubscribed
                ? await localizationService.GetResourceAsync("Forum.UnwatchTopic")
                : await localizationService.GetResourceAsync("Forum.WatchTopic"),
            PostsPageIndex = posts.PageIndex,
            PostsPageSize = posts.PageSize,
            PostsTotalRecords = posts.TotalCount
        };

        foreach (var post in posts)
        {
            model.ForumPostModels.Add(new ForumPostModel
            {
                Id = post.Id,
                ForumTopicId = forumTopic.Id,
                ForumTopicSeName = topicSeName,
                FormattedText = FormatPostText(post.Text),
                IsCurrentCustomerAllowedToEditPost = await forumService.IsCustomerAllowedToEditPostAsync(customer, post),
                IsCurrentCustomerAllowedToDeletePost = await forumService.IsCustomerAllowedToDeletePostAsync(customer, post),
                CustomerId = post.CustomerId,
                PostCreatedOnStr = post.CreatedOnUtc.ToString("f"),
                ShowCustomersPostCount = forumSettings.ShowCustomersPostCount,
                AllowPostVoting = forumSettings.AllowPostVoting,
                VoteCount = post.VoteCount,
                CurrentTopicPage = page
            });
        }

        return View(model);
    }

    // --- Topic Create ---

    public async Task<IActionResult> TopicCreate(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forum = await forumService.GetForumByIdAsync(id);
        if (forum == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToCreateTopicAsync(customer, forum))
            return Challenge();

        var model = new EditForumTopicModel
        {
            ForumId = forum.Id,
            ForumName = forum.Name,
            ForumSeName = SeoExtensions.GetSeName(forum.Name, false, false),
            ForumEditor = forumSettings.ForumEditor,
            IsCustomerAllowedToSetTopicPriority = await forumService.IsCustomerAllowedToSetTopicPriorityAsync(customer),
            TopicPriorities = PrepareTopicPriorities(),
            IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer),
            Subscribed = false
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopicCreate(EditForumTopicModel model)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forum = await forumService.GetForumByIdAsync(model.ForumId);
        if (forum == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToCreateTopicAsync(customer, forum))
            return Challenge();

        if (ModelState.IsValid)
        {
            try
            {
                var subject = TruncateText(model.Subject, forumSettings.TopicSubjectMaxLength);
                var text = TruncateText(model.Text, forumSettings.PostMaxLength);

                var topicType = ForumTopicType.Normal;
                if (await forumService.IsCustomerAllowedToSetTopicPriorityAsync(customer))
                    topicType = (ForumTopicType)model.TopicTypeId;

                var nowUtc = DateTime.UtcNow;
                var forumTopic = new ForumTopic
                {
                    ForumId = forum.Id,
                    CustomerId = customer.Id,
                    TopicTypeId = (int)topicType,
                    Subject = subject,
                    CreatedOnUtc = nowUtc,
                    UpdatedOnUtc = nowUtc
                };
                await forumService.InsertTopicAsync(forumTopic, true);

                var forumPost = new ForumPost
                {
                    TopicId = forumTopic.Id,
                    CustomerId = customer.Id,
                    Text = text,
                    IPAddress = webHelper.GetCurrentIpAddress(),
                    CreatedOnUtc = nowUtc,
                    UpdatedOnUtc = nowUtc
                };
                await forumService.InsertPostAsync(forumPost, false);

                forumTopic.NumPosts = 1;
                forumTopic.LastPostId = forumPost.Id;
                forumTopic.LastPostCustomerId = forumPost.CustomerId;
                forumTopic.LastPostTime = forumPost.CreatedOnUtc;
                forumTopic.UpdatedOnUtc = nowUtc;
                await forumService.UpdateTopicAsync(forumTopic);

                if (await forumService.IsCustomerAllowedToSubscribeAsync(customer) && model.Subscribed)
                {
                    await forumService.InsertSubscriptionAsync(new ForumSubscription
                    {
                        SubscriptionGuid = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        TopicId = forumTopic.Id,
                        CreatedOnUtc = nowUtc
                    });
                }

                return RedirectToAction(nameof(Topic), new { id = forumTopic.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        model.ForumName = forum.Name;
        model.ForumSeName = SeoExtensions.GetSeName(forum.Name, false, false);
        model.ForumEditor = forumSettings.ForumEditor;
        model.IsCustomerAllowedToSetTopicPriority = await forumService.IsCustomerAllowedToSetTopicPriorityAsync(customer);
        model.TopicPriorities = PrepareTopicPriorities();
        model.IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer);
        return View(model);
    }

    // --- Topic Edit ---

    public async Task<IActionResult> TopicEdit(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(id);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToEditTopicAsync(customer, forumTopic))
            return Challenge();

        var forum = await forumService.GetForumByIdAsync(forumTopic.ForumId);
        var firstPost = (await forumService.GetAllPostsAsync(forumTopic.Id, ascSort: true, pageSize: 1)).FirstOrDefault();

        var model = new EditForumTopicModel
        {
            IsEdit = true,
            Id = forumTopic.Id,
            ForumId = forumTopic.ForumId,
            ForumName = forum?.Name,
            ForumSeName = SeoExtensions.GetSeName(forum?.Name ?? "", false, false),
            TopicTypeId = forumTopic.TopicTypeId,
            Subject = forumTopic.Subject,
            Text = firstPost?.Text,
            ForumEditor = forumSettings.ForumEditor,
            IsCustomerAllowedToSetTopicPriority = await forumService.IsCustomerAllowedToSetTopicPriorityAsync(customer),
            TopicPriorities = PrepareTopicPriorities(),
            IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer)
        };

        if (model.IsCustomerAllowedToSubscribe)
        {
            var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, 0, forumTopic.Id, 0, 1);
            model.Subscribed = subs.Any();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopicEdit(EditForumTopicModel model)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(model.Id);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToEditTopicAsync(customer, forumTopic))
            return Challenge();

        if (ModelState.IsValid)
        {
            try
            {
                var subject = TruncateText(model.Subject, forumSettings.TopicSubjectMaxLength);
                var text = TruncateText(model.Text, forumSettings.PostMaxLength);
                var nowUtc = DateTime.UtcNow;

                if (await forumService.IsCustomerAllowedToSetTopicPriorityAsync(customer))
                    forumTopic.TopicTypeId = model.TopicTypeId;

                forumTopic.Subject = subject;
                forumTopic.UpdatedOnUtc = nowUtc;
                await forumService.UpdateTopicAsync(forumTopic);

                var firstPost = (await forumService.GetAllPostsAsync(forumTopic.Id, ascSort: true, pageSize: 1)).FirstOrDefault();
                if (firstPost != null)
                {
                    firstPost.Text = text;
                    firstPost.UpdatedOnUtc = nowUtc;
                    await forumService.UpdatePostAsync(firstPost);
                }

                await ManageSubscriptionAsync(customer, 0, forumTopic.Id, model.Subscribed);

                return RedirectToAction(nameof(Topic), new { id = forumTopic.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        var forum = await forumService.GetForumByIdAsync(forumTopic.ForumId);
        model.IsEdit = true;
        model.ForumName = forum?.Name;
        model.ForumSeName = SeoExtensions.GetSeName(forum?.Name ?? "", false, false);
        model.ForumEditor = forumSettings.ForumEditor;
        model.IsCustomerAllowedToSetTopicPriority = await forumService.IsCustomerAllowedToSetTopicPriorityAsync(customer);
        model.TopicPriorities = PrepareTopicPriorities();
        model.IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer);
        return View(model);
    }

    // --- Topic Delete (AJAX) ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopicDelete(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return Json(new { redirect = Url.Action("Index", "Home") });

        var forumTopic = await forumService.GetTopicByIdAsync(id);
        if (forumTopic == null)
            return Json(new { redirect = Url.Action(nameof(Index)) });

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToDeleteTopicAsync(customer, forumTopic))
            return Forbid();

        var forumId = forumTopic.ForumId;
        await forumService.DeleteTopicAsync(forumTopic);

        var forum = await forumService.GetForumByIdAsync(forumId);
        if (forum != null)
            return Json(new { redirect = Url.Action(nameof(Forum), new { id = forum.Id }) });

        return Json(new { redirect = Url.Action(nameof(Index)) });
    }

    // --- Topic Move ---

    public async Task<IActionResult> TopicMove(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(id);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        var model = new TopicMoveModel
        {
            Id = forumTopic.Id,
            TopicSeName = SeoExtensions.GetSeName(forumTopic.Subject, false, false),
            ForumList = await PrepareForumListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopicMove(TopicMoveModel model)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumTopic = await forumService.GetTopicByIdAsync(model.Id);
        if (forumTopic == null)
            return RedirectToAction(nameof(Index));

        if (forumTopic.ForumId != model.ForumSelected)
        {
            var customer = workContext.CurrentCustomer;
            if (await forumService.IsCustomerAllowedToMoveTopicAsync(customer, forumTopic))
                await forumService.MoveTopicAsync(forumTopic.Id, model.ForumSelected);
        }

        return RedirectToAction(nameof(Topic), new { id = forumTopic.Id });
    }

    // --- Helpers ---

    private static IEnumerable<SelectListItem> PrepareTopicPriorities()
    {
        return
        [
            new SelectListItem { Text = "Normal", Value = ((int)ForumTopicType.Normal).ToString() },
            new SelectListItem { Text = "Sticky", Value = ((int)ForumTopicType.Sticky).ToString() },
            new SelectListItem { Text = "Announcement", Value = ((int)ForumTopicType.Announcement).ToString() }
        ];
    }

    private static string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || maxLength <= 0)
            return text;
        return text.Length > maxLength ? text[..maxLength] : text;
    }

    private async Task ManageSubscriptionAsync(Customer customer, int forumId, int topicId, bool subscribe)
    {
        if (!await forumService.IsCustomerAllowedToSubscribeAsync(customer))
            return;

        var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, forumId, topicId, 0, 1);
        var existing = subs.FirstOrDefault();

        if (subscribe)
        {
            if (existing == null)
            {
                await forumService.InsertSubscriptionAsync(new ForumSubscription
                {
                    SubscriptionGuid = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    ForumId = forumId,
                    TopicId = topicId,
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
        }
        else
        {
            if (existing != null)
                await forumService.DeleteSubscriptionAsync(existing);
        }
    }

    private static string FormatPostText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return System.Net.WebUtility.HtmlEncode(text).Replace("\n", "<br />");
    }
}
