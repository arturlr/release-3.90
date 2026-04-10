using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class CatalogPagingFilteringModel : BasePageableModel
{
    public int? OrderBy { get; set; }
    public string? ViewMode { get; set; }

    // Price range filter
    public string? Price { get; set; }

    // Specification filter (comma-separated option IDs)
    public string? Specs { get; set; }
}
