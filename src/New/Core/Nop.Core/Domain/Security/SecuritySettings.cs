using Nop.Core.Configuration;

namespace Nop.Core.Domain.Security;

public class SecuritySettings : ISettings
{
    public bool ForceSslForAllPages { get; set; }
    public string? EncryptionKey { get; set; }
    public List<string> AdminAreaAllowedIpAddresses { get; set; } = [];
    public bool EnableXsrfProtectionForAdminArea { get; set; }
    public bool EnableXsrfProtectionForPublicStore { get; set; }
    public bool HoneypotEnabled { get; set; }
    public string? HoneypotInputName { get; set; }
}
