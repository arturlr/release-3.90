using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Messages;

namespace Nop.Services.Forums;

public partial class ForumService(
    IStaticCacheManager cacheManager,
    IRepository<ForumGroup> forumGroupRepository,
    IRepository<Forum> forumRepository,
    IRepository<ForumTopic> forumTopicRepository,
    IRepository<ForumPost> forumPostRepository,
    IRepository<ForumPostVote> forumPostVoteRepository,
    IRepository<PrivateMessage> privateMessageRepository,
    IRepository<ForumSubscription> forumSubscriptionRepository,
    IRepository<Customer> customerRepository,
    ForumSettings forumSettings,
    IGenericAttributeService genericAttributeService,
    ICustomerService customerService,
    IWorkflowMessageService workflowMessageService,
    IEventPublisher eventPublisher) : IForumService
{
    private const string ForumGroupAllKey = "Nop.forumgroup.all";
    private const string ForumAllByGroupKey = "Nop.forum.allbyforumgroupid-{0}";
    private const string ForumGroupPrefix = "Nop.forumgroup.";
    private const string ForumPrefix = "Nop.forum.";

    #region Utilities

    private async Task UpdateForumStatsAsync(int forumId)
    {
        if (forumId == 0) return;
        var forum = await GetForumByIdAsync(forumId);
        if (forum is null) return;

        forum.NumTopics = forumTopicRepository.Table.Count(ft => ft.ForumId == forumId);

        forum.NumPosts = (from ft in forumTopicRepository.Table
                          join fp in forumPostRepository.Table on ft.Id equals fp.TopicId
                          where ft.ForumId == forumId
                          select fp.Id).Count();

        var lastValues = (from ft in forumTopicRepository.Table
                          join fp in forumPostRepository.Table on ft.Id equals fp.TopicId
                          where ft.ForumId == forumId
                          orderby fp.CreatedOnUtc descending, ft.CreatedOnUtc descending
                          select new { ft.Id, PostId = fp.Id, fp.CustomerId, fp.CreatedOnUtc })
                         .FirstOrDefault();

        if (lastValues is not null)
        {
            forum.LastTopicId = lastValues.Id;
            forum.LastPostId = lastValues.PostId;
            forum.LastPostCustomerId = lastValues.CustomerId;
            forum.LastPostTime = lastValues.CreatedOnUtc;
        }
        else
        {
            forum.LastTopicId = 0;
            forum.LastPostId = 0;
            forum.LastPostCustomerId = 0;
            forum.LastPostTime = null;
        }

        await UpdateForumAsync(forum);
    }

    private async Task UpdateForumTopicStatsAsync(int forumTopicId)
    {
        if (forumTopicId == 0) return;
        var topic = await GetTopicByIdAsync(forumTopicId);
        if (topic is null) return;

        topic.NumPosts = forumPostRepository.Table.Count(fp => fp.TopicId == forumTopicId);

        var lastValues = (from fp in forumPostRepository.Table
                          where fp.TopicId == forumTopicId
                          orderby fp.CreatedOnUtc descending
                          select new { fp.Id, fp.CustomerId, fp.CreatedOnUtc })
                         .FirstOrDefault();

        if (lastValues is not null)
        {
            topic.LastPostId = lastValues.Id;
            topic.LastPostCustomerId = lastValues.CustomerId;
            topic.LastPostTime = lastValues.CreatedOnUtc;
        }
        else
        {
            topic.LastPostId = 0;
            topic.LastPostCustomerId = 0;
            topic.LastPostTime = null;
        }

        await UpdateTopicAsync(topic);
    }

    private async Task UpdateCustomerStatsAsync(int customerId)
    {
        if (customerId == 0) return;
        var customer = await customerService.GetCustomerByIdAsync(customerId);
        if (customer is null) return;

        var numPosts = forumPostRepository.Table.Count(fp => fp.CustomerId == customerId);
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.ForumPostCount, numPosts);
    }

    private async Task<bool> IsGuestAsync(Customer customer)
    {
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole is null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(guestRole.Id);
    }

    private async Task<bool> IsForumModeratorAsync(Customer customer)
    {
        var modRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.ForumModerators);
        if (modRole is null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(modRole.Id);
    }

    private void InvalidateForumCache()
    {
        cacheManager.RemoveByPrefixAsync(ForumGroupPrefix).GetAwaiter().GetResult();
        cacheManager.RemoveByPrefixAsync(ForumPrefix).GetAwaiter().GetResult();
    }

    #endregion
}
