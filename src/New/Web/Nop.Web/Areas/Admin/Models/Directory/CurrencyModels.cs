using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Directory;

public class CurrencyModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal Rate { get; set; }
    public string? DisplayLocale { get; set; }
    public string? CustomFormatting { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
    public int RoundingTypeId { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class CurrencyGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal Rate { get; set; }
    public bool IsPrimaryExchangeRateCurrency { get; set; }
    public bool IsPrimaryStoreCurrency { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}
