using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Areas.Admin.Models.Catalog;

public class ProductReviewListModel
{
    public DateTime? CreatedOnFrom { get; set; }
    public DateTime? CreatedOnTo { get; set; }
    public string? SearchText { get; set; }
    public int SearchStoreId { get; set; }
    public int SearchProductId { get; set; }
    public int SearchApprovedId { get; set; }
    public bool IsLoggedInAsVendor { get; set; }
    public List<SelectListItem> AvailableStores { get; set; } = [];
    public List<SelectListItem> AvailableApprovedOptions { get; set; } = [];
}

public class ProductReviewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerInfo { get; set; }
    public string? Title { get; set; }
    public string? ReviewText { get; set; }
    public string? ReplyText { get; set; }
    public int Rating { get; set; }
    public bool IsApproved { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsLoggedInAsVendor { get; set; }
}
