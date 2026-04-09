using Nop.Core.Configuration;

namespace Nop.Core.Domain.Messages;

public class EmailAccountSettings : ISettings
{
    public int DefaultEmailAccountId { get; set; }
}
