using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Directory;

public class CountryListModel
{
    public string? SearchCountryName { get; set; }
}

public class CountryModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public bool AllowsBilling { get; set; }
    public bool AllowsShipping { get; set; }
    public string? TwoLetterIsoCode { get; set; }
    public string? ThreeLetterIsoCode { get; set; }
    public int NumericIsoCode { get; set; }
    public bool SubjectToVat { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
    public bool LimitedToStores { get; set; }
}

public class CountryGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool AllowsBilling { get; set; }
    public bool AllowsShipping { get; set; }
    public string? TwoLetterIsoCode { get; set; }
    public string? ThreeLetterIsoCode { get; set; }
    public int NumericIsoCode { get; set; }
    public bool SubjectToVat { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}

public class StateProvinceModel
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string? Name { get; set; }
    public string? Abbreviation { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}
