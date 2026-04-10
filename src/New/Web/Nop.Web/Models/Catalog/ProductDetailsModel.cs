using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class ProductDetailsModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public bool ShowSku { get; set; }
    public string? Sku { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Gtin { get; set; }
    public bool FreeShipping { get; set; }
    public string? StockAvailability { get; set; }
    public bool IsCurrentCustomerRegistered { get; set; }
    public bool AllowCustomerReviews { get; set; }
    public string? ProductPrice { get; set; }
    public string? OldPrice { get; set; }
    public string? ImageUrl { get; set; }
    public List<PictureModel> Pictures { get; set; } = [];
    public string? ProductTemplateViewPath { get; set; }
    public bool DisplayEditLink { get; set; }
    public string? EditLink { get; set; }

    public class PictureModel
    {
        public string? ImageUrl { get; set; }
        public string? FullSizeImageUrl { get; set; }
        public string? Title { get; set; }
        public string? AlternateText { get; set; }
    }
}

public class ProductReviewsModel : BaseNopEntityModel
{
    public string? ProductName { get; set; }
    public string? ProductSeName { get; set; }
    public List<ProductReviewModel> Items { get; set; } = [];
    public AddProductReviewModel AddProductReview { get; set; } = new();
}

public class ProductReviewModel : BaseNopEntityModel
{
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Title { get; set; }
    public string? ReviewText { get; set; }
    public int Rating { get; set; }
    public int HelpfulYesTotal { get; set; }
    public int HelpfulNoTotal { get; set; }
    public DateTime WrittenOnStr { get; set; }
}

public class AddProductReviewModel
{
    public string? Title { get; set; }
    public string? ReviewText { get; set; }
    public int Rating { get; set; }
    public bool CanCurrentCustomerLeaveReview { get; set; } = true;
    public bool SuccessfullyAdded { get; set; }
    public string? Result { get; set; }
}

public class CompareProductsModel
{
    public bool IncludeShortDescriptionInCompareProducts { get; set; }
    public bool IncludeFullDescriptionInCompareProducts { get; set; }
    public List<ProductOverviewModel> Products { get; set; } = [];
}
