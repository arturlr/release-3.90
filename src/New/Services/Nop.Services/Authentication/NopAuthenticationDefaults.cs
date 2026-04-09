namespace Nop.Services.Authentication;

/// <summary>
/// Authentication scheme and claim constants for nopCommerce cookie authentication.
/// </summary>
public static class NopAuthenticationDefaults
{
    /// <summary>
    /// The default authentication scheme used for cookie authentication.
    /// </summary>
    public const string AuthenticationScheme = "NopAuthentication";

    /// <summary>
    /// Claim type for the customer GUID stored in the authentication cookie.
    /// </summary>
    public const string CustomerGuidClaimType = "NopCustomerGuid";
}
