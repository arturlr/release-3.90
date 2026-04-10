using System.Text;
using System.Xml;
using ClosedXML.Excel;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.ExportImport;

public partial class ExportManager
{
    public async Task<string> ExportCategoriestoXmlAsync()
    {
        var sb = new StringBuilder();
        using var stringWriter = new StringWriter(sb);
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, Async = true });
        await xmlWriter.WriteStartDocumentAsync();
        await xmlWriter.WriteStartElementAsync(null, "Categories", null);
        xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

        await WriteCategoriesAsync(xmlWriter, 0);

        await xmlWriter.WriteEndElementAsync();
        await xmlWriter.WriteEndDocumentAsync();
        await xmlWriter.FlushAsync();
        return sb.ToString();
    }

    public async Task<byte[]> ExportCategoriesToXlsxAsync(IList<Category> categories)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Categories");
        var headers = new[] { "Id", "Name", "Description", "CategoryTemplateId", "MetaKeywords", "MetaDescription", "MetaTitle", "ParentCategoryId", "PictureId", "PageSize", "AllowCustomersToSelectPageSize", "PageSizeOptions", "PriceRanges", "ShowOnHomePage", "IncludeInTopMenu", "Published", "Deleted", "DisplayOrder", "CreatedOnUtc", "UpdatedOnUtc" };
        WriteHeaders(ws, headers);

        var row = 2;
        foreach (var c in categories)
        {
            var col = 1;
            ws.Cell(row, col++).Value = c.Id;
            ws.Cell(row, col++).Value = c.Name;
            ws.Cell(row, col++).Value = c.Description;
            ws.Cell(row, col++).Value = c.CategoryTemplateId;
            ws.Cell(row, col++).Value = c.MetaKeywords;
            ws.Cell(row, col++).Value = c.MetaDescription;
            ws.Cell(row, col++).Value = c.MetaTitle;
            ws.Cell(row, col++).Value = c.ParentCategoryId;
            ws.Cell(row, col++).Value = c.PictureId;
            ws.Cell(row, col++).Value = c.PageSize;
            ws.Cell(row, col++).Value = c.AllowCustomersToSelectPageSize;
            ws.Cell(row, col++).Value = c.PageSizeOptions;
            ws.Cell(row, col++).Value = c.PriceRanges;
            ws.Cell(row, col++).Value = c.ShowOnHomePage;
            ws.Cell(row, col++).Value = c.IncludeInTopMenu;
            ws.Cell(row, col++).Value = c.Published;
            ws.Cell(row, col++).Value = c.Deleted;
            ws.Cell(row, col++).Value = c.DisplayOrder;
            ws.Cell(row, col++).Value = c.CreatedOnUtc.ToString("o");
            ws.Cell(row, col++).Value = c.UpdatedOnUtc.ToString("o");
            row++;
        }

        return await Task.FromResult(SaveWorkbook(wb));
    }

    public async Task<string> ExportProductsToXmlAsync(IList<Product> products)
    {
        var sb = new StringBuilder();
        using var stringWriter = new StringWriter(sb);
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, Async = true });
        await xmlWriter.WriteStartDocumentAsync();
        await xmlWriter.WriteStartElementAsync(null, "Products", null);
        xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

        foreach (var p in products)
        {
            await xmlWriter.WriteStartElementAsync(null, "Product", null);
            xmlWriter.WriteString("ProductId", p.Id);
            xmlWriter.WriteString("ProductTypeId", p.ProductTypeId);
            xmlWriter.WriteString("ParentGroupedProductId", p.ParentGroupedProductId);
            xmlWriter.WriteString("VisibleIndividually", p.VisibleIndividually);
            xmlWriter.WriteString("Name", p.Name);
            xmlWriter.WriteString("ShortDescription", p.ShortDescription);
            xmlWriter.WriteString("FullDescription", p.FullDescription);
            xmlWriter.WriteString("VendorId", p.VendorId);
            xmlWriter.WriteString("ProductTemplateId", p.ProductTemplateId);
            xmlWriter.WriteString("ShowOnHomePage", p.ShowOnHomePage);
            xmlWriter.WriteString("MetaKeywords", p.MetaKeywords);
            xmlWriter.WriteString("MetaDescription", p.MetaDescription);
            xmlWriter.WriteString("MetaTitle", p.MetaTitle);
            xmlWriter.WriteString("AllowCustomerReviews", p.AllowCustomerReviews);
            xmlWriter.WriteString("SKU", p.Sku);
            xmlWriter.WriteString("ManufacturerPartNumber", p.ManufacturerPartNumber);
            xmlWriter.WriteString("Gtin", p.Gtin);
            xmlWriter.WriteString("IsGiftCard", p.IsGiftCard);
            xmlWriter.WriteString("GiftCardTypeId", p.GiftCardTypeId);
            xmlWriter.WriteString("RequireOtherProducts", p.RequireOtherProducts);
            xmlWriter.WriteString("RequiredProductIds", p.RequiredProductIds);
            xmlWriter.WriteString("AutomaticallyAddRequiredProducts", p.AutomaticallyAddRequiredProducts);
            xmlWriter.WriteString("IsDownload", p.IsDownload);
            xmlWriter.WriteString("UnlimitedDownloads", p.UnlimitedDownloads);
            xmlWriter.WriteString("MaxNumberOfDownloads", p.MaxNumberOfDownloads);
            xmlWriter.WriteString("DownloadActivationTypeId", p.DownloadActivationTypeId);
            xmlWriter.WriteString("HasSampleDownload", p.HasSampleDownload);
            xmlWriter.WriteString("HasUserAgreement", p.HasUserAgreement);
            xmlWriter.WriteString("UserAgreementText", p.UserAgreementText);
            xmlWriter.WriteString("IsRecurring", p.IsRecurring);
            xmlWriter.WriteString("RecurringCycleLength", p.RecurringCycleLength);
            xmlWriter.WriteString("RecurringCyclePeriodId", p.RecurringCyclePeriodId);
            xmlWriter.WriteString("RecurringTotalCycles", p.RecurringTotalCycles);
            xmlWriter.WriteString("IsRental", p.IsRental);
            xmlWriter.WriteString("RentalPriceLength", p.RentalPriceLength);
            xmlWriter.WriteString("RentalPricePeriodId", p.RentalPricePeriodId);
            xmlWriter.WriteString("IsShipEnabled", p.IsShipEnabled);
            xmlWriter.WriteString("IsFreeShipping", p.IsFreeShipping);
            xmlWriter.WriteString("ShipSeparately", p.ShipSeparately);
            xmlWriter.WriteString("AdditionalShippingCharge", p.AdditionalShippingCharge);
            xmlWriter.WriteString("DeliveryDateId", p.DeliveryDateId);
            xmlWriter.WriteString("IsTaxExempt", p.IsTaxExempt);
            xmlWriter.WriteString("TaxCategoryId", p.TaxCategoryId);
            xmlWriter.WriteString("ManageInventoryMethodId", p.ManageInventoryMethodId);
            xmlWriter.WriteString("StockQuantity", p.StockQuantity);
            xmlWriter.WriteString("DisplayStockAvailability", p.DisplayStockAvailability);
            xmlWriter.WriteString("DisplayStockQuantity", p.DisplayStockQuantity);
            xmlWriter.WriteString("MinStockQuantity", p.MinStockQuantity);
            xmlWriter.WriteString("LowStockActivityId", p.LowStockActivityId);
            xmlWriter.WriteString("NotifyAdminForQuantityBelow", p.NotifyAdminForQuantityBelow);
            xmlWriter.WriteString("BackorderModeId", p.BackorderModeId);
            xmlWriter.WriteString("AllowBackInStockSubscriptions", p.AllowBackInStockSubscriptions);
            xmlWriter.WriteString("OrderMinimumQuantity", p.OrderMinimumQuantity);
            xmlWriter.WriteString("OrderMaximumQuantity", p.OrderMaximumQuantity);
            xmlWriter.WriteString("AllowedQuantities", p.AllowedQuantities);
            xmlWriter.WriteString("DisableBuyButton", p.DisableBuyButton);
            xmlWriter.WriteString("DisableWishlistButton", p.DisableWishlistButton);
            xmlWriter.WriteString("AvailableForPreOrder", p.AvailableForPreOrder);
            xmlWriter.WriteString("PreOrderAvailabilityStartDateTimeUtc", p.PreOrderAvailabilityStartDateTimeUtc);
            xmlWriter.WriteString("CallForPrice", p.CallForPrice);
            xmlWriter.WriteString("Price", p.Price);
            xmlWriter.WriteString("OldPrice", p.OldPrice);
            xmlWriter.WriteString("ProductCost", p.ProductCost);
            xmlWriter.WriteString("MarkAsNew", p.MarkAsNew);
            xmlWriter.WriteString("MarkAsNewStartDateTimeUtc", p.MarkAsNewStartDateTimeUtc);
            xmlWriter.WriteString("MarkAsNewEndDateTimeUtc", p.MarkAsNewEndDateTimeUtc);
            xmlWriter.WriteString("Weight", p.Weight);
            xmlWriter.WriteString("Length", p.Length);
            xmlWriter.WriteString("Width", p.Width);
            xmlWriter.WriteString("Height", p.Height);
            xmlWriter.WriteString("Published", p.Published);
            xmlWriter.WriteString("CreatedOnUtc", p.CreatedOnUtc);
            xmlWriter.WriteString("UpdatedOnUtc", p.UpdatedOnUtc);

            // product categories
            await xmlWriter.WriteStartElementAsync(null, "ProductCategories", null);
            var productCategories = await categoryService.GetProductCategoriesByProductIdAsync(p.Id, true);
            foreach (var pc in productCategories)
            {
                await xmlWriter.WriteStartElementAsync(null, "ProductCategory", null);
                xmlWriter.WriteString("ProductCategoryId", pc.Id);
                xmlWriter.WriteString("CategoryId", pc.CategoryId);
                xmlWriter.WriteString("IsFeaturedProduct", pc.IsFeaturedProduct);
                xmlWriter.WriteString("DisplayOrder", pc.DisplayOrder);
                await xmlWriter.WriteEndElementAsync();
            }
            await xmlWriter.WriteEndElementAsync();

            // product manufacturers
            await xmlWriter.WriteStartElementAsync(null, "ProductManufacturers", null);
            var productManufacturers = await manufacturerService.GetProductManufacturersByProductIdAsync(p.Id, true);
            foreach (var pm in productManufacturers)
            {
                await xmlWriter.WriteStartElementAsync(null, "ProductManufacturer", null);
                xmlWriter.WriteString("ProductManufacturerId", pm.Id);
                xmlWriter.WriteString("ManufacturerId", pm.ManufacturerId);
                xmlWriter.WriteString("IsFeaturedProduct", pm.IsFeaturedProduct);
                xmlWriter.WriteString("DisplayOrder", pm.DisplayOrder);
                await xmlWriter.WriteEndElementAsync();
            }
            await xmlWriter.WriteEndElementAsync();

            // product pictures
            await xmlWriter.WriteStartElementAsync(null, "ProductPictures", null);
            var productPictures = await pictureService.GetPicturesByProductIdAsync(p.Id);
            foreach (var pic in productPictures)
            {
                await xmlWriter.WriteStartElementAsync(null, "ProductPicture", null);
                xmlWriter.WriteString("PictureId", pic.Id);
                xmlWriter.WriteString("MimeType", pic.MimeType);
                xmlWriter.WriteString("SeoFilename", pic.SeoFilename);
                await xmlWriter.WriteEndElementAsync();
            }
            await xmlWriter.WriteEndElementAsync();

            await xmlWriter.WriteEndElementAsync(); // Product
        }

        await xmlWriter.WriteEndElementAsync();
        await xmlWriter.WriteEndDocumentAsync();
        await xmlWriter.FlushAsync();
        return sb.ToString();
    }

    public async Task<byte[]> ExportProductsToXlsxAsync(IList<Product> products)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");
        var headers = new[] { "Id", "ProductTypeId", "ParentGroupedProductId", "VisibleIndividually", "Name", "ShortDescription", "FullDescription", "VendorId", "ProductTemplateId", "ShowOnHomePage", "MetaKeywords", "MetaDescription", "MetaTitle", "AllowCustomerReviews", "Published", "SKU", "ManufacturerPartNumber", "Gtin", "IsGiftCard", "GiftCardTypeId", "RequireOtherProducts", "RequiredProductIds", "AutomaticallyAddRequiredProducts", "IsDownload", "UnlimitedDownloads", "MaxNumberOfDownloads", "DownloadActivationTypeId", "HasSampleDownload", "HasUserAgreement", "IsRecurring", "RecurringCycleLength", "RecurringCyclePeriodId", "RecurringTotalCycles", "IsRental", "RentalPriceLength", "RentalPricePeriodId", "IsShipEnabled", "IsFreeShipping", "ShipSeparately", "AdditionalShippingCharge", "DeliveryDateId", "IsTaxExempt", "TaxCategoryId", "ManageInventoryMethodId", "StockQuantity", "DisplayStockAvailability", "DisplayStockQuantity", "MinStockQuantity", "LowStockActivityId", "NotifyAdminForQuantityBelow", "BackorderModeId", "AllowBackInStockSubscriptions", "OrderMinimumQuantity", "OrderMaximumQuantity", "AllowedQuantities", "DisableBuyButton", "DisableWishlistButton", "AvailableForPreOrder", "CallForPrice", "Price", "OldPrice", "ProductCost", "MarkAsNew", "Weight", "Length", "Width", "Height", "CreatedOnUtc", "UpdatedOnUtc", "Categories", "Manufacturers", "ProductTags", "Picture1", "Picture2", "Picture3" };
        WriteHeaders(ws, headers);

        var row = 2;
        foreach (var p in products)
        {
            var col = 1;
            ws.Cell(row, col++).Value = p.Id;
            ws.Cell(row, col++).Value = p.ProductTypeId;
            ws.Cell(row, col++).Value = p.ParentGroupedProductId;
            ws.Cell(row, col++).Value = p.VisibleIndividually;
            ws.Cell(row, col++).Value = p.Name;
            ws.Cell(row, col++).Value = p.ShortDescription;
            ws.Cell(row, col++).Value = p.FullDescription;
            ws.Cell(row, col++).Value = p.VendorId;
            ws.Cell(row, col++).Value = p.ProductTemplateId;
            ws.Cell(row, col++).Value = p.ShowOnHomePage;
            ws.Cell(row, col++).Value = p.MetaKeywords;
            ws.Cell(row, col++).Value = p.MetaDescription;
            ws.Cell(row, col++).Value = p.MetaTitle;
            ws.Cell(row, col++).Value = p.AllowCustomerReviews;
            ws.Cell(row, col++).Value = p.Published;
            ws.Cell(row, col++).Value = p.Sku;
            ws.Cell(row, col++).Value = p.ManufacturerPartNumber;
            ws.Cell(row, col++).Value = p.Gtin;
            ws.Cell(row, col++).Value = p.IsGiftCard;
            ws.Cell(row, col++).Value = p.GiftCardTypeId;
            ws.Cell(row, col++).Value = p.RequireOtherProducts;
            ws.Cell(row, col++).Value = p.RequiredProductIds;
            ws.Cell(row, col++).Value = p.AutomaticallyAddRequiredProducts;
            ws.Cell(row, col++).Value = p.IsDownload;
            ws.Cell(row, col++).Value = p.UnlimitedDownloads;
            ws.Cell(row, col++).Value = p.MaxNumberOfDownloads;
            ws.Cell(row, col++).Value = p.DownloadActivationTypeId;
            ws.Cell(row, col++).Value = p.HasSampleDownload;
            ws.Cell(row, col++).Value = p.HasUserAgreement;
            ws.Cell(row, col++).Value = p.IsRecurring;
            ws.Cell(row, col++).Value = p.RecurringCycleLength;
            ws.Cell(row, col++).Value = p.RecurringCyclePeriodId;
            ws.Cell(row, col++).Value = p.RecurringTotalCycles;
            ws.Cell(row, col++).Value = p.IsRental;
            ws.Cell(row, col++).Value = p.RentalPriceLength;
            ws.Cell(row, col++).Value = p.RentalPricePeriodId;
            ws.Cell(row, col++).Value = p.IsShipEnabled;
            ws.Cell(row, col++).Value = p.IsFreeShipping;
            ws.Cell(row, col++).Value = p.ShipSeparately;
            ws.Cell(row, col++).Value = (double)p.AdditionalShippingCharge;
            ws.Cell(row, col++).Value = p.DeliveryDateId;
            ws.Cell(row, col++).Value = p.IsTaxExempt;
            ws.Cell(row, col++).Value = p.TaxCategoryId;
            ws.Cell(row, col++).Value = p.ManageInventoryMethodId;
            ws.Cell(row, col++).Value = p.StockQuantity;
            ws.Cell(row, col++).Value = p.DisplayStockAvailability;
            ws.Cell(row, col++).Value = p.DisplayStockQuantity;
            ws.Cell(row, col++).Value = p.MinStockQuantity;
            ws.Cell(row, col++).Value = p.LowStockActivityId;
            ws.Cell(row, col++).Value = p.NotifyAdminForQuantityBelow;
            ws.Cell(row, col++).Value = p.BackorderModeId;
            ws.Cell(row, col++).Value = p.AllowBackInStockSubscriptions;
            ws.Cell(row, col++).Value = p.OrderMinimumQuantity;
            ws.Cell(row, col++).Value = p.OrderMaximumQuantity;
            ws.Cell(row, col++).Value = p.AllowedQuantities;
            ws.Cell(row, col++).Value = p.DisableBuyButton;
            ws.Cell(row, col++).Value = p.DisableWishlistButton;
            ws.Cell(row, col++).Value = p.AvailableForPreOrder;
            ws.Cell(row, col++).Value = p.CallForPrice;
            ws.Cell(row, col++).Value = (double)p.Price;
            ws.Cell(row, col++).Value = (double)p.OldPrice;
            ws.Cell(row, col++).Value = (double)p.ProductCost;
            ws.Cell(row, col++).Value = p.MarkAsNew;
            ws.Cell(row, col++).Value = (double)p.Weight;
            ws.Cell(row, col++).Value = (double)p.Length;
            ws.Cell(row, col++).Value = (double)p.Width;
            ws.Cell(row, col++).Value = (double)p.Height;
            ws.Cell(row, col++).Value = p.CreatedOnUtc.ToString("o");
            ws.Cell(row, col++).Value = p.UpdatedOnUtc.ToString("o");

            // categories
            var categories = await categoryService.GetProductCategoriesByProductIdAsync(p.Id, true);
            var categoryNames = string.Join(";", categories.Select(pc =>
            {
                var cat = categoryService.GetCategoryByIdAsync(pc.CategoryId).GetAwaiter().GetResult();
                return cat?.Name ?? string.Empty;
            }));
            ws.Cell(row, col++).Value = categoryNames;

            // manufacturers
            var manufacturers = await manufacturerService.GetProductManufacturersByProductIdAsync(p.Id, true);
            var manufacturerNames = string.Join(";", manufacturers.Select(pm =>
            {
                var mfr = manufacturerService.GetManufacturerByIdAsync(pm.ManufacturerId).GetAwaiter().GetResult();
                return mfr?.Name ?? string.Empty;
            }));
            ws.Cell(row, col++).Value = manufacturerNames;

            // product tags
            var tags = await productTagService.GetProductTagsByProductIdAsync(p.Id);
            ws.Cell(row, col++).Value = string.Join(";", tags.Select(t => t.Name));

            // pictures (up to 3)
            var pictures = await pictureService.GetPicturesByProductIdAsync(p.Id, 3);
            ws.Cell(row, col++).Value = pictures.Count > 0 ? await pictureService.GetThumbLocalPathAsync(pictures[0]) : string.Empty;
            ws.Cell(row, col++).Value = pictures.Count > 1 ? await pictureService.GetThumbLocalPathAsync(pictures[1]) : string.Empty;
            ws.Cell(row, col++).Value = pictures.Count > 2 ? await pictureService.GetThumbLocalPathAsync(pictures[2]) : string.Empty;

            row++;
        }

        return SaveWorkbook(wb);
    }

    public async Task<byte[]> ExportOrdersToXlsxAsync(IList<Order> orders)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Orders");
        var headers = new[] { "OrderId", "OrderGuid", "CustomerId", "OrderStatusId", "PaymentStatusId", "ShippingStatusId", "OrderSubtotalInclTax", "OrderSubtotalExclTax", "OrderSubTotalDiscountInclTax", "OrderSubTotalDiscountExclTax", "OrderShippingInclTax", "OrderShippingExclTax", "PaymentMethodAdditionalFeeInclTax", "PaymentMethodAdditionalFeeExclTax", "OrderTax", "OrderTotal", "OrderDiscount", "CurrencyRate", "CustomerCurrencyCode", "PaymentMethodSystemName", "ShippingMethod", "CreatedOnUtc" };
        WriteHeaders(ws, headers);

        var row = 2;
        foreach (var o in orders)
        {
            var col = 1;
            ws.Cell(row, col++).Value = o.Id;
            ws.Cell(row, col++).Value = o.OrderGuid.ToString();
            ws.Cell(row, col++).Value = o.CustomerId;
            ws.Cell(row, col++).Value = o.OrderStatusId;
            ws.Cell(row, col++).Value = o.PaymentStatusId;
            ws.Cell(row, col++).Value = o.ShippingStatusId;
            ws.Cell(row, col++).Value = (double)o.OrderSubtotalInclTax;
            ws.Cell(row, col++).Value = (double)o.OrderSubtotalExclTax;
            ws.Cell(row, col++).Value = (double)o.OrderSubTotalDiscountInclTax;
            ws.Cell(row, col++).Value = (double)o.OrderSubTotalDiscountExclTax;
            ws.Cell(row, col++).Value = (double)o.OrderShippingInclTax;
            ws.Cell(row, col++).Value = (double)o.OrderShippingExclTax;
            ws.Cell(row, col++).Value = (double)o.PaymentMethodAdditionalFeeInclTax;
            ws.Cell(row, col++).Value = (double)o.PaymentMethodAdditionalFeeExclTax;
            ws.Cell(row, col++).Value = (double)o.OrderTax;
            ws.Cell(row, col++).Value = (double)o.OrderTotal;
            ws.Cell(row, col++).Value = (double)o.OrderDiscount;
            ws.Cell(row, col++).Value = (double)o.CurrencyRate;
            ws.Cell(row, col++).Value = o.CustomerCurrencyCode;
            ws.Cell(row, col++).Value = o.PaymentMethodSystemName;
            ws.Cell(row, col++).Value = o.ShippingMethod;
            ws.Cell(row, col++).Value = o.CreatedOnUtc.ToString("o");

            // order items as sub-rows
            var orderItems = await orderService.GetOrderItemsByOrderIdAsync(o.Id);
            foreach (var oi in orderItems)
            {
                row++;
                var product = await productService.GetProductByIdAsync(oi.ProductId);
                ws.Cell(row, 2).Value = product?.Name ?? string.Empty;
                ws.Cell(row, 3).Value = product?.Sku ?? string.Empty;
                ws.Cell(row, 4).Value = oi.Quantity;
                ws.Cell(row, 5).Value = (double)oi.UnitPriceExclTax;
                ws.Cell(row, 6).Value = (double)oi.PriceExclTax;
            }

            row++;
        }

        return SaveWorkbook(wb);
    }

    public async Task<byte[]> ExportCustomersToXlsxAsync(IList<Customer> customers)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Customers");
        var headers = new[] { "Id", "CustomerGuid", "Email", "Username", "Active", "CreatedOnUtc", "LastActivityDateUtc" };
        WriteHeaders(ws, headers);

        var row = 2;
        foreach (var c in customers)
        {
            var col = 1;
            ws.Cell(row, col++).Value = c.Id;
            ws.Cell(row, col++).Value = c.CustomerGuid.ToString();
            ws.Cell(row, col++).Value = c.Email;
            ws.Cell(row, col++).Value = c.Username;
            ws.Cell(row, col++).Value = c.Active;
            ws.Cell(row, col++).Value = c.CreatedOnUtc.ToString("o");
            ws.Cell(row, col++).Value = c.LastActivityDateUtc.ToString("o");
            row++;
        }

        return await Task.FromResult(SaveWorkbook(wb));
    }

    private async Task WriteCategoriesAsync(XmlWriter xmlWriter, int parentCategoryId)
    {
        var categories = await categoryService.GetAllCategoriesByParentCategoryIdAsync(parentCategoryId, true);
        foreach (var c in categories)
        {
            await xmlWriter.WriteStartElementAsync(null, "Category", null);
            xmlWriter.WriteString("Id", c.Id);
            xmlWriter.WriteString("Name", c.Name);
            xmlWriter.WriteString("Description", c.Description);
            xmlWriter.WriteString("CategoryTemplateId", c.CategoryTemplateId);
            xmlWriter.WriteString("MetaKeywords", c.MetaKeywords);
            xmlWriter.WriteString("MetaDescription", c.MetaDescription);
            xmlWriter.WriteString("MetaTitle", c.MetaTitle);
            xmlWriter.WriteString("ParentCategoryId", c.ParentCategoryId);
            xmlWriter.WriteString("PictureId", c.PictureId);
            xmlWriter.WriteString("PageSize", c.PageSize);
            xmlWriter.WriteString("AllowCustomersToSelectPageSize", c.AllowCustomersToSelectPageSize);
            xmlWriter.WriteString("PageSizeOptions", c.PageSizeOptions);
            xmlWriter.WriteString("PriceRanges", c.PriceRanges);
            xmlWriter.WriteString("ShowOnHomePage", c.ShowOnHomePage);
            xmlWriter.WriteString("IncludeInTopMenu", c.IncludeInTopMenu);
            xmlWriter.WriteString("Published", c.Published);
            xmlWriter.WriteString("Deleted", c.Deleted);
            xmlWriter.WriteString("DisplayOrder", c.DisplayOrder);
            xmlWriter.WriteString("CreatedOnUtc", c.CreatedOnUtc);
            xmlWriter.WriteString("UpdatedOnUtc", c.UpdatedOnUtc);

            await xmlWriter.WriteStartElementAsync(null, "SubCategories", null);
            await WriteCategoriesAsync(xmlWriter, c.Id);
            await xmlWriter.WriteEndElementAsync();

            await xmlWriter.WriteEndElementAsync();
        }
    }
}
