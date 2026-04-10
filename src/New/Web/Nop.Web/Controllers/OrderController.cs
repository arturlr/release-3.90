using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Order;

namespace Nop.Web.Controllers;

public partial class OrderController(
    IOrderService orderService,
    IOrderProcessingService orderProcessingService,
    IShipmentService shipmentService,
    IPaymentService paymentService,
    IGiftCardService giftCardService,
    IWorkContext workContext,
    IStoreContext storeContext,
    ICustomerService customerService,
    IProductService productService,
    IPriceFormatter priceFormatter,
    IDateTimeHelper dateTimeHelper,
    IUrlRecordService urlRecordService,
    IRewardPointService rewardPointService,
    Nop.Services.Common.IPdfService? pdfService,
    OrderSettings orderSettings,
    RewardPointsSettings rewardPointsSettings,
    PdfSettings pdfSettings,
    CatalogSettings catalogSettings) : BasePublicController
{
    // --- My Account / Orders ---

    public async Task<IActionResult> CustomerOrders()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var model = await PrepareCustomerOrderListModelAsync();
        return View(model);
    }

    // --- Cancel Recurring Payment ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRecurringPayment(int recurringPaymentId)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var recurringPayment = await orderService.GetRecurringPaymentByIdAsync(recurringPaymentId);
        if (recurringPayment == null)
            return RedirectToAction(nameof(CustomerOrders));

        var initialOrder = await orderService.GetOrderByIdAsync(recurringPayment.InitialOrderId);
        if (!orderProcessingService.CanCancelRecurringPayment(workContext.CurrentCustomer, recurringPayment, initialOrder))
            return RedirectToAction(nameof(CustomerOrders));

        var errors = await orderProcessingService.CancelRecurringPaymentAsync(recurringPayment);
        var model = await PrepareCustomerOrderListModelAsync();
        model.RecurringPaymentErrors = errors.ToList();
        return View(nameof(CustomerOrders), model);
    }

    // --- Retry Last Recurring Payment ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryLastRecurringPayment(int recurringPaymentId)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var recurringPayment = await orderService.GetRecurringPaymentByIdAsync(recurringPaymentId);
        if (recurringPayment == null)
            return RedirectToAction(nameof(CustomerOrders));

        var initialOrder = await orderService.GetOrderByIdAsync(recurringPayment.InitialOrderId);
        if (!orderProcessingService.CanRetryLastRecurringPayment(workContext.CurrentCustomer, recurringPayment, initialOrder))
            return RedirectToAction(nameof(CustomerOrders));

        var errors = await orderProcessingService.ProcessNextRecurringPaymentAsync(recurringPayment);
        var model = await PrepareCustomerOrderListModelAsync();
        model.RecurringPaymentErrors = errors.ToList();
        return View(nameof(CustomerOrders), model);
    }

    // --- Reward Points ---

    public async Task<IActionResult> CustomerRewardPoints(int? page)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        if (!rewardPointsSettings.Enabled)
            return RedirectToAction("Index", "Home");

        var model = await PrepareCustomerRewardPointsModelAsync(page ?? 0);
        return View(model);
    }

    // --- Order Details ---

    public async Task<IActionResult> Details(int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        var model = await PrepareOrderDetailsModelAsync(order);
        return View(model);
    }

    // --- Print Order Details ---

    public async Task<IActionResult> PrintOrderDetails(int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        var model = await PrepareOrderDetailsModelAsync(order);
        model.PrintMode = true;
        return View(nameof(Details), model);
    }

    // --- PDF Invoice ---

    public async Task<IActionResult> GetPdfInvoice(int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        if (pdfService == null)
            return RedirectToAction(nameof(Details), new { orderId });

        using var stream = new MemoryStream();
        await pdfService.PrintOrdersToPdfAsync(stream, [order], workContext.WorkingLanguage.Id);
        return File(stream.ToArray(), MimeTypes.ApplicationPdf, $"order_{order.Id}.pdf");
    }

    // --- Re-Order ---

    public async Task<IActionResult> ReOrder(int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        await orderProcessingService.ReOrderAsync(order);
        return RedirectToAction("Cart", "ShoppingCart");
    }

    // --- Re-Post Payment ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RePostPayment(int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        if (!await paymentService.CanRePostProcessPaymentAsync(order))
            return RedirectToAction(nameof(Details), new { orderId });

        await paymentService.PostProcessPaymentAsync(new PostProcessPaymentRequest { Order = order });
        return RedirectToAction(nameof(Details), new { orderId });
    }

    // --- Shipment Details ---

    public async Task<IActionResult> ShipmentDetails(int shipmentId)
    {
        var shipment = await shipmentService.GetShipmentByIdAsync(shipmentId);
        if (shipment == null)
            return Challenge();

        var order = await orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        var model = await PrepareShipmentDetailsModelAsync(shipment, order);
        return View(model);
    }
}
