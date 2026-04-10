using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Shipping;

public class ShippingMethodModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class DeliveryDateModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}

public class ProductAvailabilityRangeModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}

public class WarehouseModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? AdminComment { get; set; }
    public WarehouseAddressModel Address { get; set; } = new();
}

public class WarehouseAddressModel
{
    public int? CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string? City { get; set; }
    public string? Address1 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public IList<SelectListItem> AvailableCountries { get; set; } = [];
    public IList<SelectListItem> AvailableStates { get; set; } = [];
}

public class ShippingMethodRestrictionModel
{
    public IList<ShippingMethodModel> AvailableShippingMethods { get; set; } = [];
    public IList<CountryModel> AvailableCountries { get; set; } = [];
    /// <summary>
    /// Restricted[countryId][shippingMethodId] = true means restricted.
    /// </summary>
    public Dictionary<int, Dictionary<int, bool>> Restricted { get; set; } = [];
}

public class CountryModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
