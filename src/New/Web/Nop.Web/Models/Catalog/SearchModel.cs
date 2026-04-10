using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class SearchModel : BaseNopModel
{
    public string? Warning { get; set; }
    public bool NoResults { get; set; }

    public string? q { get; set; }
    public int cid { get; set; }
    public bool isc { get; set; }
    public int mid { get; set; }
    public int vid { get; set; }
    public string? pf { get; set; }
    public string? pt { get; set; }
    public bool sid { get; set; }
    public bool adv { get; set; }
    public bool asv { get; set; }

    public List<SelectListItem> AvailableCategories { get; set; } = [];
    public List<SelectListItem> AvailableManufacturers { get; set; } = [];
    public List<SelectListItem> AvailableVendors { get; set; } = [];

    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public List<ProductOverviewModel> Products { get; set; } = [];
}

public class SelectListItem
{
    public string? Text { get; set; }
    public string? Value { get; set; }
    public bool Selected { get; set; }
}

public class SearchBoxModel : BaseNopModel
{
    public bool AutoCompleteEnabled { get; set; }
    public bool ShowProductImagesInSearchAutoComplete { get; set; }
    public int SearchTermMinimumLength { get; set; }
}

public class CategorySimpleModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? SeName { get; set; }
    public int? NumberOfProducts { get; set; }
    public bool IncludeInTopMenu { get; set; }
    public List<CategorySimpleModel> SubCategories { get; set; } = [];
}

public class TopMenuModel : BaseNopModel
{
    public List<CategorySimpleModel> Categories { get; set; } = [];
    public bool BlogEnabled { get; set; }
    public bool ForumEnabled { get; set; }
    public bool DisplayHomePageMenuItem { get; set; }
    public bool DisplayProductSearchMenuItem { get; set; }
    public bool DisplayCustomerInfoMenuItem { get; set; }
    public bool DisplayBlogMenuItem { get; set; }
    public bool DisplayForumsMenuItem { get; set; }
    public bool DisplayContactUsMenuItem { get; set; }
}

public class CategoryNavigationModel : BaseNopModel
{
    public int CurrentCategoryId { get; set; }
    public List<CategorySimpleModel> Categories { get; set; } = [];
}
