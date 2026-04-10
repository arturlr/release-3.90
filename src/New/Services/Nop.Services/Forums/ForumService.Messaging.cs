using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Services.Events;

namespace Nop.Services.Forums;

public partial class ForumService
{
    #region PrivateMessage

    public virtual async Task DeletePrivateMessageAsync(PrivateMessage privateMessage)
    {
        ArgumentNullException.ThrowIfNull(privateMessage);
        privateMessageRepository.Delete(privateMessage);
        await eventPublisher.EntityDeletedAsync(privateMessage);
    }

    public virtual Task<PrivateMessage?> GetPrivateMessageByIdAsync(int privateMessageId)
    {
        if (privateMessageId == 0) return Task.FromResult<PrivateMessage?>(null);
        return Task.FromResult<PrivateMessage?>(privateMessageRepository.GetById(privateMessageId));
    }

    public virtual Task<IPagedList<PrivateMessage>> GetAllPrivateMessagesAsync(int storeId, int fromCustomerId,
        int toCustomerId, bool? isRead, bool? isDeletedByAuthor, bool? isDeletedByRecipient,
        string keywords, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = privateMessageRepository.Table;
        if (storeId > 0)
            query = query.Where(pm => pm.StoreId == storeId);
        if (fromCustomerId > 0)
            query = query.Where(pm => pm.FromCustomerId == fromCustomerId);
        if (toCustomerId > 0)
            query = query.Where(pm => pm.ToCustomerId == toCustomerId);
        if (isRead.HasValue)
            query = query.Where(pm => pm.IsRead == isRead.Value);
        if (isDeletedByAuthor.HasValue)
            query = query.Where(pm => pm.IsDeletedByAuthor == isDeletedByAuthor.Value);
        if (isDeletedByRecipient.HasValue)
            query = query.Where(pm => pm.IsDeletedByRecipient == isDeletedByRecipient.Value);
        if (!string.IsNullOrEmpty(keywords))
            query = query.Where(pm => pm.Subject!.Contains(keywords) || pm.Text!.Contains(keywords));

        query = query.OrderByDescending(pm => pm.CreatedOnUtc);
        return Task.FromResult<IPagedList<PrivateMessage>>(new PagedList<PrivateMessage>(query, pageIndex, pageSize));
    }

    public virtual async Task InsertPrivateMessageAsync(PrivateMessage privateMessage)
    {
        ArgumentNullException.ThrowIfNull(privateMessage);
        privateMessageRepository.Insert(privateMessage);
        await eventPublisher.EntityInsertedAsync(privateMessage);

        var customerTo = await customerService.GetCustomerByIdAsync(privateMessage.ToCustomerId)
            ?? throw new NopException("Recipient could not be loaded");

        // UI notification flag
        await genericAttributeService.SaveAttributeAsync(customerTo,
            SystemCustomerAttributeNames.NotifiedAboutNewPrivateMessages, false, privateMessage.StoreId);

        // Email notification
        if (forumSettings.NotifyAboutPrivateMessages)
        {
            await workflowMessageService.SendPrivateMessageNotificationAsync(privateMessage, 0);
        }
    }

    public virtual async Task UpdatePrivateMessageAsync(PrivateMessage privateMessage)
    {
        ArgumentNullException.ThrowIfNull(privateMessage);

        if (privateMessage.IsDeletedByAuthor && privateMessage.IsDeletedByRecipient)
        {
            privateMessageRepository.Delete(privateMessage);
            await eventPublisher.EntityDeletedAsync(privateMessage);
        }
        else
        {
            privateMessageRepository.Update(privateMessage);
            await eventPublisher.EntityUpdatedAsync(privateMessage);
        }
    }

    #endregion

    #region ForumSubscription

    public virtual async Task DeleteSubscriptionAsync(ForumSubscription forumSubscription)
    {
        ArgumentNullException.ThrowIfNull(forumSubscription);
        forumSubscriptionRepository.Delete(forumSubscription);
        await eventPublisher.EntityDeletedAsync(forumSubscription);
    }

