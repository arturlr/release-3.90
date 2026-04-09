using Nop.Core.Configuration;

namespace Nop.Core.Domain.Blogs;

public class BlogSettings : ISettings
{
    public bool Enabled { get; set; }
    public int PostsPageSize { get; set; }
    public bool AllowNotRegisteredUsersToLeaveComments { get; set; }
    public bool NotifyAboutNewBlogComments { get; set; }
    public int NumberOfTags { get; set; }
    public bool ShowHeaderRssUrl { get; set; }
    public bool BlogCommentsMustBeApproved { get; set; }
    public bool ShowBlogCommentsPerStore { get; set; }
}
