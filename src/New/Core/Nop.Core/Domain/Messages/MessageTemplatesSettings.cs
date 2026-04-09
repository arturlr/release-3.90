using Nop.Core.Configuration;

namespace Nop.Core.Domain.Messages;

public class MessageTemplatesSettings : ISettings
{
    public bool CaseInvariantReplacement { get; set; }
    public string? Color1 { get; set; }
    public string? Color2 { get; set; }
    public string? Color3 { get; set; }
}
