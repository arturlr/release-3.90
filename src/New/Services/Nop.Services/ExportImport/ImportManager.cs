using ClosedXML.Excel;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Messages;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Seo;

namespace Nop.Services.ExportImport;

public partial class ImportManager(
    IProductService productService,
    ICategoryService categoryService,
    IManufacturerService manufacturerService,
    IPictureService pictureService,
    IUrlRecordService urlRecordService,
    INewsLetterSubscriptionService newsLetterSubscriptionService,
    ICountryService countryService,
    IStateProvinceService stateProvinceService,
    IProductTagService productTagService) : IImportManager
{
    public async Task ImportProductsFromXlsxAsync(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault()
            ?? throw new NopException("No worksheet found");

        var headers = ReadHeaders(ws);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            var sku = GetCellString(ws, row, headers, "SKU");
            if (string.IsNullOrWhiteSpace(sku))
                continue;

            var product = await productService.GetProductBySkuAsync(sku);
            var isNew = product is null;
            product ??= new Product { CreatedOnUtc = DateTime.UtcNow };

            SetProductFields(ws, row, headers, product);
            product.UpdatedOnUtc = DateTime.UtcNow;

            if (isNew)
            {
                if (product.ProductTypeId == 0)
                    product.ProductTypeId = (int)ProductType.SimpleProduct;
                if (!product.VisibleIndividually)
                    product.VisibleIndividually = true;
                if (!product.Published)
                    product.Published = true;

                await productService.InsertProductAsync(product);
            }
            else
            {
                await productService.UpdateProductAsync(product);
            }

            // SEO slug
            var seName = GetCellString(ws, row, headers, "SeName");
            if (!string.IsNullOrEmpty(seName))
            {
                var validSeName = await product.ValidateSeNameAsync(
                    seName, product.Name ?? string.Empty, true,
                    urlRecordService, new Nop.Core.Domain.Seo.SeoSettings());
                await urlRecordService.SaveSlugAsync(product, validSeName, 0);
            }

            // categories
            var categoryNames = GetCellString(ws, row, headers, "Categories");
            if (!string.IsNullOrEmpty(categoryNames))
                await ImportProductCategoriesAsync(product, categoryNames, isNew);

            // manufacturers
            var manufacturerNames = GetCellString(ws, row, headers, "Manufacturers");
            if (!string.IsNullOrEmpty(manufacturerNames))
                await ImportProductManufacturersAsync(product, manufacturerNames, isNew);

            // pictures
            await ImportProductPicturesAsync(product, ws, row, headers, isNew);

            // tags
            var tagNames = GetCellString(ws, row, headers, "ProductTags");
            if (!string.IsNullOrEmpty(tagNames))
                await ImportProductTagsAsync(product, tagNames);
        }
    }

    public async Task ImportManufacturersFromXlsxAsync(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault()
            ?? throw new NopException("No worksheet found");

        var headers = ReadHeaders(ws);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            var name = GetCellString(ws, row, headers, "Name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var allManufacturers = await manufacturerService.GetAllManufacturersAsync(showHidden: true);
            var manufacturer = allManufacturers.FirstOrDefault(m =>
                m.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
            var isNew = manufacturer is null;
            manufacturer ??= new Manufacturer { CreatedOnUtc = DateTime.UtcNow };

            manufacturer.Name = name;
            manufacturer.Description = GetCellString(ws, row, headers, "Description");
            manufacturer.ManufacturerTemplateId = GetCellInt(ws, row, headers, "ManufacturerTemplateId");
            manufacturer.MetaKeywords = GetCellString(ws, row, headers, "MetaKeywords");
            manufacturer.MetaDescription = GetCellString(ws, row, headers, "MetaDescription");
            manufacturer.MetaTitle = GetCellString(ws, row, headers, "MetaTitle");
            manufacturer.PictureId = GetCellInt(ws, row, headers, "PictureId");
            manufacturer.PageSize = GetCellInt(ws, row, headers, "PageSize", 6);
            manufacturer.AllowCustomersToSelectPageSize = GetCellBool(ws, row, headers, "AllowCustomersToSelectPageSize", true);
            manufacturer.PageSizeOptions = GetCellString(ws, row, headers, "PageSizeOptions");
            manufacturer.Published = GetCellBool(ws, row, headers, "Published", true);
            manufacturer.DisplayOrder = GetCellInt(ws, row, headers, "DisplayOrder");
            manufacturer.UpdatedOnUtc = DateTime.UtcNow;

            if (isNew)
                await manufacturerService.InsertManufacturerAsync(manufacturer);
            else
                await manufacturerService.UpdateManufacturerAsync(manufacturer);
        }
    }

    public async Task ImportCategoriesFromXlsxAsync(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault()
            ?? throw new NopException("No worksheet found");

        var headers = ReadHeaders(ws);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            var name = GetCellString(ws, row, headers, "Name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var allCategories = await categoryService.GetAllCategoriesAsync(showHidden: true);
            var category = allCategories.FirstOrDefault(c =>
                c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
            var isNew = category is null;
            category ??= new Category { CreatedOnUtc = DateTime.UtcNow };

            category.Name = name;
            category.Description = GetCellString(ws, row, headers, "Description");
            category.CategoryTemplateId = GetCellInt(ws, row, headers, "CategoryTemplateId");
            category.MetaKeywords = GetCellString(ws, row, headers, "MetaKeywords");
            category.MetaDescription = GetCellString(ws, row, headers, "MetaDescription");
            category.MetaTitle = GetCellString(ws, row, headers, "MetaTitle");
            category.ParentCategoryId = GetCellInt(ws, row, headers, "ParentCategoryId");
            category.PictureId = GetCellInt(ws, row, headers, "PictureId");
            category.PageSize = GetCellInt(ws, row, headers, "PageSize", 6);
            category.AllowCustomersToSelectPageSize = GetCellBool(ws, row, headers, "AllowCustomersToSelectPageSize", true);
            category.PageSizeOptions = GetCellString(ws, row, headers, "PageSizeOptions");
            category.ShowOnHomePage = GetCellBool(ws, row, headers, "ShowOnHomePage");
            category.IncludeInTopMenu = GetCellBool(ws, row, headers, "IncludeInTopMenu", true);
            category.Published = GetCellBool(ws, row, headers, "Published", true);
            category.DisplayOrder = GetCellInt(ws, row, headers, "DisplayOrder");
            category.UpdatedOnUtc = DateTime.UtcNow;

            if (isNew)
                await categoryService.InsertCategoryAsync(category);
            else
                await categoryService.UpdateCategoryAsync(category);
        }
    }

    public async Task<int> ImportNewsletterSubscribersFromTxtAsync(Stream stream)
    {
        var count = 0;
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 1)
                continue;

            var email = parts[0].Trim();
            if (string.IsNullOrEmpty(email))
                continue;

            var active = parts.Length > 1 && bool.TryParse(parts[1].Trim(), out var a) && a;
            var storeId = parts.Length > 2 && int.TryParse(parts[2].Trim(), out var s) ? s : 0;

            var existing = await newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(email, storeId);
            if (existing is not null)
            {
                existing.Active = active;
                await newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(existing);
            }
            else
            {
                await newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
                {
                    Active = active,
                    Email = email,
                    StoreId = storeId,
                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
            count++;
        }
        return count;
    }

    public async Task<int> ImportStatesFromTxtAsync(Stream stream)
    {
        var count = 0;
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 3)
                continue;

            if (!int.TryParse(parts[0].Trim(), out var countryId))
                continue;

            var name = parts[1].Trim();
            var abbreviation = parts[2].Trim();
            var published = parts.Length > 3 && bool.TryParse(parts[3].Trim(), out var p) && p;
            var displayOrder = parts.Length > 4 && int.TryParse(parts[4].Trim(), out var d) ? d : 0;

            var country = await countryService.GetCountryByIdAsync(countryId);
            if (country is null)
                continue;

            var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(countryId);
            var existing = states.FirstOrDefault(sp =>
                sp.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

            if (existing is not null)
            {
                existing.Abbreviation = abbreviation;
                existing.Published = published;
                existing.DisplayOrder = displayOrder;
                await stateProvinceService.UpdateStateProvinceAsync(existing);
            }
            else
            {
                await stateProvinceService.InsertStateProvinceAsync(new StateProvince
                {
                    CountryId = countryId,
                    Name = name,
                    Abbreviation = abbreviation,
                    Published = published,
                    DisplayOrder = displayOrder
                });
            }
            count++;
        }
        return count;
    }

    #region Helpers

    private static Dictionary<string, int> ReadHeaders(IXLWorksheet ws)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = ws.Row(1);
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var col = 1; col <= lastCol; col++)
        {
            var val = headerRow.Cell(col).GetString();
            if (!string.IsNullOrWhiteSpace(val))
                headers[val] = col;
        }
        return headers;
    }

    private static string GetCellString(IXLWorksheet ws, int row, Dictionary<string, int> headers, string column)
    {
        if (!headers.TryGetValue(column, out var col))
            return string.Empty;
        return ws.Cell(row, col).GetString();
    }

    private static int GetCellInt(IXLWorksheet ws, int row, Dictionary<string, int> headers, string column, int defaultValue = 0)
    {
        var s = GetCellString(ws, row, headers, column);
        return int.TryParse(s, out var v) ? v : defaultValue;
    }

    private static bool GetCellBool(IXLWorksheet ws, int row, Dictionary<string, int> headers, string column, bool defaultValue = false)
    {
        var s = GetCellString(ws, row, headers, column);
        return bool.TryParse(s, out var v) ? v : defaultValue;
    }

    private static decimal GetCellDecimal(IXLWorksheet ws, int row, Dictionary<string, int> headers, string column)
    {
        var s = GetCellString(ws, row, headers, column);
        return decimal.TryParse(s, out var v) ? v : 0m;
    }

    private static void SetProductFields(IXLWorksheet ws, int row, Dictionary<string, int> headers, Product p)
    {
        p.ProductTypeId = GetCellInt(ws, row, headers, "ProductTypeId", p.ProductTypeId);
        p.ParentGroupedProductId = GetCellInt(ws, row, headers, "ParentGroupedProductId");
        p.VisibleIndividually = GetCellBool(ws, row, headers, "VisibleIndividually", p.VisibleIndividually);
        p.Name = GetCellString(ws, row, headers, "Name");
        p.ShortDescription = GetCellString(ws, row, headers, "ShortDescription");
        p.FullDescription = GetCellString(ws, row, headers, "FullDescription");
        p.VendorId = GetCellInt(ws, row, headers, "VendorId");
        p.ProductTemplateId = GetCellInt(ws, row, headers, "ProductTemplateId");
        p.ShowOnHomePage = GetCellBool(ws, row, headers, "ShowOnHomePage");
        p.MetaKeywords = GetCellString(ws, row, headers, "MetaKeywords");
        p.MetaDescription = GetCellString(ws, row, headers, "MetaDescription");
        p.MetaTitle = GetCellString(ws, row, headers, "MetaTitle");
        p.AllowCustomerReviews = GetCellBool(ws, row, headers, "AllowCustomerReviews", true);
        p.Published = GetCellBool(ws, row, headers, "Published", p.Published);
        p.Sku = GetCellString(ws, row, headers, "SKU");
        p.ManufacturerPartNumber = GetCellString(ws, row, headers, "ManufacturerPartNumber");
        p.Gtin = GetCellString(ws, row, headers, "Gtin");
        p.IsGiftCard = GetCellBool(ws, row, headers, "IsGiftCard");
        p.GiftCardTypeId = GetCellInt(ws, row, headers, "GiftCardTypeId");
        p.RequireOtherProducts = GetCellBool(ws, row, headers, "RequireOtherProducts");
        p.RequiredProductIds = GetCellString(ws, row, headers, "RequiredProductIds");
        p.AutomaticallyAddRequiredProducts = GetCellBool(ws, row, headers, "AutomaticallyAddRequiredProducts");
        p.IsDownload = GetCellBool(ws, row, headers, "IsDownload");
        p.UnlimitedDownloads = GetCellBool(ws, row, headers, "UnlimitedDownloads", true);
        p.MaxNumberOfDownloads = GetCellInt(ws, row, headers, "MaxNumberOfDownloads", 10);
        p.DownloadActivationTypeId = GetCellInt(ws, row, headers, "DownloadActivationTypeId");
        p.HasSampleDownload = GetCellBool(ws, row, headers, "HasSampleDownload");
        p.HasUserAgreement = GetCellBool(ws, row, headers, "HasUserAgreement");
        p.IsRecurring = GetCellBool(ws, row, headers, "IsRecurring");
        p.RecurringCycleLength = GetCellInt(ws, row, headers, "RecurringCycleLength", 100);
        p.RecurringCyclePeriodId = GetCellInt(ws, row, headers, "RecurringCyclePeriodId");
        p.RecurringTotalCycles = GetCellInt(ws, row, headers, "RecurringTotalCycles", 10);
        p.IsRental = GetCellBool(ws, row, headers, "IsRental");
        p.RentalPriceLength = GetCellInt(ws, row, headers, "RentalPriceLength", 1);
        p.RentalPricePeriodId = GetCellInt(ws, row, headers, "RentalPricePeriodId");
        p.IsShipEnabled = GetCellBool(ws, row, headers, "IsShipEnabled", true);
        p.IsFreeShipping = GetCellBool(ws, row, headers, "IsFreeShipping");
        p.ShipSeparately = GetCellBool(ws, row, headers, "ShipSeparately");
        p.AdditionalShippingCharge = GetCellDecimal(ws, row, headers, "AdditionalShippingCharge");
        p.DeliveryDateId = GetCellInt(ws, row, headers, "DeliveryDateId");
        p.IsTaxExempt = GetCellBool(ws, row, headers, "IsTaxExempt");
        p.TaxCategoryId = GetCellInt(ws, row, headers, "TaxCategoryId");
        p.ManageInventoryMethodId = GetCellInt(ws, row, headers, "ManageInventoryMethodId");
        p.StockQuantity = GetCellInt(ws, row, headers, "StockQuantity", 10000);
        p.DisplayStockAvailability = GetCellBool(ws, row, headers, "DisplayStockAvailability");
        p.DisplayStockQuantity = GetCellBool(ws, row, headers, "DisplayStockQuantity");
        p.MinStockQuantity = GetCellInt(ws, row, headers, "MinStockQuantity");
        p.LowStockActivityId = GetCellInt(ws, row, headers, "LowStockActivityId");
        p.NotifyAdminForQuantityBelow = GetCellInt(ws, row, headers, "NotifyAdminForQuantityBelow", 1);
        p.BackorderModeId = GetCellInt(ws, row, headers, "BackorderModeId");
        p.AllowBackInStockSubscriptions = GetCellBool(ws, row, headers, "AllowBackInStockSubscriptions");
        p.OrderMinimumQuantity = GetCellInt(ws, row, headers, "OrderMinimumQuantity", 1);
        p.OrderMaximumQuantity = GetCellInt(ws, row, headers, "OrderMaximumQuantity", 10000);
        p.AllowedQuantities = GetCellString(ws, row, headers, "AllowedQuantities");
        p.DisableBuyButton = GetCellBool(ws, row, headers, "DisableBuyButton");
        p.DisableWishlistButton = GetCellBool(ws, row, headers, "DisableWishlistButton");
        p.AvailableForPreOrder = GetCellBool(ws, row, headers, "AvailableForPreOrder");
        p.CallForPrice = GetCellBool(ws, row, headers, "CallForPrice");
        p.Price = GetCellDecimal(ws, row, headers, "Price");
        p.OldPrice = GetCellDecimal(ws, row, headers, "OldPrice");
        p.ProductCost = GetCellDecimal(ws, row, headers, "ProductCost");
        p.MarkAsNew = GetCellBool(ws, row, headers, "MarkAsNew");
        p.Weight = GetCellDecimal(ws, row, headers, "Weight");
        p.Length = GetCellDecimal(ws, row, headers, "Length");
        p.Width = GetCellDecimal(ws, row, headers, "Width");
        p.Height = GetCellDecimal(ws, row, headers, "Height");
    }

    private async Task ImportProductCategoriesAsync(Product product, string categoryNames, bool isNew)
    {
        var allCategories = await categoryService.GetAllCategoriesAsync(showHidden: true);
        var existingMappings = isNew
            ? []
            : await categoryService.GetProductCategoriesByProductIdAsync(product.Id, true);
        var existingCategoryIds = existingMappings.Select(pc => pc.CategoryId).ToHashSet();

        foreach (var name in categoryNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var category = allCategories.FirstOrDefault(c =>
                c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
            if (category is null || existingCategoryIds.Contains(category.Id))
                continue;

            await categoryService.InsertProductCategoryAsync(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = category.Id,
                DisplayOrder = 1
            });
        }
    }

    private async Task ImportProductManufacturersAsync(Product product, string manufacturerNames, bool isNew)
    {
        var allManufacturers = await manufacturerService.GetAllManufacturersAsync(showHidden: true);
        var existingMappings = isNew
            ? []
            : await manufacturerService.GetProductManufacturersByProductIdAsync(product.Id, true);
        var existingManufacturerIds = existingMappings.Select(pm => pm.ManufacturerId).ToHashSet();

        foreach (var name in manufacturerNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var manufacturer = allManufacturers.FirstOrDefault(m =>
                m.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
            if (manufacturer is null || existingManufacturerIds.Contains(manufacturer.Id))
                continue;

            await manufacturerService.InsertProductManufacturerAsync(new ProductManufacturer
            {
                ProductId = product.Id,
                ManufacturerId = manufacturer.Id,
                DisplayOrder = 1
            });
        }
    }

    private async Task ImportProductPicturesAsync(Product product, IXLWorksheet ws, int row, Dictionary<string, int> headers, bool isNew)
    {
        foreach (var picCol in new[] { "Picture1", "Picture2", "Picture3" })
        {
            var picturePath = GetCellString(ws, row, headers, picCol);
            if (string.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
                continue;

            var mimeType = GetMimeTypeFromPath(picturePath);
            var pictureBinary = await File.ReadAllBytesAsync(picturePath);
            var newPicture = await pictureService.InsertPictureAsync(pictureBinary, mimeType,
                pictureService.GetPictureSeName(product.Name ?? "product"));

            await productService.InsertProductPictureAsync(new ProductPicture
            {
                ProductId = product.Id,
                PictureId = newPicture.Id,
                DisplayOrder = 1
            });
        }
    }

    private async Task ImportProductTagsAsync(Product product, string tagNames)
    {
        var tags = tagNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tags.Length > 0)
            await productTagService.UpdateProductTagsAsync(product, tags);
    }

    private static string GetMimeTypeFromPath(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => MimeTypes.ImageJpeg,
            ".png" => MimeTypes.ImagePng,
            ".gif" => MimeTypes.ImageGif,
            ".bmp" => MimeTypes.ImageBmp,
            ".tiff" or ".tif" => MimeTypes.ImageTiff,
            _ => MimeTypes.ImageJpeg
        };
    }

    #endregion
}
