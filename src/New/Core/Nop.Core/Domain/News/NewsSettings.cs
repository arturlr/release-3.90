using Nop.Core.Configuration;

namespace Nop.Core.Domain.News;

public class NewsSettings : ISettings
{
    public bool Enabled { get; set; }
    public bool AllowNotRegisteredUsersToLeaveComments { get; set; }
    public bool NotifyAboutNewNewsComments { get; set; }
    public bool ShowNewsOnMainPage { get; set; }
    public int MainPageNewsCount { get; set; }
    public int NewsArchivePageSize { get; set; }
    public bool ShowHeaderRssUrl { get; set; }
    public bool NewsCommentsMustBeApproved { get; set; }
    public bool ShowNewsCommentsPerStore { get; set; }
}
