namespace Nop.Services.Customers;

public interface ICustomerAttributeFormatter
{
    Task<string> FormatAttributesAsync(string attributesXml,
        string separator = "<br />",
        bool htmlEncode = true);
}
