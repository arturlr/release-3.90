using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class ManufacturerModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public string? PictureUrl { get; set; }

    public List<ProductOverviewModel> FeaturedProducts { get; set; } = [];
    public List<ProductOverviewModel> Products { get; set; } = [];
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
}

public class ManufacturerNavigationModel : BaseNopModel
{
    public List<ManufacturerBriefInfoModel> Manufacturers { get; set; } = [];
    public int TotalManufacturers { get; set; }
}

public class ManufacturerBriefInfoModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? SeName { get; set; }
    public bool IsActive { get; set; }
}
