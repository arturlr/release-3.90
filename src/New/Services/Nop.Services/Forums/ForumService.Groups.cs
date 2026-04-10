using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Forums;
using Nop.Services.Events;

namespace Nop.Services.Forums;

public partial class ForumService
{
    #region ForumGroup

    public virtual async Task DeleteForumGroupAsync(ForumGroup forumGroup)
    {
        ArgumentNullException.ThrowIfNull(forumGroup);
        forumGroupRepository.Delete(forumGroup);
        InvalidateForumCache();
        await eventPublisher.EntityDeletedAsync(forumGroup);
    }

    public virtual Task<ForumGroup?> GetForumGroupByIdAsync(int forumGroupId)
    {
        if (forumGroupId == 0) return Task.FromResult<ForumGroup?>(null);
        return Task.FromResult<ForumGroup?>(forumGroupRepository.GetById(forumGroupId));
    }

    public virtual async Task<IList<ForumGroup>> GetAllForumGroupsAsync()
    {
        return await cacheManager.GetAsync(new CacheKey(ForumGroupAllKey, ForumGroupPrefix), () =>
        {
            var query = from fg in forumGroupRepository.Table
                        orderby fg.DisplayOrder, fg.Id
                        select fg;
            return Task.FromResult<IList<ForumGroup>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task InsertForumGroupAsync(ForumGroup forumGroup)
    {
        ArgumentNullException.ThrowIfNull(forumGroup);
        forumGroupRepository.Insert(forumGroup);
        InvalidateForumCache();
        await eventPublisher.EntityInsertedAsync(forumGroup);
    }

    public virtual async Task UpdateForumGroupAsync(ForumGroup forumGroup)
    {
        ArgumentNullException.ThrowIfNull(forumGroup);
        forumGroupRepository.Update(forumGroup);
        InvalidateForumCache();
        await eventPublisher.EntityUpdatedAsync(forumGroup);
    }

    #endregion

    #region Forum

    public virtual async Task DeleteForumAsync(Forum forum)
    {
        ArgumentNullException.ThrowIfNull(forum);

        // delete topic subscriptions
        var topicIds = forumTopicRepository.Table.Where(ft => ft.ForumId == forum.Id).Select(ft => ft.Id);
        var topicSubs = forumSubscriptionRepository.Table.Where(fs => topicIds.Contains(fs.TopicId)).ToList();
        foreach (var fs in topicSubs)
        {
            forumSubscriptionRepository.Delete(fs);
            await eventPublisher.EntityDeletedAsync(fs);
        }

        // delete forum subscriptions
        var forumSubs = forumSubscriptionRepository.Table.Where(fs => fs.ForumId == forum.Id).ToList();
        foreach (var fs in forumSubs)
        {
            forumSubscriptionRepository.Delete(fs);
            await eventPublisher.EntityDeletedAsync(fs);
        }

        forumRepository.Delete(forum);
        InvalidateForumCache();
        await eventPublisher.EntityDeletedAsync(forum);
    }

    public virtual Task<Forum?> GetForumByIdAsync(int forumId)
    {
        if (forumId == 0) return Task.FromResult<Forum?>(null);
        return Task.FromResult<Forum?>(forumRepository.GetById(forumId));
    }

    public virtual async Task<IList<Forum>> GetAllForumsByGroupIdAsync(int forumGroupId)
    {
        var key = new CacheKey(string.Format(ForumAllByGroupKey, forumGroupId), ForumPrefix);
        return await cacheManager.GetAsync(key, () =>
        {
            var query = from f in forumRepository.Table
                        where f.ForumGroupId == forumGroupId
                        orderby f.DisplayOrder, f.Id
                        select f;
            return Task.FromResult<IList<Forum>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task InsertForumAsync(Forum forum)
    {
        ArgumentNullException.ThrowIfNull(forum);
        forumRepository.Insert(forum);
        InvalidateForumCache();
        await eventPublisher.EntityInsertedAsync(forum);
    }

    public virtual async Task UpdateForumAsync(Forum forum)
    {
        ArgumentNullException.ThrowIfNull(forum);
        forumRepository.Update(forum);
        InvalidateForumCache();
        await eventPublisher.EntityUpdatedAsync(forum);
    }

    #endregion
}
