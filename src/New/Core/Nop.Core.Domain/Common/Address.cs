namespace Nop.Core.Domain.Common;

public class Address : BaseEntity, ICloneable
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public int? CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string? City { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? CustomAttributes { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public object Clone()
    {
        return new Address
        {
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Company = Company,
            CountryId = CountryId,
            StateProvinceId = StateProvinceId,
            City = City,
            Address1 = Address1,
            Address2 = Address2,
            ZipPostalCode = ZipPostalCode,
            PhoneNumber = PhoneNumber,
            FaxNumber = FaxNumber,
            CustomAttributes = CustomAttributes,
            CreatedOnUtc = CreatedOnUtc,
        };
    }
}
