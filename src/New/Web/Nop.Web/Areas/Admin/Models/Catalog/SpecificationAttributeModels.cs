using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Catalog;

public class SpecificationAttributeModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}

public class SpecificationAttributeOptionModel : BaseNopEntityModel
{
    public int SpecificationAttributeId { get; set; }
    public string? Name { get; set; }
    public string? ColorSquaresRgb { get; set; }
    public bool EnableColorSquaresRgb { get; set; }
    public int NumberOfAssociatedProducts { get; set; }
    public int DisplayOrder { get; set; }
}
