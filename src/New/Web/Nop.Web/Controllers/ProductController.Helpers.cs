using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Controllers;

public partial class ProductController
{
    private static bool IsAvailable(Product product)
    {
        if (product.AvailableStartDateTimeUtc.HasValue && product.AvailableStartDateTimeUtc.Value > DateTime.UtcNow)
            return false;
        if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow)
            return false;
        return true;
    }

    private async Task<bool> IsGuestAsync(Customer customer)
    {
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole == null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(guestRole.Id);
    }

    private async Task<ProductDetailsModel> PrepareProductDetailsModelAsync(Product product)
    {
        var seName = await product.GetSeNameAsync(0, urlRecordService);

        // Pictures
        var pictures = await pictureService.GetPicturesByProductIdAsync(product.Id);
        var pictureModels = new List<ProductDetailsModel.PictureModel>();
        foreach (var pic in pictures)
        {
            pictureModels.Add(new ProductDetailsModel.PictureModel
            {
                ImageUrl = await pictureService.GetPictureUrlAsync(pic, mediaSettings.ProductDetailsPictureSize),
                FullSizeImageUrl = await pictureService.GetPictureUrlAsync(pic, 0),
                Title = product.Name,
                AlternateText = product.Name,
            });
        }

        if (pictureModels.Count == 0)
        {
            pictureModels.Add(new ProductDetailsModel.PictureModel
            {
                ImageUrl = await pictureService.GetDefaultPictureUrlAsync(mediaSettings.ProductDetailsPictureSize),
                FullSizeImageUrl = await pictureService.GetDefaultPictureUrlAsync(0),
                Title = product.Name,
                AlternateText = product.Name,
            });
        }

        // Price
        var finalPrice = await priceCalculationService.GetFinalPriceAsync(product, workContext.CurrentCustomer);
        var priceStr = await priceFormatter.FormatPriceAsync(finalPrice);
        string? oldPriceStr = null;
        if (product.OldPrice > 0 && product.OldPrice > finalPrice)
            oldPriceStr = await priceFormatter.FormatPriceAsync(product.OldPrice);

        // Stock availability
        var stockMessage = product.ManageInventoryMethodId switch
        {
            (int)ManageInventoryMethod.ManageStock when product.StockQuantity > 0 => "In Stock",
            (int)ManageInventoryMethod.ManageStock when product.BackorderModeId == (int)BackorderMode.NoBackorders => "Out of Stock",
            _ => null,
        };

        // Template
        var template = await productTemplateService.GetProductTemplateByIdAsync(product.ProductTemplateId);
        var templateViewPath = template?.ViewPath ?? "ProductDetails";

        // Edit link
        var showEditLink = permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel)
            && permissionService.Authorize(StandardPermissionProvider.ManageProducts);

        return new ProductDetailsModel
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            MetaKeywords = product.MetaKeywords,
            MetaDescription = product.MetaDescription,
            MetaTitle = product.MetaTitle,
            SeName = seName,
            ShowSku = catalogSettings.ShowSkuOnProductDetailsPage,
            Sku = product.Sku,
            ManufacturerPartNumber = product.ManufacturerPartNumber,
            Gtin = product.Gtin,
            FreeShipping = product.IsFreeShipping,
            StockAvailability = stockMessage,
            AllowCustomerReviews = product.AllowCustomerReviews,
            ProductPrice = priceStr,
            OldPrice = oldPriceStr,
            ImageUrl = pictureModels[0].ImageUrl,
            Pictures = pictureModels,
            ProductTemplateViewPath = templateViewPath,
            DisplayEditLink = showEditLink,
            EditLink = showEditLink ? Url.Action("Edit", "Product", new { id = product.Id, Area = "Admin" }) : null,
        };
    }

    private async Task<ProductReviewsModel> PrepareProductReviewsModelAsync(Product product)
    {
        var seName = await product.GetSeNameAsync(0, urlRecordService);
        var reviews = await productService.GetAllProductReviewsAsync(
            customerId: 0, approved: true, productId: product.Id);

        var model = new ProductReviewsModel
        {
            Id = product.Id,
            ProductName = product.Name,
            ProductSeName = seName,
        };

        foreach (var review in reviews)
        {
            model.Items.Add(new ProductReviewModel
            {
                Id = review.Id,
                CustomerId = review.CustomerId,
                Title = review.Title,
                ReviewText = review.ReviewText,
                Rating = review.Rating,
                HelpfulYesTotal = review.HelpfulYesTotal,
                HelpfulNoTotal = review.HelpfulNoTotal,
                WrittenOnStr = review.CreatedOnUtc,
            });
        }

        return model;
    }

    private async Task<ProductOverviewModel> PrepareProductOverviewModelAsync(Product product)
    {
        var seName = await product.GetSeNameAsync(0, urlRecordService);

        var pictures = await pictureService.GetPicturesByProductIdAsync(product.Id, 1);
        var imageUrl = pictures.Count > 0
            ? await pictureService.GetPictureUrlAsync(pictures[0], mediaSettings.ProductThumbPictureSize)
            : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.ProductThumbPictureSize);

        var finalPrice = await priceCalculationService.GetFinalPriceAsync(product, workContext.CurrentCustomer);
        var priceStr = await priceFormatter.FormatPriceAsync(finalPrice);
        string? oldPriceStr = null;
        if (product.OldPrice > 0 && product.OldPrice > finalPrice)
            oldPriceStr = await priceFormatter.FormatPriceAsync(product.OldPrice);

        return new ProductOverviewModel
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            SeName = seName,
            ImageUrl = imageUrl,
            Price = priceStr,
            OldPrice = oldPriceStr,
            MarkAsNew = product.MarkAsNew
                && (!product.MarkAsNewStartDateTimeUtc.HasValue || product.MarkAsNewStartDateTimeUtc.Value <= DateTime.UtcNow)
                && (!product.MarkAsNewEndDateTimeUtc.HasValue || product.MarkAsNewEndDateTimeUtc.Value >= DateTime.UtcNow),
        };
    }
}
