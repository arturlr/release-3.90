using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Catalog;

public class ProductAttributeModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class PredefinedProductAttributeValueModel : BaseNopEntityModel
{
    public int ProductAttributeId { get; set; }
    public string? Name { get; set; }
    public decimal PriceAdjustment { get; set; }
    public decimal WeightAdjustment { get; set; }
    public decimal Cost { get; set; }
    public bool IsPreSelected { get; set; }
    public int DisplayOrder { get; set; }
}

public class UsedByProductModel : BaseNopEntityModel
{
    public string? ProductName { get; set; }
    public bool Published { get; set; }
}
