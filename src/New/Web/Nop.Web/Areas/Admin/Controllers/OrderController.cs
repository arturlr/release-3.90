using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class OrderController(
    IOrderService orderService,
    IOrderProcessingService orderProcessingService,
    IProductService productService,
    ICustomerService customerService,
    IAddressService addressService,
    ICountryService countryService,
    IStoreService storeService,
    IVendorService vendorService,
    IShipmentService shipmentService,
    IExportManager exportManager,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IWorkContext workContext) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var model = new OrderListModel
        {
            IsLoggedInAsVendor = workContext.CurrentVendor is not null
        };

        // Order statuses
        foreach (var os in Enum.GetValues<OrderStatus>())
            model.AvailableOrderStatuses.Add(new SelectListItem { Text = os.ToString(), Value = ((int)os).ToString() });

        // Payment statuses
        foreach (var ps in Enum.GetValues<PaymentStatus>())
            model.AvailablePaymentStatuses.Add(new SelectListItem { Text = ps.ToString(), Value = ((int)ps).ToString() });

        // Shipping statuses
        foreach (var ss in Enum.GetValues<ShippingStatus>())
            model.AvailableShippingStatuses.Add(new SelectListItem { Text = ss.ToString(), Value = ((int)ss).ToString() });

        // Stores
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        // Vendors
        model.AvailableVendors.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var v in await vendorService.GetAllVendorsAsync())
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

        // Countries
        model.AvailableCountries.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var c in await countryService.GetAllCountriesAsync(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> OrderList(DataSourceRequest command, OrderListModel model)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var vendorId = workContext.CurrentVendor?.Id ?? model.VendorId;

        var osIds = ParseStatusIds(model.OrderStatusIds);
        var psIds = ParseStatusIds(model.PaymentStatusIds);
        var ssIds = ParseStatusIds(model.ShippingStatusIds);

        var orders = await orderService.SearchOrdersAsync(
            storeId: model.StoreId,
            vendorId: vendorId,
            billingCountryId: model.BillingCountryId,
            paymentMethodSystemName: model.PaymentMethodSystemName,
            createdFromUtc: model.StartDate,
            createdToUtc: model.EndDate?.AddDays(1),
            osIds: osIds,
            psIds: psIds,
            ssIds: ssIds,
            billingEmail: model.BillingEmail,
            billingLastName: model.BillingLastName,
            orderNotes: model.OrderNotes,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var gridModels = new List<OrderGridModel>();
        foreach (var o in orders)
        {
            var customer = await customerService.GetCustomerByIdAsync(o.CustomerId);
            var store = await storeService.GetStoreByIdAsync(o.StoreId);
            gridModels.Add(new OrderGridModel
            {
                Id = o.Id,
                CustomOrderNumber = o.CustomOrderNumber,
                OrderStatus = o.OrderStatus.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                ShippingStatus = o.ShippingStatus.ToString(),
                CustomerEmail = customer?.Email,
                OrderTotal = o.OrderTotal,
                StoreName = store?.Name,
                CreatedOn = o.CreatedOnUtc
            });
        }

        return Json(new DataSourceResult { Data = gridModels, Total = orders.TotalCount });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null || order.Deleted)
            return RedirectToAction("List");

        if (workContext.CurrentVendor is not null)
            return RedirectToAction("List");

        var model = await PrepareOrderModelAsync(order);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        await orderProcessingService.DeleteOrderAsync(order);

        customerActivityService.InsertActivity("DeleteOrder", $"Deleted order (#{order.CustomOrderNumber})");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> GoToOrderNumber(OrderListModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.GoDirectlyToNumber))
        {
            var order = await orderService.GetOrderByCustomOrderNumberAsync(model.GoDirectlyToNumber.Trim());
            if (order is not null)
                return RedirectToAction("Edit", new { id = order.Id });

            // Try parsing as int (order ID)
            if (int.TryParse(model.GoDirectlyToNumber.Trim(), out var orderId))
            {
                order = await orderService.GetOrderByIdAsync(orderId);
                if (order is not null)
                    return RedirectToAction("Edit", new { id = order.Id });
            }
        }

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(OrderListModel model)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var vendorId = workContext.CurrentVendor?.Id ?? model.VendorId;
        var osIds = ParseStatusIds(model.OrderStatusIds);
        var psIds = ParseStatusIds(model.PaymentStatusIds);
        var ssIds = ParseStatusIds(model.ShippingStatusIds);

        var orders = await orderService.SearchOrdersAsync(
            storeId: model.StoreId,
            vendorId: vendorId,
            billingCountryId: model.BillingCountryId,
            paymentMethodSystemName: model.PaymentMethodSystemName,
            createdFromUtc: model.StartDate,
            createdToUtc: model.EndDate?.AddDays(1),
            osIds: osIds,
            psIds: psIds,
            ssIds: ssIds,
            billingEmail: model.BillingEmail,
            billingLastName: model.BillingLastName,
            orderNotes: model.OrderNotes);

        var bytes = await exportManager.ExportOrdersToXlsxAsync(orders);
        return File(bytes, MimeTypes.TextXlsx, "orders.xlsx");
    }

    // --- Payment operations ---

    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanCancelOrder(order))
            await orderProcessingService.CancelOrderAsync(order, true);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> CaptureOrder(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanCapture(order))
            await orderProcessingService.CaptureAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> MarkOrderAsPaid(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanMarkOrderAsPaid(order))
            await orderProcessingService.MarkOrderAsPaidAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RefundOrder(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanRefund(order))
            await orderProcessingService.RefundAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RefundOrderOffline(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanRefundOffline(order))
            await orderProcessingService.RefundOfflineAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> VoidOrder(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanVoid(order))
            await orderProcessingService.VoidAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> VoidOrderOffline(int id)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        if (orderProcessingService.CanVoidOffline(order))
            await orderProcessingService.VoidOfflineAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeOrderStatus(int id, int orderStatusId)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Forbid();

        var order = await orderService.GetOrderByIdAsync(id);
        if (order is null)
            return RedirectToAction("List");

        order.OrderStatusId = orderStatusId;
        await orderService.UpdateOrderAsync(order);
        await orderProcessingService.CheckOrderStatusAsync(order);

        return RedirectToAction("Edit", new { id });
    }

    // --- Order notes ---

    [HttpPost]
    public async Task<JsonResult> OrderNotesSelect(int orderId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var notes = await orderService.GetOrderNotesByOrderIdAsync(orderId);
        var gridModel = new DataSourceResult
        {
            Data = notes.OrderByDescending(n => n.CreatedOnUtc)
                .Skip((command.Page - 1) * command.PageSize)
                .Take(command.PageSize)
                .Select(n => new OrderNoteModel
                {
                    Id = n.Id,
                    OrderId = orderId,
                    Note = n.Note,
                    DisplayToCustomer = n.DisplayToCustomer,
                    CreatedOn = n.CreatedOnUtc
                }),
            Total = notes.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> OrderNoteAdd(int orderId, bool displayToCustomer, string message)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Json(new { Result = false });

        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order is null)
            return Json(new { Result = false });

        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = orderId,
            Note = message,
            DisplayToCustomer = displayToCustomer,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<JsonResult> OrderNoteDelete(int id, int orderId)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Json(new { Result = false });

        var note = await orderService.GetOrderNoteByIdAsync(id);
        if (note is not null)
            await orderService.DeleteOrderNoteAsync(note);

        return Json(new { Result = true });
    }

    // --- Shipments grid (for order detail page) ---

    [HttpPost]
    public async Task<JsonResult> ShipmentsByOrder(int orderId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageOrders"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var shipments = await shipmentService.GetShipmentsByOrderIdAsync(orderId);

        var gridModel = new DataSourceResult
        {
            Data = shipments
                .Skip((command.Page - 1) * command.PageSize)
                .Take(command.PageSize)
                .Select(s => new ShipmentGridModel
                {
                    Id = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    ShippedDate = s.ShippedDateUtc,
                    DeliveryDate = s.DeliveryDateUtc
                }),
            Total = shipments.Count
        };

        return Json(gridModel);
    }
}
