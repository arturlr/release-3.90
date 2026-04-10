using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Catalog;

public class ManufacturerListModel : BaseNopModel
{
    public string? SearchManufacturerName { get; set; }
    public int SearchStoreId { get; set; }

    public List<SelectListItem> AvailableStores { get; set; } = [];
}

public class ManufacturerModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int ManufacturerTemplateId { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public int PictureId { get; set; }
    public int PageSize { get; set; }
    public bool AllowCustomersToSelectPageSize { get; set; }
    public string? PageSizeOptions { get; set; }
    public string? PriceRanges { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }

    public List<SelectListItem> AvailableManufacturerTemplates { get; set; } = [];
}

public class ManufacturerGridModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}
