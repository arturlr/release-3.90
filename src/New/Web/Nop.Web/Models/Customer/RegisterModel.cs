using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Customer;

public class RegisterModel : BaseNopModel
{
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    public bool EnteringEmailTwice { get; set; }

    [DataType(DataType.EmailAddress)]
    public string? ConfirmEmail { get; set; }

    public bool UsernamesEnabled { get; set; }
    public bool CheckUsernameAvailabilityEnabled { get; set; }
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    // Form fields
    public bool GenderEnabled { get; set; }
    public string? Gender { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public bool DateOfBirthEnabled { get; set; }
    public int? DateOfBirthDay { get; set; }
    public int? DateOfBirthMonth { get; set; }
    public int? DateOfBirthYear { get; set; }
    public bool DateOfBirthRequired { get; set; }

    public DateTime? ParseDateOfBirth()
    {
        if (!DateOfBirthYear.HasValue || !DateOfBirthMonth.HasValue || !DateOfBirthDay.HasValue)
            return null;
        try { return new DateTime(DateOfBirthYear.Value, DateOfBirthMonth.Value, DateOfBirthDay.Value); }
        catch { return null; }
    }

    public bool CompanyEnabled { get; set; }
    public bool CompanyRequired { get; set; }
    public string? Company { get; set; }

    public bool StreetAddressEnabled { get; set; }
    public bool StreetAddressRequired { get; set; }
    public string? StreetAddress { get; set; }

    public bool StreetAddress2Enabled { get; set; }
    public bool StreetAddress2Required { get; set; }
    public string? StreetAddress2 { get; set; }

    public bool ZipPostalCodeEnabled { get; set; }
    public bool ZipPostalCodeRequired { get; set; }
    public string? ZipPostalCode { get; set; }

    public bool CityEnabled { get; set; }
    public bool CityRequired { get; set; }
    public string? City { get; set; }

    public bool CountryEnabled { get; set; }
    public bool CountryRequired { get; set; }
    public int CountryId { get; set; }
    public IList<SelectListItem> AvailableCountries { get; set; } = [];

    public bool StateProvinceEnabled { get; set; }
    public bool StateProvinceRequired { get; set; }
    public int StateProvinceId { get; set; }
    public IList<SelectListItem> AvailableStates { get; set; } = [];

    public bool PhoneEnabled { get; set; }
    public bool PhoneRequired { get; set; }
    public string? Phone { get; set; }

    public bool FaxEnabled { get; set; }
    public bool FaxRequired { get; set; }
    public string? Fax { get; set; }

    public bool NewsletterEnabled { get; set; }
    public bool Newsletter { get; set; }

    public bool AcceptPrivacyPolicyEnabled { get; set; }

    // Time zone
    public string? TimeZoneId { get; set; }
    public bool AllowCustomersToSetTimeZone { get; set; }
    public IList<SelectListItem> AvailableTimeZones { get; set; } = [];

    // EU VAT
    public string? VatNumber { get; set; }
    public bool DisplayVatNumber { get; set; }
}
