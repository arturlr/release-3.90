using Nop.Core.Configuration;

namespace Nop.Core.Domain.Customers;

public class ExternalAuthenticationSettings : ISettings
{
    public bool AutoRegisterEnabled { get; set; }
    public bool RequireEmailValidation { get; set; }
    public List<string> ActiveAuthenticationMethodSystemNames { get; set; } = [];
}
