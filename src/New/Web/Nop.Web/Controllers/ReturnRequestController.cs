using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Order;

namespace Nop.Web.Controllers;

public partial class ReturnRequestController(
    IReturnRequestService returnRequestService,
    IOrderService orderService,
    IOrderProcessingService orderProcessingService,
    IProductService productService,
    IDownloadService downloadService,
    ICustomerService customerService,
    ICustomNumberFormatter customNumberFormatter,
    ILocalizationService localizationService,
    IWorkflowMessageService workflowMessageService,
    IWorkContext workContext,
    IStoreContext storeContext,
    IUrlRecordService urlRecordService,
    IPriceFormatter priceFormatter,
    OrderSettings orderSettings,
    LocalizationSettings localizationSettings) : BasePublicController
{
    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        return registeredRole != null && roleIds.Contains(registeredRole.Id);
    }

    public async Task<IActionResult> CustomerReturnRequests()
    {
        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        var requests = await returnRequestService.SearchReturnRequestsAsync(
            storeId: storeContext.CurrentStore.Id,
            customerId: customer.Id);

        var model = new CustomerReturnRequestsModel();
        foreach (var rr in requests)
        {
            var orderItem = await orderService.GetOrderItemByIdAsync(rr.OrderItemId);
            var product = orderItem != null ? await productService.GetProductByIdAsync(orderItem.ProductId) : null;

            var downloadGuid = Guid.Empty;
            if (rr.UploadedFileId > 0)
            {
                var download = await downloadService.GetDownloadByIdAsync(rr.UploadedFileId);
                if (download != null)
                    downloadGuid = download.DownloadGuid;
            }

            model.Items.Add(new CustomerReturnRequestsModel.ReturnRequestBriefModel
            {
                Id = rr.Id,
                CustomNumber = rr.CustomNumber,
                ReturnRequestStatus = rr.ReturnRequestStatus.ToString(),
                ProductId = product?.Id ?? 0,
                ProductName = product?.Name,
                ProductSeName = product != null ? await product.GetSeNameAsync(0, urlRecordService) : "",
                Quantity = rr.Quantity,
                ReturnReason = rr.ReasonForReturn,
                ReturnAction = rr.RequestedAction,
                Comments = rr.CustomerComments,
                UploadedFileGuid = downloadGuid,
                CreatedOn = rr.CreatedOnUtc
            });
        }

        return View(model);
    }

    public async Task<IActionResult> ReturnRequest(int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        if (!await orderProcessingService.IsReturnRequestAllowedAsync(order))
            return RedirectToAction("Index", "Home");

        var model = await PrepareSubmitReturnRequestModelAsync(order);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnRequestSubmit(int orderId, SubmitReturnRequestModel model)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return Challenge();

        if (!await orderProcessingService.IsReturnRequestAllowedAsync(order))
            return RedirectToAction("Index", "Home");

        var downloadId = 0;
        if (orderSettings.ReturnRequestsAllowFiles && model.UploadedFileGuid != Guid.Empty)
        {
            var download = await downloadService.GetDownloadByGuidAsync(model.UploadedFileGuid);
            if (download != null)
                downloadId = download.Id;
        }

        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        var count = 0;

        foreach (var orderItem in orderItems)
        {
            var product = await productService.GetProductByIdAsync(orderItem.ProductId);
            if (product != null && product.NotReturnable)
                continue;

            var quantityKey = $"quantity{orderItem.Id}";
            if (!Request.Form.TryGetValue(quantityKey, out var quantityValue) ||
                !int.TryParse(quantityValue, out var quantity) || quantity <= 0)
                continue;

            var reason = await returnRequestService.GetReturnRequestReasonByIdAsync(model.ReturnRequestReasonId);
            var action = await returnRequestService.GetReturnRequestActionByIdAsync(model.ReturnRequestActionId);

            var rr = new ReturnRequest
            {
                CustomNumber = "",
                StoreId = storeContext.CurrentStore.Id,
                OrderItemId = orderItem.Id,
                Quantity = quantity,
                CustomerId = workContext.CurrentCustomer.Id,
                ReasonForReturn = reason?.Name ?? "not available",
                RequestedAction = action?.Name ?? "not available",
                CustomerComments = model.Comments,
                UploadedFileId = downloadId,
                StaffNotes = string.Empty,
                ReturnRequestStatus = ReturnRequestStatus.Pending,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await returnRequestService.InsertReturnRequestAsync(rr);

            rr.CustomNumber = customNumberFormatter.GenerateReturnRequestCustomNumber(rr);
            await returnRequestService.UpdateReturnRequestAsync(rr);

            await workflowMessageService.SendNewReturnRequestStoreOwnerNotificationAsync(rr, orderItem, localizationSettings.DefaultAdminLanguageId);
            await workflowMessageService.SendNewReturnRequestCustomerNotificationAsync(rr, orderItem, order.CustomerLanguageId);

            count++;
        }

        var resultModel = await PrepareSubmitReturnRequestModelAsync(order);
        resultModel.Result = count > 0
            ? await localizationService.GetResourceAsync("ReturnRequests.Submitted")
            : await localizationService.GetResourceAsync("ReturnRequests.NoItemsSubmitted");

        return View("ReturnRequest", resultModel);
    }

    [HttpPost]
    public async Task<IActionResult> UploadFileReturnRequest()
    {
        if (!orderSettings.ReturnRequestsEnabled || !orderSettings.ReturnRequestsAllowFiles)
            return Json(new { success = false, downloadGuid = Guid.Empty });

        var httpPostedFile = Request.Form.Files.FirstOrDefault();
        if (httpPostedFile == null)
            return Json(new { success = false, message = "No file uploaded", downloadGuid = Guid.Empty });

        using var ms = new MemoryStream();
        await httpPostedFile.CopyToAsync(ms);
        var fileBinary = ms.ToArray();

        if (orderSettings.ReturnRequestsFileMaximumSize > 0 &&
            fileBinary.Length > orderSettings.ReturnRequestsFileMaximumSize * 1024)
        {
            return Json(new
            {
                success = false,
                message = string.Format(
                    await localizationService.GetResourceAsync("ShoppingCart.MaximumUploadedFileSize"),
                    orderSettings.ReturnRequestsFileMaximumSize),
                downloadGuid = Guid.Empty
            });
        }

        var download = new Download
        {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = false,
            DownloadBinary = fileBinary,
            ContentType = httpPostedFile.ContentType,
            Filename = Path.GetFileNameWithoutExtension(httpPostedFile.FileName),
            Extension = Path.GetExtension(httpPostedFile.FileName)?.ToLowerInvariant(),
            IsNew = true
        };
        await downloadService.InsertDownloadAsync(download);

        return Json(new
        {
            success = true,
            message = await localizationService.GetResourceAsync("ShoppingCart.FileUploaded"),
            downloadGuid = download.DownloadGuid
        });
    }

    private async Task<SubmitReturnRequestModel> PrepareSubmitReturnRequestModelAsync(Order order)
    {
        var model = new SubmitReturnRequestModel
        {
            OrderId = order.Id,
            CustomOrderNumber = order.CustomOrderNumber,
            AllowFiles = orderSettings.ReturnRequestsAllowFiles
        };

        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        foreach (var orderItem in orderItems)
        {
            var product = await productService.GetProductByIdAsync(orderItem.ProductId);
            if (product == null || product.NotReturnable)
                continue;

            model.Items.Add(new SubmitReturnRequestModel.OrderItemModel
            {
                Id = orderItem.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSeName = await product.GetSeNameAsync(0, urlRecordService),
                AttributeInfo = orderItem.AttributeDescription,
                UnitPrice = await priceFormatter.FormatPriceAsync(orderItem.UnitPriceInclTax),
                Quantity = orderItem.Quantity
            });
        }

        foreach (var reason in await returnRequestService.GetAllReturnRequestReasonsAsync())
            model.AvailableReturnReasons.Add(new SubmitReturnRequestModel.ReturnRequestReasonModel { Id = reason.Id, Name = reason.Name });

        foreach (var action in await returnRequestService.GetAllReturnRequestActionsAsync())
            model.AvailableReturnActions.Add(new SubmitReturnRequestModel.ReturnRequestActionModel { Id = action.Id, Name = action.Name });

        return model;
    }
}
