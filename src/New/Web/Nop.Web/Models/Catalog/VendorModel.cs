using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class VendorModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public string? PictureUrl { get; set; }

    public List<ProductOverviewModel> Products { get; set; } = [];
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
}

public class VendorNavigationModel : BaseNopModel
{
    public List<VendorBriefInfoModel> Vendors { get; set; } = [];
    public int TotalVendors { get; set; }
}

public class VendorBriefInfoModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? SeName { get; set; }
}
