using Nop.Core.Domain.Common;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Customer;

public class CustomerAddressListModel : BaseNopModel
{
    public IList<AddressModel> Addresses { get; set; } = [];
}

public class CustomerAddressEditModel : BaseNopModel
{
    public AddressModel Address { get; set; } = new();
}

/// <summary>
/// Simplified address model for public views.
/// </summary>
public class AddressModel : BaseNopEntityModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public int? CountryId { get; set; }
    public string? CountryName { get; set; }
    public int? StateProvinceId { get; set; }
    public string? StateProvinceName { get; set; }
    public string? City { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
}
