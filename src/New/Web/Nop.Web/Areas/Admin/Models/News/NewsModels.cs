using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.News;

public class NewsItemListModel : BaseNopModel
{
    public int SearchStoreId { get; set; }
    public List<SelectListItem> AvailableStores { get; set; } = [];
}

public class NewsItemModel : BaseNopEntityModel
{
    public int LanguageId { get; set; }
    public string? Title { get; set; }
    public string? Short { get; set; }
    public string? Full { get; set; }
    public bool Published { get; set; }
    public bool AllowComments { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }

    public List<SelectListItem> AvailableLanguages { get; set; } = [];
}

public class NewsItemGridModel : BaseNopEntityModel
{
    public string? Title { get; set; }
    public string? LanguageName { get; set; }
    public bool Published { get; set; }
    public int ApprovedComments { get; set; }
    public int NotApprovedComments { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class NewsCommentListModel : BaseNopModel
{
    public int? FilterByNewsItemId { get; set; }
    public int SearchApprovedId { get; set; }
    public DateTime? CreatedOnFrom { get; set; }
    public DateTime? CreatedOnTo { get; set; }
    public string? SearchText { get; set; }

    public List<SelectListItem> AvailableApprovedOptions { get; set; } = [];
}

public class NewsCommentModel : BaseNopEntityModel
{
    public int NewsItemId { get; set; }
    public string? NewsItemTitle { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerInfo { get; set; }
    public string? CommentTitle { get; set; }
    public string? CommentText { get; set; }
    public bool IsApproved { get; set; }
    public int StoreId { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedOn { get; set; }
}
