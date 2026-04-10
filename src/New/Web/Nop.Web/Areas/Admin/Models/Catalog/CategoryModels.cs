using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Catalog;

public class CategoryListModel : BaseNopModel
{
    public string? SearchCategoryName { get; set; }
    public int SearchStoreId { get; set; }

    public List<SelectListItem> AvailableStores { get; set; } = [];
}

public class CategoryModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int CategoryTemplateId { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public int ParentCategoryId { get; set; }
    public int PictureId { get; set; }
    public int PageSize { get; set; }
    public bool AllowCustomersToSelectPageSize { get; set; }
    public string? PageSizeOptions { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool IncludeInTopMenu { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }

    public List<SelectListItem> AvailableCategories { get; set; } = [];
    public List<SelectListItem> AvailableCategoryTemplates { get; set; } = [];
}

public class CategoryGridModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Breadcrumb { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}
