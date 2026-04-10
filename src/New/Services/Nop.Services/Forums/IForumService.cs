using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;

namespace Nop.Services.Forums;

public interface IForumService
{
    // ForumGroup
    Task DeleteForumGroupAsync(ForumGroup forumGroup);
    Task<ForumGroup?> GetForumGroupByIdAsync(int forumGroupId);
    Task<IList<ForumGroup>> GetAllForumGroupsAsync();
    Task InsertForumGroupAsync(ForumGroup forumGroup);
    Task UpdateForumGroupAsync(ForumGroup forumGroup);

    // Forum
    Task DeleteForumAsync(Forum forum);
    Task<Forum?> GetForumByIdAsync(int forumId);
    Task<IList<Forum>> GetAllForumsByGroupIdAsync(int forumGroupId);
    Task InsertForumAsync(Forum forum);
    Task UpdateForumAsync(Forum forum);

    // ForumTopic
    Task DeleteTopicAsync(ForumTopic forumTopic);
    Task<ForumTopic?> GetTopicByIdAsync(int forumTopicId, bool increaseViews = false);
    Task<IPagedList<ForumTopic>> GetAllTopicsAsync(int forumId = 0, int customerId = 0,
        string keywords = "", ForumSearchType searchType = ForumSearchType.All,
        int limitDays = 0, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<IPagedList<ForumTopic>> GetActiveTopicsAsync(int forumId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertTopicAsync(ForumTopic forumTopic, bool sendNotifications);
    Task UpdateTopicAsync(ForumTopic forumTopic);
    Task<ForumTopic?> MoveTopicAsync(int forumTopicId, int newForumId);

    // ForumPost
    Task DeletePostAsync(ForumPost forumPost);
    Task<ForumPost?> GetPostByIdAsync(int forumPostId);
    Task<IPagedList<ForumPost>> GetAllPostsAsync(int forumTopicId = 0, int customerId = 0,
        string keywords = "", bool ascSort = false,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertPostAsync(ForumPost forumPost, bool sendNotifications);
    Task UpdatePostAsync(ForumPost forumPost);

    // PrivateMessage
    Task DeletePrivateMessageAsync(PrivateMessage privateMessage);
    Task<PrivateMessage?> GetPrivateMessageByIdAsync(int privateMessageId);
    Task<IPagedList<PrivateMessage>> GetAllPrivateMessagesAsync(int storeId, int fromCustomerId,
        int toCustomerId, bool? isRead, bool? isDeletedByAuthor, bool? isDeletedByRecipient,
        string keywords, int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertPrivateMessageAsync(PrivateMessage privateMessage);
    Task UpdatePrivateMessageAsync(PrivateMessage privateMessage);

    // ForumSubscription
    Task DeleteSubscriptionAsync(ForumSubscription forumSubscription);
    Task<ForumSubscription?> GetSubscriptionByIdAsync(int forumSubscriptionId);
    Task<IPagedList<ForumSubscription>> GetAllSubscriptionsAsync(int customerId = 0, int forumId = 0,
        int topicId = 0, int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertSubscriptionAsync(ForumSubscription forumSubscription);
    Task UpdateSubscriptionAsync(ForumSubscription forumSubscription);

    // Permission checks
    Task<bool> IsCustomerAllowedToCreateTopicAsync(Customer customer, Forum forum);
    Task<bool> IsCustomerAllowedToEditTopicAsync(Customer customer, ForumTopic topic);
    Task<bool> IsCustomerAllowedToMoveTopicAsync(Customer customer, ForumTopic topic);
    Task<bool> IsCustomerAllowedToDeleteTopicAsync(Customer customer, ForumTopic topic);
    Task<bool> IsCustomerAllowedToCreatePostAsync(Customer customer, ForumTopic topic);
    Task<bool> IsCustomerAllowedToEditPostAsync(Customer customer, ForumPost post);
    Task<bool> IsCustomerAllowedToDeletePostAsync(Customer customer, ForumPost post);
    Task<bool> IsCustomerAllowedToSetTopicPriorityAsync(Customer customer);
    Task<bool> IsCustomerAllowedToSubscribeAsync(Customer customer);

    // Utilities
    Task<int> CalculateTopicPageIndexAsync(int forumTopicId, int pageSize, int postId);

    // PostVote
    Task<ForumPostVote?> GetPostVoteAsync(int postId, Customer customer);
    Task<int> GetNumberOfPostVotesAsync(Customer customer, DateTime createdFromUtc);
    Task InsertPostVoteAsync(ForumPostVote postVote);
    Task UpdatePostVoteAsync(ForumPostVote postVote);
    Task DeletePostVoteAsync(ForumPostVote postVote);
}
