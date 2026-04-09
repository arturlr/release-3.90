using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Nop.Core;

/// <summary>
/// XML helper class
/// </summary>
public static partial class XmlHelper
{
    [GeneratedRegex(@"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", RegexOptions.Compiled)]
    private static partial Regex InvalidXmlCharsRegex();

    public static string? XmlEncode(string? str)
    {
        if (str is null) return null;
        str = InvalidXmlCharsRegex().Replace(str, string.Empty);
        return XmlEncodeAsIs(str);
    }

    public static string? XmlEncodeAsIs(string? str)
    {
        if (str is null) return null;
        using var sw = new StringWriter();
        using var xwr = XmlWriter.Create(sw, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment });
        xwr.WriteString(str);
        xwr.Flush();
        return sw.ToString();
    }

    public static string? XmlEncodeAttribute(string? str)
    {
        if (str is null) return null;
        str = InvalidXmlCharsRegex().Replace(str, string.Empty);
        return XmlEncodeAttributeAsIs(str);
    }

    public static string? XmlEncodeAttributeAsIs(string? str) =>
        XmlEncodeAsIs(str)?.Replace("\"", "&quot;");

    public static string XmlDecode(string str) =>
        new StringBuilder(str)
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .ToString();

    public static string SerializeDateTime(DateTime dateTime)
    {
        var xmlS = new XmlSerializer(typeof(DateTime));
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        xmlS.Serialize(sw, dateTime);
        return sb.ToString();
    }

    public static DateTime DeserializeDateTime(string dateTime)
    {
        var xmlS = new XmlSerializer(typeof(DateTime));
        using var sr = new StringReader(dateTime);
        return (DateTime)xmlS.Deserialize(sr)!;
    }
}