    public virtual Task<ForumSubscription?> GetSubscriptionByIdAsync(int forumSubscriptionId)
    {
        if (forumSubscriptionId == 0) return Task.FromResult<ForumSubscription?>(null);
        return Task.FromResult<ForumSubscription?>(forumSubscriptionRepository.GetById(forumSubscriptionId));
    }

    public virtual Task<IPagedList<ForumSubscription>> GetAllSubscriptionsAsync(int customerId = 0, int forumId = 0,
        int topicId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        // join with Customer to filter out deleted/inactive customers
        var fsQuery = from fs in forumSubscriptionRepository.Table
                      join c in customerRepository.Table on fs.CustomerId equals c.Id
                      where
                          (customerId == 0 || fs.CustomerId == customerId) &&
                          (forumId == 0 || fs.ForumId == forumId) &&
                          (topicId == 0 || fs.TopicId == topicId) &&
                          c.Active && !c.Deleted
                      select fs.SubscriptionGuid;

        var query = from fs in forumSubscriptionRepository.Table
                    where fsQuery.Contains(fs.SubscriptionGuid)
                    orderby fs.CreatedOnUtc descending, fs.SubscriptionGuid descending
                    select fs;

        return Task.FromResult<IPagedList<ForumSubscription>>(new PagedList<ForumSubscription>(query, pageIndex, pageSize));
    }

    public virtual async Task InsertSubscriptionAsync(ForumSubscription forumSubscription)
    {
        ArgumentNullException.ThrowIfNull(forumSubscription);
        forumSubscriptionRepository.Insert(forumSubscription);
        await eventPublisher.EntityInsertedAsync(forumSubscription);
    }

    public virtual async Task UpdateSubscriptionAsync(ForumSubscription forumSubscription)
    {
        ArgumentNullException.ThrowIfNull(forumSubscription);
        forumSubscriptionRepository.Update(forumSubscription);
        await eventPublisher.EntityUpdatedAsync(forumSubscription);
    }

    #endregion

    #region Permission checks

    public virtual async Task<bool> IsCustomerAllowedToCreateTopicAsync(Customer customer, Forum forum)
    {
        if (forum is null || customer is null) return false;
        if (await IsGuestAsync(customer) && !forumSettings.AllowGuestsToCreateTopics) return false;
        return true;
    }

    public virtual async Task<bool> IsCustomerAllowedToEditTopicAsync(Customer customer, ForumTopic topic)
    {
        if (topic is null || customer is null) return false;
        if (await IsGuestAsync(customer)) return false;
        if (await IsForumModeratorAsync(customer)) return true;
        return forumSettings.AllowCustomersToEditPosts && customer.Id == topic.CustomerId;
    }

    public virtual async Task<bool> IsCustomerAllowedToMoveTopicAsync(Customer customer, ForumTopic topic)
    {
        if (topic is null || customer is null) return false;
        if (await IsGuestAsync(customer)) return false;
        return await IsForumModeratorAsync(customer);
    }

    public virtual async Task<bool> IsCustomerAllowedToDeleteTopicAsync(Customer customer, ForumTopic topic)
    {
        if (topic is null || customer is null) return false;
        if (await IsGuestAsync(customer)) return false;
        if (await IsForumModeratorAsync(customer)) return true;
        return forumSettings.AllowCustomersToDeletePosts && customer.Id == topic.CustomerId;
    }

    public virtual async Task<bool> IsCustomerAllowedToCreatePostAsync(Customer customer, ForumTopic topic)
    {
        if (topic is null || customer is null) return false;
        if (await IsGuestAsync(customer) && !forumSettings.AllowGuestsToCreatePosts) return false;
        return true;
    }

