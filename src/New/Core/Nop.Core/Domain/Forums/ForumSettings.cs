using Nop.Core.Configuration;
using Nop.Core.Domain.Forums;

namespace Nop.Core.Domain.Forums;

public class ForumSettings : ISettings
{
    public bool ForumsEnabled { get; set; }
    public bool RelativeDateTimeFormattingEnabled { get; set; }
    public bool AllowCustomersToEditPosts { get; set; }
    public bool AllowCustomersToManageSubscriptions { get; set; }
    public bool AllowGuestsToCreatePosts { get; set; }
    public bool AllowGuestsToCreateTopics { get; set; }
    public bool AllowCustomersToDeletePosts { get; set; }
    public bool AllowPostVoting { get; set; }
    public int MaxVotesPerDay { get; set; }
    public int TopicSubjectMaxLength { get; set; }
    public int StrippedTopicMaxLength { get; set; }
    public int PostMaxLength { get; set; }
    public int TopicsPageSize { get; set; }
    public int PostsPageSize { get; set; }
    public int SearchResultsPageSize { get; set; }
    public int ActiveDiscussionsPageSize { get; set; }
    public int LatestCustomerPostsPageSize { get; set; }
    public bool ShowCustomersPostCount { get; set; }
    public EditorType ForumEditor { get; set; }
    public bool SignaturesEnabled { get; set; }
    public bool AllowPrivateMessages { get; set; }
    public bool ShowAlertForPM { get; set; }
    public int PrivateMessagesPageSize { get; set; }
    public int ForumSubscriptionsPageSize { get; set; }
    public bool NotifyAboutPrivateMessages { get; set; }
    public int PMSubjectMaxLength { get; set; }
    public int PMTextMaxLength { get; set; }
    public int HomePageActiveDiscussionsTopicCount { get; set; }
    public int ActiveDiscussionsFeedCount { get; set; }
    public bool ActiveDiscussionsFeedEnabled { get; set; }
    public bool ForumFeedsEnabled { get; set; }
    public int ForumFeedCount { get; set; }
    public int ForumSearchTermMinimumLength { get; set; }
}
