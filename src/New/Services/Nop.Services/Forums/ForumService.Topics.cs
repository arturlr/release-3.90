using Nop.Core;
using Nop.Core.Domain.Forums;
using Nop.Services.Events;

namespace Nop.Services.Forums;

public partial class ForumService
{
    #region ForumTopic

    public virtual async Task DeleteTopicAsync(ForumTopic forumTopic)
    {
        ArgumentNullException.ThrowIfNull(forumTopic);

        int customerId = forumTopic.CustomerId;
        int forumId = forumTopic.ForumId;

        forumTopicRepository.Delete(forumTopic);

        // delete topic subscriptions
        var subs = forumSubscriptionRepository.Table.Where(fs => fs.TopicId == forumTopic.Id).ToList();
        foreach (var fs in subs)
        {
            forumSubscriptionRepository.Delete(fs);
            await eventPublisher.EntityDeletedAsync(fs);
        }

        await UpdateForumStatsAsync(forumId);
        await UpdateCustomerStatsAsync(customerId);
        InvalidateForumCache();
        await eventPublisher.EntityDeletedAsync(forumTopic);
    }

    public virtual async Task<ForumTopic?> GetTopicByIdAsync(int forumTopicId, bool increaseViews = false)
    {
        if (forumTopicId == 0) return null;
        var topic = forumTopicRepository.GetById(forumTopicId);
        if (topic is null) return null;

        if (increaseViews)
        {
            topic.Views++;
            await UpdateTopicAsync(topic);
        }

        return topic;
    }

    public virtual Task<IPagedList<ForumTopic>> GetAllTopicsAsync(int forumId = 0, int customerId = 0,
        string keywords = "", ForumSearchType searchType = ForumSearchType.All,
        int limitDays = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        DateTime? limitDate = limitDays > 0 ? DateTime.UtcNow.AddDays(-limitDays) : null;
        bool searchKeywords = !string.IsNullOrEmpty(keywords);
        bool searchTopicTitles = searchType is ForumSearchType.All or ForumSearchType.TopicTitlesOnly;
        bool searchPostText = searchType is ForumSearchType.All or ForumSearchType.PostTextOnly;

        var query1 = from ft in forumTopicRepository.Table
                     join fp in forumPostRepository.Table on ft.Id equals fp.TopicId
                     where
                         (forumId == 0 || ft.ForumId == forumId) &&
                         (customerId == 0 || ft.CustomerId == customerId) &&
                         (!searchKeywords ||
                             (searchTopicTitles && ft.Subject!.Contains(keywords)) ||
                             (searchPostText && fp.Text!.Contains(keywords))) &&
                         (!limitDate.HasValue || limitDate.Value <= ft.LastPostTime)
                     select ft.Id;

        var query2 = from ft in forumTopicRepository.Table
                     where query1.Contains(ft.Id)
                     orderby ft.TopicTypeId descending, ft.LastPostTime descending, ft.Id descending
                     select ft;

        return Task.FromResult<IPagedList<ForumTopic>>(new PagedList<ForumTopic>(query2, pageIndex, pageSize));
    }

    public virtual Task<IPagedList<ForumTopic>> GetActiveTopicsAsync(int forumId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from ft in forumTopicRepository.Table
                    where (forumId == 0 || ft.ForumId == forumId) && ft.LastPostTime.HasValue
                    orderby ft.LastPostTime descending
                    select ft;

        return Task.FromResult<IPagedList<ForumTopic>>(new PagedList<ForumTopic>(query, pageIndex, pageSize));
    }

    public virtual async Task InsertTopicAsync(ForumTopic forumTopic, bool sendNotifications)
    {
        ArgumentNullException.ThrowIfNull(forumTopic);
        forumTopicRepository.Insert(forumTopic);

        await UpdateForumStatsAsync(forumTopic.ForumId);
        InvalidateForumCache();
        await eventPublisher.EntityInsertedAsync(forumTopic);

        if (sendNotifications)
        {
            var forum = await GetForumByIdAsync(forumTopic.ForumId);
            if (forum is not null)
            {
                var subscriptions = await GetAllSubscriptionsAsync(forumId: forum.Id);
                foreach (var sub in subscriptions)
                {
                    if (sub.CustomerId == forumTopic.CustomerId) continue;
                    var subscriber = await customerService.GetCustomerByIdAsync(sub.CustomerId);
                    if (subscriber is not null && !string.IsNullOrEmpty(subscriber.Email))
                    {
                        await workflowMessageService.SendNewForumTopicMessageAsync(subscriber, forumTopic, forum, 0);
                    }
                }
            }
        }
    }

    public virtual async Task UpdateTopicAsync(ForumTopic forumTopic)
    {
        ArgumentNullException.ThrowIfNull(forumTopic);
        forumTopicRepository.Update(forumTopic);
        InvalidateForumCache();
        await eventPublisher.EntityUpdatedAsync(forumTopic);
    }

