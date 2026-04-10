using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Customers;

public class CustomerListModel : BaseNopModel
{
    public string? SearchEmail { get; set; }
    public string? SearchUsername { get; set; }
    public string? SearchFirstName { get; set; }
    public string? SearchLastName { get; set; }
    public string? SearchDayOfBirth { get; set; }
    public string? SearchMonthOfBirth { get; set; }
    public string? SearchCompany { get; set; }
    public string? SearchPhone { get; set; }
    public string? SearchZipPostalCode { get; set; }
    public string? SearchIpAddress { get; set; }
    public List<int> SearchCustomerRoleIds { get; set; } = [];

    public bool UsernamesEnabled { get; set; }
    public bool DateOfBirthEnabled { get; set; }
    public bool CompanyEnabled { get; set; }
    public bool PhoneEnabled { get; set; }
    public bool ZipPostalCodeEnabled { get; set; }

    public List<SelectListItem> AvailableCustomerRoles { get; set; } = [];
}

public class CustomerModel : BaseNopEntityModel
{
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int VendorId { get; set; }
    public string? AdminComment { get; set; }
    public bool IsTaxExempt { get; set; }
    public bool Active { get; set; }

    // Form fields (stored as GenericAttributes)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Company { get; set; }
    public string? StreetAddress { get; set; }
    public string? StreetAddress2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? City { get; set; }
    public int CountryId { get; set; }
    public int StateProvinceId { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }

    // Read-only display
    public string? TimeZoneId { get; set; }
    public string? RegisteredInStore { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public string? LastIpAddress { get; set; }
    public string? LastVisitedPage { get; set; }

    // Roles
    public List<int> SelectedCustomerRoleIds { get; set; } = [];

    // Settings-driven visibility
    public bool UsernamesEnabled { get; set; }
    public bool GenderEnabled { get; set; }
    public bool DateOfBirthEnabled { get; set; }
    public bool CompanyEnabled { get; set; }
    public bool StreetAddressEnabled { get; set; }
    public bool StreetAddress2Enabled { get; set; }
    public bool ZipPostalCodeEnabled { get; set; }
    public bool CityEnabled { get; set; }
    public bool CountryEnabled { get; set; }
    public bool StateProvinceEnabled { get; set; }
    public bool PhoneEnabled { get; set; }
    public bool FaxEnabled { get; set; }

    // Dropdowns
    public List<SelectListItem> AvailableVendors { get; set; } = [];
    public List<SelectListItem> AvailableCustomerRoles { get; set; } = [];
    public List<SelectListItem> AvailableCountries { get; set; } = [];
    public List<SelectListItem> AvailableStates { get; set; } = [];
    public List<SelectListItem> AvailableTimeZones { get; set; } = [];
}

public class CustomerGridModel : BaseNopEntityModel
{
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? CustomerRoleNames { get; set; }
    public bool Active { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? LastActivityDate { get; set; }
}
