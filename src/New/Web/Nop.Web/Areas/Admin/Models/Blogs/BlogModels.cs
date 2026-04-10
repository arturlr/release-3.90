using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Blogs;

public class BlogPostListModel : BaseNopModel
{
    public int SearchStoreId { get; set; }
    public List<SelectListItem> AvailableStores { get; set; } = [];
}

public class BlogPostModel : BaseNopEntityModel
{
    public int LanguageId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? BodyOverview { get; set; }
    public bool AllowComments { get; set; }
    public string? Tags { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }

    public List<SelectListItem> AvailableLanguages { get; set; } = [];
}

public class BlogPostGridModel : BaseNopEntityModel
{
    public string? Title { get; set; }
    public string? LanguageName { get; set; }
    public int ApprovedComments { get; set; }
    public int NotApprovedComments { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class BlogCommentListModel : BaseNopModel
{
    public int? FilterByBlogPostId { get; set; }
    public int SearchApprovedId { get; set; }
    public DateTime? CreatedOnFrom { get; set; }
    public DateTime? CreatedOnTo { get; set; }
    public string? SearchText { get; set; }

    public List<SelectListItem> AvailableApprovedOptions { get; set; } = [];
}

public class BlogCommentModel : BaseNopEntityModel
{
    public int BlogPostId { get; set; }
    public string? BlogPostTitle { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerInfo { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public int StoreId { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedOn { get; set; }
}
