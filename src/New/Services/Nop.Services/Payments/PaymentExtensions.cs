using System.Xml;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;

namespace Nop.Services.Payments;

public static class PaymentExtensions
{
    public static bool IsPaymentMethodActive(
        this IPaymentMethod paymentMethod, PaymentSettings paymentSettings)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);
        ArgumentNullException.ThrowIfNull(paymentSettings);

        return paymentSettings.ActivePaymentMethodSystemNames
            .Contains(paymentMethod.SystemName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Serialize CustomValues of ProcessPaymentRequest to XML.
    /// Preserves legacy XML format for data migration compatibility.
    /// </summary>
    public static string? SerializeCustomValues(this ProcessPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CustomValues.Count == 0)
            return null;

        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw);
        writer.WriteStartElement("CustomValues");
        foreach (var (key, value) in request.CustomValues)
        {
            writer.WriteStartElement("item");
            writer.WriteElementString("key", key);
            writer.WriteElementString("value", value?.ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
        writer.Flush();
        return sw.ToString();
    }

    /// <summary>
    /// Deserialize CustomValues from Order.CustomValuesXml.
    /// </summary>
    public static Dictionary<string, object> DeserializeCustomValues(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return DeserializeCustomValues(order.CustomValuesXml);
    }

    public static Dictionary<string, object> DeserializeCustomValues(string? customValuesXml)
    {
        if (string.IsNullOrWhiteSpace(customValuesXml))
            return [];

        var result = new Dictionary<string, object>();
        try
        {
            using var sr = new StringReader(customValuesXml);
            using var reader = XmlReader.Create(sr);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "item")
                {
                    reader.ReadStartElement("item");
                    var key = reader.ReadElementString("key");
                    var value = reader.ReadElementString("value");
                    result[key] = value;
                    reader.ReadEndElement();
                }
            }
        }
        catch
        {
            // Malformed XML — return what we have
        }

        return result;
    }
}
