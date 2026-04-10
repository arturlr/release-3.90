using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers;

public class DownloadController(
    IDownloadService downloadService,
    IProductService productService,
    IOrderService orderService,
    IWorkContext workContext,
    ILocalizationService localizationService,
    CustomerSettings customerSettings) : BasePublicController
{
    public async Task<IActionResult> Sample(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound();

        if (!product.HasSampleDownload)
            return Content("Product doesn't have a sample download.");

        var download = await downloadService.GetDownloadByIdAsync(product.SampleDownloadId);
        if (download == null)
            return Content("Sample download is not available any more.");

        if (download.UseDownloadUrl)
            return Redirect(download.DownloadUrl!);

        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType) ? download.ContentType : MimeTypes.ApplicationOctetStream;
        return File(download.DownloadBinary, contentType, fileName + download.Extension);
    }

    public async Task<IActionResult> GetDownload(Guid orderItemId, bool agree = false)
    {
        var orderItem = await orderService.GetOrderItemByGuidAsync(orderItemId);
        if (orderItem == null)
            return NotFound();

        var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
        var product = await productService.GetProductByIdAsync(orderItem.ProductId);
        if (order == null || product == null || !IsDownloadAllowed(orderItem, order, product))
            return Content("Downloads are not allowed");

        if (customerSettings.DownloadableProductsValidateUser)
        {
            var customer = workContext.CurrentCustomer;
            if (customer == null)
                return Unauthorized();
            if (order.CustomerId != customer.Id)
                return Content("This is not your order");
        }

        var download = await downloadService.GetDownloadByIdAsync(product.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (product.HasUserAgreement && !agree)
            return RedirectToAction("UserAgreement", "Customer", new { orderItemId });

        if (!product.UnlimitedDownloads && orderItem.DownloadCount >= product.MaxNumberOfDownloads)
            return Content(string.Format(
                await localizationService.GetResourceAsync("DownloadableProducts.ReachedMaximumNumber"),
                product.MaxNumberOfDownloads));

        // Increment download count
        orderItem.DownloadCount++;
        await orderService.UpdateOrderItemAsync(orderItem);

        if (download.UseDownloadUrl)
            return Redirect(download.DownloadUrl!);

        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType) ? download.ContentType : MimeTypes.ApplicationOctetStream;
        return File(download.DownloadBinary, contentType, fileName + download.Extension);
    }

    public async Task<IActionResult> GetLicense(Guid orderItemId)
    {
        var orderItem = await orderService.GetOrderItemByGuidAsync(orderItemId);
        if (orderItem == null)
            return NotFound();

        var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
        var product = await productService.GetProductByIdAsync(orderItem.ProductId);
        if (order == null || product == null ||
            !IsDownloadAllowed(orderItem, order, product) ||
            !orderItem.LicenseDownloadId.HasValue || orderItem.LicenseDownloadId <= 0)
            return Content("Downloads are not allowed");

        if (customerSettings.DownloadableProductsValidateUser)
        {
            var customer = workContext.CurrentCustomer;
            if (customer == null || order.CustomerId != customer.Id)
                return Unauthorized();
        }

        var download = await downloadService.GetDownloadByIdAsync(orderItem.LicenseDownloadId.Value);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return Redirect(download.DownloadUrl!);

        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType) ? download.ContentType : MimeTypes.ApplicationOctetStream;
        return File(download.DownloadBinary, contentType, fileName + download.Extension);
    }

    public async Task<IActionResult> GetFileUpload(Guid downloadId)
    {
        var download = await downloadService.GetDownloadByGuidAsync(downloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return Redirect(download.DownloadUrl!);

        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : downloadId.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType) ? download.ContentType : MimeTypes.ApplicationOctetStream;
        return File(download.DownloadBinary, contentType, fileName + download.Extension);
    }

    public async Task<IActionResult> GetOrderNoteFile(int orderNoteId)
    {
        var orderNote = await orderService.GetOrderNoteByIdAsync(orderNoteId);
        if (orderNote == null)
            return NotFound();

        var order = await orderService.GetOrderByIdAsync(orderNote.OrderId);
        if (order == null)
            return NotFound();

        var customer = workContext.CurrentCustomer;
        if (customer == null || order.CustomerId != customer.Id)
            return Unauthorized();

        var download = await downloadService.GetDownloadByIdAsync(orderNote.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return Redirect(download.DownloadUrl!);

        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : orderNote.Id.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType) ? download.ContentType : MimeTypes.ApplicationOctetStream;
        return File(download.DownloadBinary, contentType, fileName + download.Extension);
    }

    /// <summary>
    /// Checks whether download is allowed for the given order item.
    /// Implements the logic deferred from IDownloadService in [3.7].
    /// </summary>
    private static bool IsDownloadAllowed(OrderItem orderItem, Order order, Product product)
    {
        if (order.Deleted || (OrderStatus)order.OrderStatusId == OrderStatus.Cancelled)
            return false;

        if (!product.IsDownload)
            return false;

        return (DownloadActivationType)product.DownloadActivationTypeId switch
        {
            DownloadActivationType.WhenOrderIsPaid =>
                (PaymentStatus)order.PaymentStatusId == PaymentStatus.Paid
                && order.PaidDateUtc.HasValue
                && (!product.DownloadExpirationDays.HasValue
                    || order.PaidDateUtc.Value.AddDays(product.DownloadExpirationDays.Value) > DateTime.UtcNow),

            DownloadActivationType.Manually =>
                orderItem.IsDownloadActivated
                && (!product.DownloadExpirationDays.HasValue
                    || order.CreatedOnUtc.AddDays(product.DownloadExpirationDays.Value) > DateTime.UtcNow),

            _ => false
        };
    }
}
