using System.Xml;

namespace Nop.Services.ExportImport;

public static class XmlWriterExtensions
{
    public static void WriteString(this XmlWriter xmlWriter, string nodeName, object? nodeValue, bool ignore = false)
    {
        if (ignore) return;
        xmlWriter.WriteElementString(nodeName, nodeValue?.ToString() ?? string.Empty);
    }
}
