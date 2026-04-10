using System.Text;
using System.Xml;
using ClosedXML.Excel;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Stores;

namespace Nop.Services.ExportImport;

public partial class ExportManager(
    ICategoryService categoryService,
    IManufacturerService manufacturerService,
    IProductService productService,
    IProductTagService productTagService,
    IPictureService pictureService,
    IOrderService orderService) : IExportManager
{
    public string ExportManufacturersToXml(IList<Manufacturer> manufacturers)
    {
        var sb = new StringBuilder();
        using var stringWriter = new StringWriter(sb);
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true });
        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement("Manufacturers");
        xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

        foreach (var m in manufacturers)
        {
            xmlWriter.WriteStartElement("Manufacturer");
            xmlWriter.WriteString("ManufacturerId", m.Id);
            xmlWriter.WriteString("Name", m.Name);
            xmlWriter.WriteString("Description", m.Description);
            xmlWriter.WriteString("ManufacturerTemplateId", m.ManufacturerTemplateId);
            xmlWriter.WriteString("MetaKeywords", m.MetaKeywords);
            xmlWriter.WriteString("MetaDescription", m.MetaDescription);
            xmlWriter.WriteString("MetaTitle", m.MetaTitle);
            xmlWriter.WriteString("PictureId", m.PictureId);
            xmlWriter.WriteString("PageSize", m.PageSize);
            xmlWriter.WriteString("AllowCustomersToSelectPageSize", m.AllowCustomersToSelectPageSize);
            xmlWriter.WriteString("PageSizeOptions", m.PageSizeOptions);
            xmlWriter.WriteString("PriceRanges", m.PriceRanges);
            xmlWriter.WriteString("Published", m.Published);
            xmlWriter.WriteString("Deleted", m.Deleted);
            xmlWriter.WriteString("DisplayOrder", m.DisplayOrder);
            xmlWriter.WriteString("CreatedOnUtc", m.CreatedOnUtc);
            xmlWriter.WriteString("UpdatedOnUtc", m.UpdatedOnUtc);
            xmlWriter.WriteEndElement();
        }

        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        return sb.ToString();
    }

    public byte[] ExportManufacturersToXlsx(IList<Manufacturer> manufacturers)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Manufacturers");
        var headers = new[] { "Id", "Name", "Description", "ManufacturerTemplateId", "MetaKeywords", "MetaDescription", "MetaTitle", "PictureId", "PageSize", "AllowCustomersToSelectPageSize", "PageSizeOptions", "PriceRanges", "Published", "Deleted", "DisplayOrder", "CreatedOnUtc", "UpdatedOnUtc" };
        WriteHeaders(ws, headers);

        var row = 2;
        foreach (var m in manufacturers)
        {
            var col = 1;
            ws.Cell(row, col++).Value = m.Id;
            ws.Cell(row, col++).Value = m.Name;
            ws.Cell(row, col++).Value = m.Description;
            ws.Cell(row, col++).Value = m.ManufacturerTemplateId;
            ws.Cell(row, col++).Value = m.MetaKeywords;
            ws.Cell(row, col++).Value = m.MetaDescription;
            ws.Cell(row, col++).Value = m.MetaTitle;
            ws.Cell(row, col++).Value = m.PictureId;
            ws.Cell(row, col++).Value = m.PageSize;
            ws.Cell(row, col++).Value = m.AllowCustomersToSelectPageSize;
            ws.Cell(row, col++).Value = m.PageSizeOptions;
            ws.Cell(row, col++).Value = m.PriceRanges;
            ws.Cell(row, col++).Value = m.Published;
            ws.Cell(row, col++).Value = m.Deleted;
            ws.Cell(row, col++).Value = m.DisplayOrder;
            ws.Cell(row, col++).Value = m.CreatedOnUtc.ToString("o");
            ws.Cell(row, col++).Value = m.UpdatedOnUtc.ToString("o");
            row++;
        }

        return SaveWorkbook(wb);
    }

    public string ExportNewsletterSubscribersToTxt(IList<NewsLetterSubscription> subscriptions)
    {
        var sb = new StringBuilder();
        foreach (var s in subscriptions)
            sb.AppendLine($"{s.Email},{s.Active},{s.StoreId}");
        return sb.ToString();
    }

    public string ExportStatesToTxt(IList<StateProvince> states)
    {
        var sb = new StringBuilder();
        foreach (var s in states)
            sb.AppendLine($"{s.CountryId},{s.Name},{s.Abbreviation},{s.Published},{s.DisplayOrder}");
        return sb.ToString();
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(184, 204, 228);
        }
    }

    private static byte[] SaveWorkbook(XLWorkbook wb)
    {
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
