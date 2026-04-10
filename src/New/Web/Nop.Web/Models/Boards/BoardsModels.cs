using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Forums;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Boards;

// --- Index ---
public class BoardsIndexModel
{
    public IList<ForumGroupModel> ForumGroups { get; set; } = [];
}

// --- Forum Group ---
public class ForumGroupModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SeName { get; set; }
    public IList<ForumRowModel> Forums { get; set; } = [];
}

public class ForumRowModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SeName { get; set; }
    public string? Description { get; set; }
    public int NumTopics { get; set; }
    public int NumPosts { get; set; }
    public int LastPostId { get; set; }
}

// --- Forum Page (topic list) ---
public class ForumPageModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SeName { get; set; }
    public string? Description { get; set; }
    public string? WatchForumText { get; set; }
    public IList<ForumTopicRowModel> ForumTopics { get; set; } = [];
    public int TopicPageSize { get; set; }
    public int TopicTotalRecords { get; set; }
    public int TopicPageIndex { get; set; }
    public bool IsCustomerAllowedToSubscribe { get; set; }
    public bool ForumFeedsEnabled { get; set; }
    public int PostsPageSize { get; set; }
    public bool AllowPostVoting { get; set; }
}

public class ForumTopicRowModel
{
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? SeName { get; set; }
    public int LastPostId { get; set; }
    public int NumPosts { get; set; }
    public int Views { get; set; }
    public int Votes { get; set; }
    public int NumReplies { get; set; }
    public ForumTopicType ForumTopicType { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool AllowViewingProfiles { get; set; }
    public int TotalPostPages { get; set; }
}

// --- Topic Page (post list) ---
public class ForumTopicPageModel
{
    public int Id { get; set; }
    public string? Subject { get; set; }
    public string? SeName { get; set; }
    public string? WatchTopicText { get; set; }
    public bool IsCustomerAllowedToEditTopic { get; set; }
    public bool IsCustomerAllowedToDeleteTopic { get; set; }
    public bool IsCustomerAllowedToMoveTopic { get; set; }
    public bool IsCustomerAllowedToSubscribe { get; set; }
    public IList<ForumPostModel> ForumPostModels { get; set; } = [];
    public int PostsPageIndex { get; set; }
    public int PostsPageSize { get; set; }
    public int PostsTotalRecords { get; set; }
}

public class ForumPostModel
{
    public int Id { get; set; }
    public int ForumTopicId { get; set; }
    public string? ForumTopicSeName { get; set; }
    public string? FormattedText { get; set; }
    public bool IsCurrentCustomerAllowedToEditPost { get; set; }
    public bool IsCurrentCustomerAllowedToDeletePost { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool AllowViewingProfiles { get; set; }
    public string? PostCreatedOnStr { get; set; }
    public bool ShowCustomersPostCount { get; set; }
    public int ForumPostCount { get; set; }
    public bool AllowPostVoting { get; set; }
    public int VoteCount { get; set; }
    public bool? VoteIsUp { get; set; }
    public int CurrentTopicPage { get; set; }
}

// --- Active Discussions ---
public class ActiveDiscussionsModel
{
    public IList<ForumTopicRowModel> ForumTopics { get; set; } = [];
    public bool ViewAllLinkEnabled { get; set; }
    public bool ActiveDiscussionsFeedEnabled { get; set; }
    public int TopicPageSize { get; set; }
    public int TopicTotalRecords { get; set; }
    public int TopicPageIndex { get; set; }
    public int PostsPageSize { get; set; }
    public bool AllowPostVoting { get; set; }
}

// --- Topic Create/Edit ---
public class EditForumTopicModel
{
    public bool IsEdit { get; set; }
    public int Id { get; set; }
    public int ForumId { get; set; }
    public string? ForumName { get; set; }
    public string? ForumSeName { get; set; }
    public int TopicTypeId { get; set; }
    public EditorType ForumEditor { get; set; }
    public string? Subject { get; set; }
    public string? Text { get; set; }
    public bool IsCustomerAllowedToSetTopicPriority { get; set; }
    public IEnumerable<SelectListItem> TopicPriorities { get; set; } = [];
    public bool IsCustomerAllowedToSubscribe { get; set; }
    public bool Subscribed { get; set; }
}

// --- Post Create/Edit ---
public class EditForumPostModel
{
    public int Id { get; set; }
    public int ForumTopicId { get; set; }
    public bool IsEdit { get; set; }
    public string? Text { get; set; }
    public EditorType ForumEditor { get; set; }
    public string? ForumName { get; set; }
    public string? ForumTopicSubject { get; set; }
    public string? ForumTopicSeName { get; set; }
    public bool IsCustomerAllowedToSubscribe { get; set; }
    public bool Subscribed { get; set; }
}

// --- Topic Move ---
public class TopicMoveModel : BaseNopEntityModel
{
    public int ForumSelected { get; set; }
    public string? TopicSeName { get; set; }
    public IEnumerable<SelectListItem> ForumList { get; set; } = [];
}

// --- Search ---
public class ForumSearchModel
{
    public bool ShowAdvancedSearch { get; set; }
    public string? SearchTerms { get; set; }
    public int? ForumId { get; set; }
    public int? Within { get; set; }
    public int? LimitDays { get; set; }
    public IList<ForumTopicRowModel> ForumTopics { get; set; } = [];
    public int TopicPageSize { get; set; }
    public int TopicTotalRecords { get; set; }
    public int TopicPageIndex { get; set; }
    public List<SelectListItem> LimitList { get; set; } = [];
    public List<SelectListItem> ForumList { get; set; } = [];
    public List<SelectListItem> WithinList { get; set; } = [];
    public int ForumIdSelected { get; set; }
    public int WithinSelected { get; set; }
    public int LimitDaysSelected { get; set; }
    public bool SearchResultsVisible { get; set; }
    public bool NoResultsVisible { get; set; }
    public string? Error { get; set; }
    public int PostsPageSize { get; set; }
    public bool AllowPostVoting { get; set; }
}

// --- Customer Forum Subscriptions ---
public class CustomerForumSubscriptionsModel
{
    public IList<ForumSubscriptionModel> ForumSubscriptions { get; set; } = [];
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }

    public class ForumSubscriptionModel : BaseNopEntityModel
    {
        public int ForumId { get; set; }
        public int ForumTopicId { get; set; }
        public bool TopicSubscription { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
    }
}
