using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Vendors;

public class VendorListModel : BaseNopModel
{
    public string? SearchName { get; set; }
}

public class VendorModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? AdminComment { get; set; }
    public int PictureId { get; set; }
    public bool Active { get; set; }
    public int DisplayOrder { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public int PageSize { get; set; }
    public bool AllowCustomersToSelectPageSize { get; set; }
    public string? PageSizeOptions { get; set; }

    // Address fields (inline — no nested AddressModel, matching AffiliateController pattern)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Company { get; set; }
    public int? CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string? City { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }

    public List<SelectListItem> AvailableCountries { get; set; } = [];
    public List<SelectListItem> AvailableStates { get; set; } = [];
}

public class VendorGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Active { get; set; }
}

public class VendorNoteModel
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class AssociatedCustomerModel
{
    public int Id { get; set; }
    public string? Email { get; set; }
}
