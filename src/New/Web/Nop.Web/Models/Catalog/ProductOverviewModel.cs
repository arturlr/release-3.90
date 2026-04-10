using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class ProductOverviewModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? SeName { get; set; }
    public string? ImageUrl { get; set; }
    public string? Price { get; set; }
    public string? OldPrice { get; set; }
    public bool MarkAsNew { get; set; }
}
