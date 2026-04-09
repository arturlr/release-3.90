namespace Nop.Services.Common;

public interface IAddressAttributeFormatter
{
    Task<string> FormatAttributesAsync(string attributesXml,
        string separator = "<br />",
        bool htmlEncode = true);
}
