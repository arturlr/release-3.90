namespace Nop.Web.Models.Profile;

public class ProfileIndexModel
{
    public int CustomerProfileId { get; set; }
    public string? ProfileTitle { get; set; }
    public int PostsPage { get; set; }
    public bool PagingPosts { get; set; }
    public bool ForumsEnabled { get; set; }

    public ProfileInfoModel Info { get; set; } = new();
    public ProfilePostsModel Posts { get; set; } = new();
}

public class ProfileInfoModel
{
    public int CustomerProfileId { get; set; }
    public string? AvatarUrl { get; set; }
    public bool LocationEnabled { get; set; }
    public string? Location { get; set; }
    public bool PMEnabled { get; set; }
    public bool TotalPostsEnabled { get; set; }
    public string? TotalPosts { get; set; }
    public bool JoinDateEnabled { get; set; }
    public string? JoinDate { get; set; }
    public bool DateOfBirthEnabled { get; set; }
    public string? DateOfBirth { get; set; }
}

public class ProfilePostsModel
{
    public List<PostModel> Posts { get; set; } = [];
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public int CustomerProfileId { get; set; }
}

public class PostModel
{
    public int ForumTopicId { get; set; }
    public string? ForumTopicTitle { get; set; }
    public string? ForumTopicSlug { get; set; }
    public string? ForumPostText { get; set; }
    public string? Posted { get; set; }
}
