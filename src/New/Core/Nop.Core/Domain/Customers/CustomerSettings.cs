using Nop.Core.Configuration;
using Nop.Core.Domain.Customers;

namespace Nop.Core.Domain.Customers;

public class CustomerSettings : ISettings
{
    public bool UsernamesEnabled { get; set; }
    public bool CheckUsernameAvailabilityEnabled { get; set; }
    public bool AllowUsersToChangeUsernames { get; set; }
    public PasswordFormat DefaultPasswordFormat { get; set; }
    public string? HashedPasswordFormat { get; set; }
    public int PasswordMinLength { get; set; }
    public int UnduplicatedPasswordsNumber { get; set; }
    public int PasswordRecoveryLinkDaysValid { get; set; }
    public int PasswordLifetime { get; set; }
    public int FailedPasswordAllowedAttempts { get; set; }
    public int FailedPasswordLockoutMinutes { get; set; }
    public UserRegistrationType UserRegistrationType { get; set; }
    public bool AllowCustomersToUploadAvatars { get; set; }
    public int AvatarMaximumSizeBytes { get; set; }
    public bool DefaultAvatarEnabled { get; set; }
    public bool ShowCustomersLocation { get; set; }
    public bool ShowCustomersJoinDate { get; set; }
    public bool AllowViewingProfiles { get; set; }
    public bool NotifyNewCustomerRegistration { get; set; }
    public bool HideDownloadableProductsTab { get; set; }
    public bool HideBackInStockSubscriptionsTab { get; set; }
    public bool DownloadableProductsValidateUser { get; set; }
    public CustomerNameFormat CustomerNameFormat { get; set; }
    public bool NewsletterEnabled { get; set; }
    public bool NewsletterTickedByDefault { get; set; }
    public bool HideNewsletterBlock { get; set; }
    public bool NewsletterBlockAllowToUnsubscribe { get; set; }
    public int OnlineCustomerMinutes { get; set; }
    public bool StoreLastVisitedPage { get; set; }
    public bool SuffixDeletedCustomers { get; set; }
    public bool EnteringEmailTwice { get; set; }
    public bool RequireRegistrationForDownloadableProducts { get; set; }
    public int DeleteGuestTaskOlderThanMinutes { get; set; }

    // Form fields
    public bool GenderEnabled { get; set; }
    public bool DateOfBirthEnabled { get; set; }
    public bool DateOfBirthRequired { get; set; }
    public int? DateOfBirthMinimumAge { get; set; }
    public bool CompanyEnabled { get; set; }
    public bool CompanyRequired { get; set; }
    public bool StreetAddressEnabled { get; set; }
    public bool StreetAddressRequired { get; set; }
    public bool StreetAddress2Enabled { get; set; }
    public bool StreetAddress2Required { get; set; }
    public bool ZipPostalCodeEnabled { get; set; }
    public bool ZipPostalCodeRequired { get; set; }
    public bool CityEnabled { get; set; }
    public bool CityRequired { get; set; }
    public bool CountryEnabled { get; set; }
    public bool CountryRequired { get; set; }
    public bool StateProvinceEnabled { get; set; }
    public bool StateProvinceRequired { get; set; }
    public bool PhoneEnabled { get; set; }
    public bool PhoneRequired { get; set; }
    public bool FaxEnabled { get; set; }
    public bool FaxRequired { get; set; }
    public bool AcceptPrivacyPolicyEnabled { get; set; }
}
