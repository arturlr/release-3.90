using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Security;
using Nop.Services.Seo;

namespace Nop.Web.Controllers;

public partial class ShoppingCartController
{
    [HttpPost]
    public async Task<IActionResult> AddProductToCart_Catalog(int productId, int shoppingCartTypeId, int quantity, bool forceredirection = false)
    {
        var cartType = (ShoppingCartType)shoppingCartTypeId;
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null)
            return Json(new { success = false, message = "No product found with the specified ID" });

        if (product.ProductType != ProductType.SimpleProduct || product.OrderMinimumQuantity > quantity ||
            product.CustomerEntersPrice || product.IsRental)
            return Json(new { redirect = Url.Action("ProductDetails", "Product", new { productId }) });

        if ((product.AllowedQuantities ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Length > 0)
            return Json(new { redirect = Url.Action("ProductDetails", "Product", new { productId }) });

        var productAttributes = await productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
        if (productAttributes.Any(pam => pam.AttributeControlTypeId != (int)AttributeControlType.ReadonlyCheckboxes))
            return Json(new { redirect = Url.Action("ProductDetails", "Product", new { productId }) });

        var attXml = "";
        foreach (var attribute in productAttributes)
        {
            var values = await productAttributeService.GetProductAttributeValuesAsync(attribute.Id);
            foreach (var v in values.Where(v => v.IsPreSelected))
                attXml = productAttributeParser.AddProductAttribute(attXml, attribute, v.Id.ToString());
        }

        var customer = workContext.CurrentCustomer;
        var storeId = storeContext.CurrentStore.Id;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, cartType, storeId);
        var existingItem = await shoppingCartService.FindShoppingCartItemInTheCartAsync(cart, cartType, product);
        var quantityToValidate = existingItem != null ? existingItem.Quantity + quantity : quantity;

        var preWarnings = await shoppingCartService.GetShoppingCartItemWarningsAsync(customer, cartType,
            product, storeId, "", 0m, null, null, quantityToValidate,
            automaticallyAddRequiredProductsIfEnabled: false, getStandardWarnings: true,
            getAttributesWarnings: false, getGiftCardWarnings: false,
            getRequiredProductWarnings: false, getRentalWarnings: false);
        if (preWarnings.Any())
            return Json(new { success = false, message = preWarnings.ToArray() });

        var addWarnings = await shoppingCartService.AddToCartAsync(customer, product, cartType, storeId, attXml, quantity: quantity);
        if (addWarnings.Any())
            return Json(new { redirect = Url.Action("ProductDetails", "Product", new { productId }) });

