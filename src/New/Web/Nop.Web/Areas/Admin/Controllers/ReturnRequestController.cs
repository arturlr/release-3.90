using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ReturnRequestController(
    IReturnRequestService returnRequestService,
    IOrderService orderService,
    IProductService productService,
    ICustomerService customerService,
    IDateTimeHelper dateTimeHelper,
    IDownloadService downloadService,
    IWorkflowMessageService workflowMessageService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Utilities

    private async Task<ReturnRequestModel> PrepareReturnRequestModelAsync(ReturnRequest rr)
    {
        var model = new ReturnRequestModel
        {
            Id = rr.Id,
            CustomNumber = rr.CustomNumber ?? string.Empty,
            CustomerId = rr.CustomerId,
            Quantity = rr.Quantity,
            ReasonForReturn = rr.ReasonForReturn,
            RequestedAction = rr.RequestedAction,
            CustomerComments = rr.CustomerComments,
            StaffNotes = rr.StaffNotes,
            ReturnRequestStatusId = rr.ReturnRequestStatusId,
            ReturnRequestStatusStr = ((ReturnRequestStatus)rr.ReturnRequestStatusId).ToString(),
            CreatedOn = dateTimeHelper.ConvertToUserTime(rr.CreatedOnUtc, DateTimeKind.Utc),
        };

        var orderItem = await orderService.GetOrderItemByIdAsync(rr.OrderItemId);
        if (orderItem != null)
        {
            model.ProductId = orderItem.ProductId;
            model.OrderId = orderItem.OrderId;
            model.AttributeInfo = orderItem.AttributeDescription ?? string.Empty;

            var product = await productService.GetProductByIdAsync(orderItem.ProductId);
            model.ProductName = product?.Name ?? $"Product #{orderItem.ProductId}";

            var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
            model.CustomOrderNumber = order?.CustomOrderNumber ?? orderItem.OrderId.ToString();
        }

        var customer = await customerService.GetCustomerByIdAsync(rr.CustomerId);
        model.CustomerInfo = customer != null ? await GetCustomerDisplayAsync(customer) : "Guest";

        if (rr.UploadedFileId > 0)
        {
            var download = await downloadService.GetDownloadByIdAsync(rr.UploadedFileId);
            model.UploadedFileGuid = download?.DownloadGuid ?? Guid.Empty;
        }

        return model;
    }

    private async Task<string> GetCustomerDisplayAsync(Nop.Core.Domain.Customers.Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(
            Nop.Core.Domain.Customers.SystemCustomerRoleNames.Registered);
        return registeredRole != null && roleIds.Contains(registeredRole.Id)
            ? customer.Email ?? "Guest"
            : "Guest";
    }

    #endregion

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageReturnRequests"))
            return Forbid();

        return View(new ReturnRequestListModel());
    }

    [HttpPost]
    public async Task<IActionResult> ReturnRequestList(DataSourceRequest command, ReturnRequestListModel model)
    {
        if (!permissionService.Authorize("ManageReturnRequests"))
            return Forbid();

        var rrs = model.ReturnRequestStatusId == -1
            ? null
            : (ReturnRequestStatus?)model.ReturnRequestStatusId;

        var startDateUtc = model.StartDate.HasValue
            ? (DateTime?)dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, dateTimeHelper.CurrentTimeZone)
            : null;

        var endDateUtc = model.EndDate.HasValue
            ? (DateTime?)dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : null;

        var returnRequests = await returnRequestService.SearchReturnRequestsAsync(
            customNumber: model.CustomNumber,
            rs: rrs,
            createdFromUtc: startDateUtc,
            createdToUtc: endDateUtc,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var gridModels = new List<ReturnRequestGridModel>();
        foreach (var rr in returnRequests)
        {
            var gm = new ReturnRequestGridModel
            {
                Id = rr.Id,
                CustomNumber = rr.CustomNumber ?? string.Empty,
                CustomerId = rr.CustomerId,
                Quantity = rr.Quantity,
                ReturnRequestStatusStr = ((ReturnRequestStatus)rr.ReturnRequestStatusId).ToString(),
                CreatedOn = dateTimeHelper.ConvertToUserTime(rr.CreatedOnUtc, DateTimeKind.Utc),
            };

            var orderItem = await orderService.GetOrderItemByIdAsync(rr.OrderItemId);
            if (orderItem != null)
            {
                var product = await productService.GetProductByIdAsync(orderItem.ProductId);
                gm.ProductName = product?.Name ?? $"Product #{orderItem.ProductId}";
                var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
                gm.CustomOrderNumber = order?.CustomOrderNumber ?? orderItem.OrderId.ToString();
            }

            var customer = await customerService.GetCustomerByIdAsync(rr.CustomerId);
            gm.CustomerInfo = customer != null ? await GetCustomerDisplayAsync(customer) : "Guest";

            gridModels.Add(gm);
        }

        return Json(new DataSourceResult
        {
            Data = gridModels,
            Total = returnRequests.TotalCount,
        });
    }

    #endregion

    #region Edit

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageReturnRequests"))
            return Forbid();

        var rr = await returnRequestService.GetReturnRequestByIdAsync(id);
        if (rr is null)
            return RedirectToAction("List");

        return View(await PrepareReturnRequestModelAsync(rr));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ReturnRequestModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageReturnRequests"))
            return Forbid();

        var rr = await returnRequestService.GetReturnRequestByIdAsync(model.Id);
        if (rr is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            rr.Quantity = model.Quantity;
            rr.ReasonForReturn = model.ReasonForReturn;
            rr.RequestedAction = model.RequestedAction;
            rr.CustomerComments = model.CustomerComments;
            rr.StaffNotes = model.StaffNotes;
            rr.ReturnRequestStatusId = model.ReturnRequestStatusId;
            rr.UpdatedOnUtc = DateTime.UtcNow;
            await returnRequestService.UpdateReturnRequestAsync(rr);

            customerActivityService.InsertActivity("EditReturnRequest",
                "Edited return request (ID = {0})", rr.Id);

            return continueEditing
                ? RedirectToAction("Edit", new { id = rr.Id })
                : RedirectToAction("List");
        }

        // Redisplay form on validation failure — reload read-only fields
        var redisplay = await PrepareReturnRequestModelAsync(rr);
        redisplay.ReasonForReturn = model.ReasonForReturn;
        redisplay.RequestedAction = model.RequestedAction;
        redisplay.CustomerComments = model.CustomerComments;
        redisplay.StaffNotes = model.StaffNotes;
        redisplay.ReturnRequestStatusId = model.ReturnRequestStatusId;
        return View(redisplay);
    }

    [HttpPost]
    public async Task<IActionResult> NotifyCustomer(int id)
    {
        if (!permissionService.Authorize("ManageReturnRequests"))
            return Forbid();

        var rr = await returnRequestService.GetReturnRequestByIdAsync(id);
        if (rr is null)
            return RedirectToAction("List");

        var orderItem = await orderService.GetOrderItemByIdAsync(rr.OrderItemId);
        if (orderItem is null)
            return RedirectToAction("Edit", new { id = rr.Id });

        var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
        await workflowMessageService.SendReturnRequestStatusChangedCustomerNotificationAsync(
            rr, orderItem, order?.CustomerLanguageId ?? 0);

        return RedirectToAction("Edit", new { id = rr.Id });
    }

    #endregion

    #region Delete

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageReturnRequests"))
            return Forbid();

        var rr = await returnRequestService.GetReturnRequestByIdAsync(id);
        if (rr is null)
            return RedirectToAction("List");

        await returnRequestService.DeleteReturnRequestAsync(rr);

        customerActivityService.InsertActivity("DeleteReturnRequest",
            "Deleted return request (ID = {0})", rr.Id);

        return RedirectToAction("List");
    }

    #endregion
}
