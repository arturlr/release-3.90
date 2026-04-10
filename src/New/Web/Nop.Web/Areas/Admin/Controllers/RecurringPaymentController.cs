using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class RecurringPaymentController(
    IOrderService orderService,
    IOrderProcessingService orderProcessingService,
    ICustomerService customerService,
    IDateTimeHelper dateTimeHelper,
    IPaymentService paymentService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IWorkContext workContext) : BaseAdminController
{
    #region Utilities

    private async Task PrepareRecurringPaymentModelAsync(RecurringPaymentModel model, RecurringPayment rp)
    {
        var initialOrder = await orderService.GetOrderByIdAsync(rp.InitialOrderId);
        var history = await orderService.GetRecurringPaymentHistoryAsync(rp);

        model.Id = rp.Id;
        model.CycleLength = rp.CycleLength;
        model.CyclePeriodId = rp.CyclePeriodId;
        model.CyclePeriodStr = ((RecurringProductCyclePeriod)rp.CyclePeriodId).ToString();
        model.TotalCycles = rp.TotalCycles;
        model.StartDate = dateTimeHelper.ConvertToUserTime(rp.StartDateUtc, DateTimeKind.Utc).ToString("g");
        model.IsActive = rp.IsActive;
        model.CyclesRemaining = Math.Max(0, rp.TotalCycles - history.Count);
        model.LastPaymentFailed = rp.LastPaymentFailed;

        var nextPayment = ComputeNextPaymentDate(rp, history);
        model.NextPaymentDate = nextPayment.HasValue
            ? dateTimeHelper.ConvertToUserTime(nextPayment.Value, DateTimeKind.Utc).ToString("g")
            : string.Empty;

        if (initialOrder != null)
        {
            model.InitialOrderId = initialOrder.Id;
            var customer = await customerService.GetCustomerByIdAsync(initialOrder.CustomerId);
            if (customer != null)
            {
                model.CustomerId = customer.Id;
                var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
                var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(
                    Nop.Core.Domain.Customers.SystemCustomerRoleNames.Registered);
                model.CustomerEmail = registeredRole != null && roleIds.Contains(registeredRole.Id)
                    ? customer.Email ?? string.Empty
                    : "Guest";
            }

            model.PaymentType = (await paymentService.GetRecurringPaymentTypeAsync(
                initialOrder.PaymentMethodSystemName ?? string.Empty)).ToString();
            model.CanCancelRecurringPayment = orderProcessingService.CanCancelRecurringPayment(
                workContext.CurrentCustomer, rp, initialOrder);
        }
    }

    private static DateTime? ComputeNextPaymentDate(RecurringPayment rp, IList<RecurringPaymentHistory> history)
    {
        if (!rp.IsActive)
            return null;

        if (history.Count >= rp.TotalCycles)
            return null;

        var lastPayment = history.OrderByDescending(h => h.CreatedOnUtc).FirstOrDefault();
        if (lastPayment == null)
            return rp.StartDateUtc;

        return rp.CyclePeriod switch
        {
            RecurringProductCyclePeriod.Days => (DateTime?)lastPayment.CreatedOnUtc.AddDays(rp.CycleLength),
            RecurringProductCyclePeriod.Weeks => lastPayment.CreatedOnUtc.AddDays(7 * rp.CycleLength),
            RecurringProductCyclePeriod.Months => lastPayment.CreatedOnUtc.AddMonths(rp.CycleLength),
            RecurringProductCyclePeriod.Years => lastPayment.CreatedOnUtc.AddYears(rp.CycleLength),
            _ => null,
        };
    }

    #endregion

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> RecurringPaymentList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payments = await orderService.SearchRecurringPaymentsAsync(
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true);

        var models = new List<RecurringPaymentModel>();
        foreach (var rp in payments)
        {
            var m = new RecurringPaymentModel();
            await PrepareRecurringPaymentModelAsync(m, rp);
            models.Add(m);
        }

        return Json(new DataSourceResult
        {
            Data = models,
            Total = payments.TotalCount,
        });
    }

    #endregion

    #region Edit

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payment = await orderService.GetRecurringPaymentByIdAsync(id);
        if (payment is null || payment.Deleted)
            return RedirectToAction("List");

        var model = new RecurringPaymentModel();
        await PrepareRecurringPaymentModelAsync(model, payment);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(RecurringPaymentModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payment = await orderService.GetRecurringPaymentByIdAsync(model.Id);
        if (payment is null || payment.Deleted)
            return RedirectToAction("List");

        payment.CycleLength = model.CycleLength;
        payment.CyclePeriodId = model.CyclePeriodId;
        payment.TotalCycles = model.TotalCycles;
        payment.IsActive = model.IsActive;
        await orderService.UpdateRecurringPaymentAsync(payment);

        customerActivityService.InsertActivity("EditRecurringPayment", "Edited recurring payment (ID = {0})", payment.Id);

        if (continueEditing)
            return RedirectToAction("Edit", new { id = payment.Id });

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payment = await orderService.GetRecurringPaymentByIdAsync(id);
        if (payment is null)
            return RedirectToAction("List");

        await orderService.DeleteRecurringPaymentAsync(payment);

        customerActivityService.InsertActivity("DeleteRecurringPayment", "Deleted recurring payment (ID = {0})", payment.Id);

        return RedirectToAction("List");
    }

    #endregion

    #region History

    [HttpPost]
    public async Task<IActionResult> HistoryList(int recurringPaymentId)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payment = await orderService.GetRecurringPaymentByIdAsync(recurringPaymentId);
        if (payment is null)
            return Json(new DataSourceResult { Data = Array.Empty<RecurringPaymentHistoryModel>(), Total = 0 });

        var history = await orderService.GetRecurringPaymentHistoryAsync(payment);
        var models = new List<RecurringPaymentHistoryModel>();
        foreach (var h in history.OrderBy(x => x.CreatedOnUtc))
        {
            var order = await orderService.GetOrderByIdAsync(h.OrderId);
            models.Add(new RecurringPaymentHistoryModel
            {
                Id = h.Id,
                OrderId = h.OrderId,
                CustomOrderNumber = order?.CustomOrderNumber ?? h.OrderId.ToString(),
                RecurringPaymentId = h.RecurringPaymentId,
                OrderStatus = order != null ? ((OrderStatus)order.OrderStatusId).ToString() : string.Empty,
                PaymentStatus = order != null ? ((PaymentStatus)order.PaymentStatusId).ToString() : string.Empty,
                ShippingStatus = order != null ? ((ShippingStatus)order.ShippingStatusId).ToString() : string.Empty,
                CreatedOn = dateTimeHelper.ConvertToUserTime(h.CreatedOnUtc, DateTimeKind.Utc),
            });
        }

        return Json(new DataSourceResult { Data = models, Total = models.Count });
    }

    [HttpPost]
    public async Task<IActionResult> ProcessNextPayment(int id)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payment = await orderService.GetRecurringPaymentByIdAsync(id);
        if (payment is null)
            return RedirectToAction("List");

        var errors = await orderProcessingService.ProcessNextRecurringPaymentAsync(payment);

        var model = new RecurringPaymentModel();
        await PrepareRecurringPaymentModelAsync(model, payment);

        if (errors.Any())
            model.PaymentType = $"Error: {string.Join("; ", errors)}";

        return RedirectToAction("Edit", new { id = payment.Id });
    }

    [HttpPost]
    public async Task<IActionResult> CancelRecurringPayment(int id)
    {
        if (!permissionService.Authorize("ManageRecurringPayments"))
            return Forbid();

        var payment = await orderService.GetRecurringPaymentByIdAsync(id);
        if (payment is null)
            return RedirectToAction("List");

        var errors = await orderProcessingService.CancelRecurringPaymentAsync(payment);

        if (errors.Any())
        {
            // Errors are logged by the service — redirect back to edit
        }

        return RedirectToAction("Edit", new { id = payment.Id });
    }

    #endregion
}