        return await BuildAddToCartSuccessResponseAsync(cartType, product, customer, storeId, forceredirection);
    }

    [HttpPost]
    public async Task<IActionResult> AddProductToCart_Details(int productId, int shoppingCartTypeId, IFormCollection form)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null)
            return Json(new { redirect = Url.Action("Index", "Home") });

        var cartType = (ShoppingCartType)shoppingCartTypeId;
        var customer = workContext.CurrentCustomer;
        var storeId = storeContext.CurrentStore.Id;

        var (attributesXml, errors) = await ParseProductAttributesAsync(product, form);

        var customerEnteredPrice = 0m;
        if (product.CustomerEntersPrice)
        {
            var key = $"addtocart_{productId}.CustomerEnteredPrice";
            if (form.ContainsKey(key) && decimal.TryParse(form[key], out var enteredPrice))
                customerEnteredPrice = currencyService.ConvertToPrimaryStoreCurrency(enteredPrice, workContext.WorkingCurrency);
        }

        var qty = 1;
        var qtyKey = $"addtocart_{productId}.EnteredQuantity";
        if (form.ContainsKey(qtyKey) && int.TryParse(form[qtyKey], out var enteredQty))
            qty = enteredQty;

        ParseRentalDates(product, form, out var rentalStartDate, out var rentalEndDate);

        var addWarnings = await shoppingCartService.AddToCartAsync(customer, product, cartType, storeId,
            attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, qty);
        if (errors.Count != 0)
            addWarnings = [.. addWarnings, .. errors];

        if (addWarnings.Any())
            return Json(new { success = false, message = addWarnings.ToArray() });

        return await BuildAddToCartSuccessResponseAsync(cartType, product, customer, storeId, false);
    }

    [HttpPost]
    public async Task<IActionResult> ProductDetails_AttributeChange(int productId, IFormCollection form)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null)
            return Json(new { });

        var (attributesXml, _) = await ParseProductAttributesAsync(product, form);

        var price = "";
        if (permissionService.Authorize(StandardPermissionProvider.DisplayPrices) && !product.CustomerEntersPrice)
        {
            var additionalCharge = 0m;
            var parsedValues = await productAttributeParser.ParseProductAttributeValuesAsync(attributesXml);
            foreach (var value in parsedValues)
                additionalCharge += await priceCalculationService.GetProductAttributeValuePriceAdjustmentAsync(value);

            var finalPrice = await priceCalculationService.GetFinalPriceAsync(product, workContext.CurrentCustomer, additionalCharge);
            var (taxPrice, _) = await taxService.GetProductPriceAsync(product, finalPrice);
            price = await priceFormatter.FormatPriceAsync(taxPrice);
        }

        var sku = product.Sku;
        var combination = await productAttributeParser.FindProductAttributeCombinationAsync(product, attributesXml);
        if (combination != null && !string.IsNullOrEmpty(combination.Sku))
            sku = combination.Sku;

        return Json(new { price, sku });
    }

    [HttpPost]
    public async Task<IActionResult> CheckoutAttributeChange(IFormCollection form)
    {
        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        var checkoutAttributesXml = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, storeContext.CurrentStore.Id);
        var attributeInfo = await checkoutAttributeFormatter.FormatAttributesAsync(checkoutAttributesXml ?? "", customer);

        return Json(new { checkoutattributeinfo = attributeInfo });
    }

    [HttpPost]
    public async Task<IActionResult> UploadFileProductAttribute(int attributeId)
    {
        var attribute = await productAttributeService.GetProductAttributeMappingByIdAsync(attributeId);
        if (attribute == null || attribute.AttributeControlTypeId != (int)AttributeControlType.FileUpload)
            return Json(new { success = false, downloadGuid = Guid.Empty });

        var httpPostedFile = Request.Form.Files.FirstOrDefault();
        if (httpPostedFile == null)
            return Json(new { success = false, message = "No file uploaded", downloadGuid = Guid.Empty });

        var fileBinary = await GetFileBytesAsync(httpPostedFile);
        var fileExtension = Path.GetExtension(httpPostedFile.FileName);

        if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
        {
            var allowed = attribute.ValidationFileAllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, message = await localizationService.GetResourceAsync("ShoppingCart.ValidationFileAllowed"), downloadGuid = Guid.Empty });
        }

        if (attribute.ValidationFileMaximumSize.HasValue && fileBinary.Length > attribute.ValidationFileMaximumSize.Value * 1024)
            return Json(new { success = false, message = string.Format(await localizationService.GetResourceAsync("ShoppingCart.MaximumUploadedFileSize"), attribute.ValidationFileMaximumSize.Value), downloadGuid = Guid.Empty });

        var download = new Nop.Core.Domain.Media.Download
        {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = false,
            DownloadBinary = fileBinary,
            ContentType = httpPostedFile.ContentType,
            Filename = Path.GetFileNameWithoutExtension(httpPostedFile.FileName),
            Extension = fileExtension,
            IsNew = true
        };
        await downloadService.InsertDownloadAsync(download);
        return Json(new { success = true, message = await localizationService.GetResourceAsync("ShoppingCart.FileUploaded"), downloadGuid = download.DownloadGuid });
    }

    [HttpPost]
    public async Task<IActionResult> UploadFileCheckoutAttribute(int attributeId)
    {
        var attribute = await checkoutAttributeService.GetCheckoutAttributeByIdAsync(attributeId);
        if (attribute == null || attribute.AttributeControlTypeId != (int)AttributeControlType.FileUpload)
            return Json(new { success = false, downloadGuid = Guid.Empty });

        var httpPostedFile = Request.Form.Files.FirstOrDefault();
        if (httpPostedFile == null)
            return Json(new { success = false, message = "No file uploaded", downloadGuid = Guid.Empty });

        var fileBinary = await GetFileBytesAsync(httpPostedFile);
        var fileExtension = Path.GetExtension(httpPostedFile.FileName);

        if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
        {
            var allowed = attribute.ValidationFileAllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(x => x.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                return Json(new { success = false, message = await localizationService.GetResourceAsync("ShoppingCart.ValidationFileAllowed"), downloadGuid = Guid.Empty });
        }

        if (attribute.ValidationFileMaximumSize.HasValue && fileBinary.Length > attribute.ValidationFileMaximumSize.Value * 1024)
            return Json(new { success = false, message = string.Format(await localizationService.GetResourceAsync("ShoppingCart.MaximumUploadedFileSize"), attribute.ValidationFileMaximumSize.Value), downloadGuid = Guid.Empty });

        var download = new Nop.Core.Domain.Media.Download
        {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = false,
            DownloadBinary = fileBinary,
            ContentType = httpPostedFile.ContentType,
            Filename = Path.GetFileNameWithoutExtension(httpPostedFile.FileName),
            Extension = fileExtension,
            IsNew = true
        };
        await downloadService.InsertDownloadAsync(download);
        return Json(new { success = true, message = await localizationService.GetResourceAsync("ShoppingCart.FileUploaded"), downloadGuid = download.DownloadGuid });
    }

    private async Task<IActionResult> BuildAddToCartSuccessResponseAsync(ShoppingCartType cartType, Product product, Customer customer, int storeId, bool forceredirection)
    {
        if (cartType == ShoppingCartType.Wishlist)
        {
            customerActivityService.InsertActivity("PublicStore.AddToWishlist",
                await localizationService.GetResourceAsync("ActivityLog.PublicStore.AddToWishlist"), product.Name ?? "");
            if (shoppingCartSettings.DisplayWishlistAfterAddingProduct || forceredirection)
                return Json(new { redirect = Url.Action("Wishlist") });
            var wishlistCount = (await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, storeId)).Sum(x => x.Quantity);
            return Json(new
            {
                success = true,
                message = string.Format(await localizationService.GetResourceAsync("Products.ProductHasBeenAddedToTheWishlist.Link"), Url.Action("Wishlist")),
                updatetopwishlistsectionhtml = string.Format(await localizationService.GetResourceAsync("Wishlist.HeaderQuantity"), wishlistCount)
            });
        }

        customerActivityService.InsertActivity("PublicStore.AddToShoppingCart",
            await localizationService.GetResourceAsync("ActivityLog.PublicStore.AddToShoppingCart"), product.Name ?? "");
        if (shoppingCartSettings.DisplayCartAfterAddingProduct || forceredirection)
            return Json(new { redirect = Url.Action("Cart") });
        var cartCount = (await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId)).Sum(x => x.Quantity);
        return Json(new
        {
            success = true,
            message = string.Format(await localizationService.GetResourceAsync("Products.ProductHasBeenAddedToTheCart.Link"), Url.Action("Cart")),
            updatetopcartsectionhtml = string.Format(await localizationService.GetResourceAsync("ShoppingCart.HeaderQuantity"), cartCount)
        });
    }

    private static async Task<byte[]> GetFileBytesAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
