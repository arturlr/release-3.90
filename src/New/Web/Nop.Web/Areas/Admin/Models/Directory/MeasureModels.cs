using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Directory;

public class MeasureDimensionModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? SystemKeyword { get; set; }
    public decimal Ratio { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimaryDimension { get; set; }
}

public class MeasureWeightModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? SystemKeyword { get; set; }
    public decimal Ratio { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimaryWeight { get; set; }
}
