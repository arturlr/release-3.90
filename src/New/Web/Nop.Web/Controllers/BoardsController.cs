using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Services.Customers;
using Nop.Services.Forums;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Boards;

namespace Nop.Web.Controllers;

public partial class BoardsController(
    IForumService forumService,
    ILocalizationService localizationService,
    IWebHelper webHelper,
    IWorkContext workContext,
    ICustomerService customerService,
    ForumSettings forumSettings) : BasePublicController
{
    // --- Index ---

    public async Task<IActionResult> Index()
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var groups = await forumService.GetAllForumGroupsAsync();
        var model = new BoardsIndexModel();

        foreach (var group in groups)
        {
            var groupModel = new ForumGroupModel
            {
                Id = group.Id,
                Name = group.Name,
                SeName = SeoExtensions.GetSeName(group.Name, false, false)
            };

            var forums = await forumService.GetAllForumsByGroupIdAsync(group.Id);
            foreach (var f in forums)
            {
                groupModel.Forums.Add(new ForumRowModel
                {
                    Id = f.Id,
                    Name = f.Name,
                    SeName = SeoExtensions.GetSeName(f.Name, false, false),
                    Description = f.Description,
                    NumTopics = f.NumTopics,
                    NumPosts = f.NumPosts,
                    LastPostId = f.LastPostId
                });
            }

            model.ForumGroups.Add(groupModel);
        }

        return View(model);
    }

    // --- Active Discussions ---

    public async Task<IActionResult> ActiveDiscussions(int forumId = 0, int page = 1)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var pageSize = forumSettings.ActiveDiscussionsPageSize > 0
            ? forumSettings.ActiveDiscussionsPageSize : 25;
        var topics = await forumService.GetActiveTopicsAsync(forumId, page - 1, pageSize);

        var model = new ActiveDiscussionsModel
        {
            TopicPageSize = topics.PageSize,
            TopicTotalRecords = topics.TotalCount,
            TopicPageIndex = topics.PageIndex,
            ActiveDiscussionsFeedEnabled = forumSettings.ActiveDiscussionsFeedEnabled,
            PostsPageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10,
            AllowPostVoting = forumSettings.AllowPostVoting
        };

        foreach (var topic in topics)
            model.ForumTopics.Add(PrepareForumTopicRowModel(topic));

        return View(model);
    }

    // --- Forum Group ---

    public async Task<IActionResult> ForumGroup(int id)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forumGroup = await forumService.GetForumGroupByIdAsync(id);
        if (forumGroup == null)
            return RedirectToAction(nameof(Index));

        var model = new ForumGroupModel
        {
            Id = forumGroup.Id,
            Name = forumGroup.Name,
            SeName = SeoExtensions.GetSeName(forumGroup.Name, false, false)
        };

        var forums = await forumService.GetAllForumsByGroupIdAsync(forumGroup.Id);
        foreach (var f in forums)
        {
            model.Forums.Add(new ForumRowModel
            {
                Id = f.Id,
                Name = f.Name,
                SeName = SeoExtensions.GetSeName(f.Name, false, false),
                Description = f.Description,
                NumTopics = f.NumTopics,
                NumPosts = f.NumPosts,
                LastPostId = f.LastPostId
            });
        }

        return View(model);
    }

    // --- Forum (topic list) ---

    public async Task<IActionResult> Forum(int id, int page = 1)
    {
        if (!forumSettings.ForumsEnabled)
            return RedirectToAction("Index", "Home");

        var forum = await forumService.GetForumByIdAsync(id);
        if (forum == null)
            return RedirectToAction(nameof(Index));

        var pageSize = forumSettings.TopicsPageSize > 0 ? forumSettings.TopicsPageSize : 10;
        var topics = await forumService.GetAllTopicsAsync(forum.Id, pageIndex: page - 1, pageSize: pageSize);

        var customer = workContext.CurrentCustomer;
        var isSubscribed = false;
        if (await forumService.IsCustomerAllowedToSubscribeAsync(customer))
        {
            var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, forum.Id, 0, 0, 1);
            isSubscribed = subs.Any();
        }

        var model = new ForumPageModel
        {
            Id = forum.Id,
            Name = forum.Name,
            SeName = SeoExtensions.GetSeName(forum.Name, false, false),
            Description = forum.Description,
            TopicPageSize = topics.PageSize,
            TopicTotalRecords = topics.TotalCount,
            TopicPageIndex = topics.PageIndex,
            IsCustomerAllowedToSubscribe = await forumService.IsCustomerAllowedToSubscribeAsync(customer),
            WatchForumText = isSubscribed
                ? await localizationService.GetResourceAsync("Forum.UnwatchForum")
                : await localizationService.GetResourceAsync("Forum.WatchForum"),
            ForumFeedsEnabled = forumSettings.ForumFeedsEnabled,
            PostsPageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10,
            AllowPostVoting = forumSettings.AllowPostVoting
        };

        foreach (var topic in topics)
            model.ForumTopics.Add(PrepareForumTopicRowModel(topic));

        return View(model);
    }

    // --- Forum Watch (AJAX toggle) ---

    [HttpPost]
    public async Task<IActionResult> ForumWatch(int id)
    {
        var watchText = await localizationService.GetResourceAsync("Forum.WatchForum");
        var unwatchText = await localizationService.GetResourceAsync("Forum.UnwatchForum");

        var forum = await forumService.GetForumByIdAsync(id);
        if (forum == null)
            return Json(new { Subscribed = false, Text = watchText, Error = true });

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToSubscribeAsync(customer))
            return Json(new { Subscribed = false, Text = watchText, Error = true });

        var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, forum.Id, 0, 0, 1);
        var existing = subs.FirstOrDefault();

        bool subscribed;
        string returnText;
        if (existing == null)
        {
            await forumService.InsertSubscriptionAsync(new ForumSubscription
            {
                SubscriptionGuid = Guid.NewGuid(),
                CustomerId = customer.Id,
                ForumId = forum.Id,
                CreatedOnUtc = DateTime.UtcNow
            });
            subscribed = true;
            returnText = unwatchText;
        }
        else
        {
            await forumService.DeleteSubscriptionAsync(existing);
            subscribed = false;
            returnText = watchText;
        }

        return Json(new { Subscribed = subscribed, Text = returnText, Error = false });
    }

    // --- Topic Watch (AJAX toggle) ---

    [HttpPost]
    public async Task<IActionResult> TopicWatch(int id)
    {
        var watchText = await localizationService.GetResourceAsync("Forum.WatchTopic");
        var unwatchText = await localizationService.GetResourceAsync("Forum.UnwatchTopic");

        var forumTopic = await forumService.GetTopicByIdAsync(id);
        if (forumTopic == null)
            return Json(new { Subscribed = false, Text = watchText, Error = true });

        var customer = workContext.CurrentCustomer;
        if (!await forumService.IsCustomerAllowedToSubscribeAsync(customer))
            return Json(new { Subscribed = false, Text = watchText, Error = true });

        var subs = await forumService.GetAllSubscriptionsAsync(customer.Id, 0, forumTopic.Id, 0, 1);
        var existing = subs.FirstOrDefault();

        bool subscribed;
        string returnText;
        if (existing == null)
        {
            await forumService.InsertSubscriptionAsync(new ForumSubscription
            {
                SubscriptionGuid = Guid.NewGuid(),
                CustomerId = customer.Id,
                TopicId = forumTopic.Id,
                CreatedOnUtc = DateTime.UtcNow
            });
            subscribed = true;
            returnText = unwatchText;
        }
        else
        {
            await forumService.DeleteSubscriptionAsync(existing);
            subscribed = false;
            returnText = watchText;
        }

        return Json(new { Subscribed = subscribed, Text = returnText, Error = false });
    }

    // --- Post Vote (AJAX) ---

    [HttpPost]
    public async Task<IActionResult> PostVote(int postId, bool isUp)
    {
        if (!forumSettings.AllowPostVoting)
            return Json(new { });

        var forumPost = await forumService.GetPostByIdAsync(postId);
        if (forumPost == null)
            return Json(new { });

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Json(new
            {
                Error = await localizationService.GetResourceAsync("Forum.Votes.Login"),
                VoteCount = forumPost.VoteCount
            });

        if (customer.Id == forumPost.CustomerId)
            return Json(new
            {
                Error = await localizationService.GetResourceAsync("Forum.Votes.OwnPost"),
                VoteCount = forumPost.VoteCount
            });

        var existingVote = await forumService.GetPostVoteAsync(postId, customer);
        if (existingVote != null)
        {
            if ((existingVote.IsUp && isUp) || (!existingVote.IsUp && !isUp))
                return Json(new
                {
                    Error = await localizationService.GetResourceAsync("Forum.Votes.AlreadyVoted"),
                    VoteCount = forumPost.VoteCount
                });

            await forumService.DeletePostVoteAsync(existingVote);
            return Json(new { VoteCount = forumPost.VoteCount });
        }

        if (await forumService.GetNumberOfPostVotesAsync(customer, DateTime.UtcNow.AddDays(-1)) >= forumSettings.MaxVotesPerDay)
            return Json(new
            {
                Error = string.Format(
                    await localizationService.GetResourceAsync("Forum.Votes.MaxVotesReached"),
                    forumSettings.MaxVotesPerDay),
                VoteCount = forumPost.VoteCount
            });

        await forumService.InsertPostVoteAsync(new ForumPostVote
        {
            CustomerId = customer.Id,
            ForumPostId = postId,
            IsUp = isUp,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new { VoteCount = forumPost.VoteCount, IsUp = isUp });
    }

    // --- Helpers ---

    private ForumTopicRowModel PrepareForumTopicRowModel(ForumTopic topic)
    {
        var postsPageSize = forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10;
        var totalPostPages = topic.NumPosts > 0
            ? (int)Math.Ceiling((double)topic.NumPosts / postsPageSize)
            : 1;

        return new ForumTopicRowModel
        {
            Id = topic.Id,
            Subject = topic.Subject,
            SeName = SeoExtensions.GetSeName(topic.Subject, false, false),
            LastPostId = topic.LastPostId,
            NumPosts = topic.NumPosts,
            Views = topic.Views,
            NumReplies = topic.NumPosts > 0 ? topic.NumPosts - 1 : 0,
            ForumTopicType = (ForumTopicType)topic.TopicTypeId,
            CustomerId = topic.CustomerId,
            TotalPostPages = totalPostPages,
            AllowViewingProfiles = false // deferred — requires CustomerSettings
        };
    }

    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(
            SystemCustomerRoleNames.Registered);
        if (registeredRole == null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(registeredRole.Id);
    }

    private async Task<IEnumerable<SelectListItem>> PrepareForumListAsync()
    {
        var items = new List<SelectListItem>();
        var groups = await forumService.GetAllForumGroupsAsync();
        foreach (var group in groups)
        {
            var forums = await forumService.GetAllForumsByGroupIdAsync(group.Id);
            foreach (var f in forums)
            {
                items.Add(new SelectListItem
                {
                    Text = $"{group.Name} >> {f.Name}",
                    Value = f.Id.ToString()
                });
            }
        }
        return items;
    }
}