    public virtual async Task<ForumTopic?> MoveTopicAsync(int forumTopicId, int newForumId)
    {
        var topic = await GetTopicByIdAsync(forumTopicId);
        if (topic is null) return null;

        var newForum = await GetForumByIdAsync(newForumId);
        if (newForum is null) return topic;

        int previousForumId = topic.ForumId;
        if (previousForumId != newForumId)
        {
            topic.ForumId = newForum.Id;
            topic.UpdatedOnUtc = DateTime.UtcNow;
            await UpdateTopicAsync(topic);

            await UpdateForumStatsAsync(previousForumId);
            await UpdateForumStatsAsync(newForumId);
        }

        return topic;
    }

    #endregion

    #region ForumPost

    public virtual async Task DeletePostAsync(ForumPost forumPost)
    {
        ArgumentNullException.ThrowIfNull(forumPost);

        int forumTopicId = forumPost.TopicId;
        int customerId = forumPost.CustomerId;
        var topic = await GetTopicByIdAsync(forumTopicId);
        int forumId = topic?.ForumId ?? 0;

        // check if this is the first post — if so, delete the entire topic
        bool deleteTopic = false;
        if (topic is not null)
        {
            var firstPost = forumPostRepository.Table
                .Where(fp => fp.TopicId == forumTopicId)
                .OrderBy(fp => fp.CreatedOnUtc)
                .ThenBy(fp => fp.Id)
                .FirstOrDefault();
            if (firstPost is not null && firstPost.Id == forumPost.Id)
                deleteTopic = true;
        }

        forumPostRepository.Delete(forumPost);

        if (deleteTopic && topic is not null)
        {
            await DeleteTopicAsync(topic);
        }

        if (!deleteTopic)
        {
            await UpdateForumTopicStatsAsync(forumTopicId);
        }
        await UpdateForumStatsAsync(forumId);
        await UpdateCustomerStatsAsync(customerId);
        InvalidateForumCache();
        await eventPublisher.EntityDeletedAsync(forumPost);
    }

    public virtual Task<ForumPost?> GetPostByIdAsync(int forumPostId)
    {
        if (forumPostId == 0) return Task.FromResult<ForumPost?>(null);
        return Task.FromResult<ForumPost?>(forumPostRepository.GetById(forumPostId));
    }

    public virtual Task<IPagedList<ForumPost>> GetAllPostsAsync(int forumTopicId = 0, int customerId = 0,
        string keywords = "", bool ascSort = false,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = forumPostRepository.Table;
        if (forumTopicId > 0)
            query = query.Where(fp => fp.TopicId == forumTopicId);
        if (customerId > 0)
            query = query.Where(fp => fp.CustomerId == customerId);
        if (!string.IsNullOrEmpty(keywords))
            query = query.Where(fp => fp.Text!.Contains(keywords));

        query = ascSort
            ? query.OrderBy(fp => fp.CreatedOnUtc).ThenBy(fp => fp.Id)
            : query.OrderByDescending(fp => fp.CreatedOnUtc).ThenBy(fp => fp.Id);

        return Task.FromResult<IPagedList<ForumPost>>(new PagedList<ForumPost>(query, pageIndex, pageSize));
    }

    public virtual async Task InsertPostAsync(ForumPost forumPost, bool sendNotifications)
    {
        ArgumentNullException.ThrowIfNull(forumPost);
        forumPostRepository.Insert(forumPost);

        var topic = await GetTopicByIdAsync(forumPost.TopicId);
        int forumId = topic?.ForumId ?? 0;

        await UpdateForumTopicStatsAsync(forumPost.TopicId);
        await UpdateForumStatsAsync(forumId);
        await UpdateCustomerStatsAsync(forumPost.CustomerId);
        InvalidateForumCache();
        await eventPublisher.EntityInsertedAsync(forumPost);

        if (sendNotifications && topic is not null)
        {
            var forum = await GetForumByIdAsync(forumId);
            var subscriptions = await GetAllSubscriptionsAsync(topicId: topic.Id);
            int friendlyPageIndex = await CalculateTopicPageIndexAsync(forumPost.TopicId,
                forumSettings.PostsPageSize > 0 ? forumSettings.PostsPageSize : 10,
                forumPost.Id) + 1;

            foreach (var sub in subscriptions)
            {
                if (sub.CustomerId == forumPost.CustomerId) continue;
                var subscriber = await customerService.GetCustomerByIdAsync(sub.CustomerId);
                if (subscriber is not null && !string.IsNullOrEmpty(subscriber.Email) && forum is not null)
                {
                    await workflowMessageService.SendNewForumPostMessageAsync(subscriber, forumPost,
                        topic, forum, friendlyPageIndex, 0);
                }
            }
        }
    }

    public virtual async Task UpdatePostAsync(ForumPost forumPost)
    {
        ArgumentNullException.ThrowIfNull(forumPost);
        forumPostRepository.Update(forumPost);
        InvalidateForumCache();
        await eventPublisher.EntityUpdatedAsync(forumPost);
    }

    #endregion
}