    public virtual async Task<bool> IsCustomerAllowedToEditPostAsync(Customer customer, ForumPost post)
    {
        if (post is null || customer is null) return false;
        if (await IsGuestAsync(customer)) return false;
        if (await IsForumModeratorAsync(customer)) return true;
        return forumSettings.AllowCustomersToEditPosts && customer.Id == post.CustomerId;
    }

    public virtual async Task<bool> IsCustomerAllowedToDeletePostAsync(Customer customer, ForumPost post)
    {
        if (post is null || customer is null) return false;
        if (await IsGuestAsync(customer)) return false;
        if (await IsForumModeratorAsync(customer)) return true;
        return forumSettings.AllowCustomersToDeletePosts && customer.Id == post.CustomerId;
    }

    public virtual async Task<bool> IsCustomerAllowedToSetTopicPriorityAsync(Customer customer)
    {
        if (customer is null) return false;
        if (await IsGuestAsync(customer)) return false;
        return await IsForumModeratorAsync(customer);
    }

    public virtual async Task<bool> IsCustomerAllowedToSubscribeAsync(Customer customer)
    {
        if (customer is null) return false;
        return !await IsGuestAsync(customer);
    }

    #endregion

    #region Utilities (public)

    public virtual Task<int> CalculateTopicPageIndexAsync(int forumTopicId, int pageSize, int postId)
    {
        var posts = forumPostRepository.Table
            .Where(fp => fp.TopicId == forumTopicId)
            .OrderBy(fp => fp.CreatedOnUtc)
            .ThenBy(fp => fp.Id)
            .Select(fp => fp.Id)
            .ToList();

        int pageIndex = 0;
        for (int i = 0; i < posts.Count; i++)
        {
            if (posts[i] == postId && pageSize > 0)
            {
                pageIndex = i / pageSize;
                break;
            }
        }

        return Task.FromResult(pageIndex);
    }

    #endregion

    #region PostVote

    public virtual Task<ForumPostVote?> GetPostVoteAsync(int postId, Customer customer)
    {
        if (customer is null) return Task.FromResult<ForumPostVote?>(null);
        return Task.FromResult(forumPostVoteRepository.Table
            .FirstOrDefault(pv => pv.ForumPostId == postId && pv.CustomerId == customer.Id));
    }

    public virtual Task<int> GetNumberOfPostVotesAsync(Customer customer, DateTime createdFromUtc)
    {
        if (customer is null) return Task.FromResult(0);
        return Task.FromResult(forumPostVoteRepository.Table
            .Count(pv => pv.CustomerId == customer.Id && pv.CreatedOnUtc > createdFromUtc));
    }

    public virtual async Task InsertPostVoteAsync(ForumPostVote postVote)
    {
        ArgumentNullException.ThrowIfNull(postVote);
        forumPostVoteRepository.Insert(postVote);

        // update post vote count
        var post = await GetPostByIdAsync(postVote.ForumPostId);
        if (post is not null)
        {
            post.VoteCount = postVote.IsUp ? post.VoteCount + 1 : post.VoteCount - 1;
            await UpdatePostAsync(post);
        }

        await eventPublisher.EntityInsertedAsync(postVote);
    }

    public virtual async Task UpdatePostVoteAsync(ForumPostVote postVote)
    {
        ArgumentNullException.ThrowIfNull(postVote);
        forumPostVoteRepository.Update(postVote);
        await eventPublisher.EntityUpdatedAsync(postVote);
    }

    public virtual async Task DeletePostVoteAsync(ForumPostVote postVote)
    {
        ArgumentNullException.ThrowIfNull(postVote);
        forumPostVoteRepository.Delete(postVote);

        // update post vote count
        var post = await GetPostByIdAsync(postVote.ForumPostId);
        if (post is not null)
        {
            post.VoteCount = postVote.IsUp ? post.VoteCount - 1 : post.VoteCount + 1;
            await UpdatePostAsync(post);
        }

        await eventPublisher.EntityDeletedAsync(postVote);
    }

    #endregion
}
