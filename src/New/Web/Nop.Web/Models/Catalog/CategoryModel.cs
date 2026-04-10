using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class CategoryModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public string? PictureUrl { get; set; }

    public bool DisplayCategoryBreadcrumb { get; set; }
    public List<CategoryModel> CategoryBreadcrumb { get; set; } = [];

    public List<SubCategoryModel> SubCategories { get; set; } = [];
    public List<ProductOverviewModel> FeaturedProducts { get; set; } = [];
    public List<ProductOverviewModel> Products { get; set; } = [];
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();

    public class SubCategoryModel : BaseNopEntityModel
    {
        public string? Name { get; set; }
        public string? SeName { get; set; }
        public string? PictureUrl { get; set; }
    }
}
