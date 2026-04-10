using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class GiftCardController(
    IGiftCardService giftCardService,
    IOrderService orderService,
    IPriceFormatter priceFormatter,
    IWorkflowMessageService workflowMessageService,
    IDateTimeHelper dateTimeHelper,
    ICurrencyService currencyService,
    CurrencySettings currencySettings,
    ILanguageService languageService,
    LocalizationSettings localizationSettings,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Utilities

    private async Task<string> GetPrimaryStoreCurrencyCodeAsync()
    {
        var currency = await currencyService.GetCurrencyByIdAsync(currencySettings.PrimaryStoreCurrencyId);
        return currency?.CurrencyCode ?? string.Empty;
    }

    private async Task PrepareGiftCardModelAsync(GiftCardModel model, GiftCard giftCard)
    {
        model.Id = giftCard.Id;
        model.GiftCardTypeId = giftCard.GiftCardTypeId;
        model.Amount = giftCard.Amount;
        model.IsGiftCardActivated = giftCard.IsGiftCardActivated;
        model.GiftCardCouponCode = giftCard.GiftCardCouponCode;
        model.RecipientName = giftCard.RecipientName;
        model.RecipientEmail = giftCard.RecipientEmail;
        model.SenderName = giftCard.SenderName;
        model.SenderEmail = giftCard.SenderEmail;
        model.Message = giftCard.Message;
        model.IsRecipientNotified = giftCard.IsRecipientNotified;
        model.CreatedOn = dateTimeHelper.ConvertToUserTime(giftCard.CreatedOnUtc, DateTimeKind.Utc);
        model.AmountStr = await priceFormatter.FormatPriceAsync(giftCard.Amount, true, false);
        model.RemainingAmountStr = await priceFormatter.FormatPriceAsync(
            await giftCardService.GetGiftCardRemainingAmountAsync(giftCard), true, false);
        model.PrimaryStoreCurrencyCode = await GetPrimaryStoreCurrencyCodeAsync();

        // Resolve PurchasedWithOrderItem → Order (replaces nav property)
        if (giftCard.PurchasedWithOrderItemId.HasValue && giftCard.PurchasedWithOrderItemId.Value > 0)
        {
            var orderItem = await orderService.GetOrderItemByIdAsync(giftCard.PurchasedWithOrderItemId.Value);
            if (orderItem is not null)
            {
                var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
                if (order is not null)
                {
                    model.PurchasedWithOrderId = order.Id;
                    model.PurchasedWithOrderNumber = order.CustomOrderNumber;
                }
            }
        }
    }

    #endregion

    #region Gift Cards

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        var model = new GiftCardListModel();
        model.ActivatedList.Add(new SelectListItem { Value = "0", Text = "All" });
        model.ActivatedList.Add(new SelectListItem { Value = "1", Text = "Activated" });
        model.ActivatedList.Add(new SelectListItem { Value = "2", Text = "Deactivated" });
        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> GiftCardList(DataSourceRequest command, GiftCardListModel model)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        bool? isGiftCardActivated = model.ActivatedId switch
        {
            1 => true,
            2 => false,
            _ => null
        };

        var giftCards = await giftCardService.GetAllGiftCardsAsync(
            isGiftCardActivated: isGiftCardActivated,
            giftCardCouponCode: model.CouponCode,
            recipientName: model.RecipientName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var items = new List<GiftCardGridModel>();
        foreach (var gc in giftCards)
        {
            items.Add(new GiftCardGridModel
            {
                Id = gc.Id,
                GiftCardCouponCode = gc.GiftCardCouponCode,
                RecipientName = gc.RecipientName,
                IsGiftCardActivated = gc.IsGiftCardActivated,
                AmountStr = await priceFormatter.FormatPriceAsync(gc.Amount, true, false),
                RemainingAmountStr = await priceFormatter.FormatPriceAsync(
                    await giftCardService.GetGiftCardRemainingAmountAsync(gc), true, false),
                CreatedOn = dateTimeHelper.ConvertToUserTime(gc.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return Json(new DataSourceResult { Data = items, Total = giftCards.TotalCount });
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        var model = new GiftCardModel
        {
            PrimaryStoreCurrencyCode = await GetPrimaryStoreCurrencyCodeAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(GiftCardModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var giftCard = new GiftCard
            {
                GiftCardTypeId = model.GiftCardTypeId,
                Amount = model.Amount,
                IsGiftCardActivated = model.IsGiftCardActivated,
                GiftCardCouponCode = model.GiftCardCouponCode,
                RecipientName = model.RecipientName,
                RecipientEmail = model.RecipientEmail,
                SenderName = model.SenderName,
                SenderEmail = model.SenderEmail,
                Message = model.Message,
                IsRecipientNotified = model.IsRecipientNotified,
                CreatedOnUtc = DateTime.UtcNow
            };
            await giftCardService.InsertGiftCardAsync(giftCard);

            customerActivityService.InsertActivity("AddNewGiftCard",
                "Added a new gift card", giftCard.GiftCardCouponCode ?? string.Empty);

            return continueEditing
                ? RedirectToAction("Edit", new { id = giftCard.Id })
                : RedirectToAction("List");
        }

        model.PrimaryStoreCurrencyCode = await GetPrimaryStoreCurrencyCodeAsync();
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        var giftCard = await giftCardService.GetGiftCardByIdAsync(id);
        if (giftCard is null)
            return RedirectToAction("List");

        var model = new GiftCardModel();
        await PrepareGiftCardModelAsync(model, giftCard);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(GiftCardModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        var giftCard = await giftCardService.GetGiftCardByIdAsync(model.Id);
        if (giftCard is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            giftCard.GiftCardTypeId = model.GiftCardTypeId;
            giftCard.Amount = model.Amount;
            giftCard.IsGiftCardActivated = model.IsGiftCardActivated;
            giftCard.GiftCardCouponCode = model.GiftCardCouponCode;
            giftCard.RecipientName = model.RecipientName;
            giftCard.RecipientEmail = model.RecipientEmail;
            giftCard.SenderName = model.SenderName;
            giftCard.SenderEmail = model.SenderEmail;
            giftCard.Message = model.Message;
            giftCard.IsRecipientNotified = model.IsRecipientNotified;
            await giftCardService.UpdateGiftCardAsync(giftCard);

            customerActivityService.InsertActivity("EditGiftCard",
                "Edited a gift card", giftCard.GiftCardCouponCode ?? string.Empty);

            if (continueEditing)
                return RedirectToAction("Edit", new { id = giftCard.Id });
            return RedirectToAction("List");
        }

        // Redisplay form on validation failure
        await PrepareGiftCardModelAsync(model, giftCard);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        var giftCard = await giftCardService.GetGiftCardByIdAsync(id);
        if (giftCard is null)
            return RedirectToAction("List");

        await giftCardService.DeleteGiftCardAsync(giftCard);

        customerActivityService.InsertActivity("DeleteGiftCard",
            "Deleted a gift card", giftCard.GiftCardCouponCode ?? string.Empty);

        return RedirectToAction("List");
    }

    [HttpPost]
    public JsonResult GenerateCouponCode()
    {
        return Json(new { CouponCode = giftCardService.GenerateGiftCardCode() });
    }

    #endregion

    #region Notify Recipient

    [HttpPost]
    public async Task<IActionResult> NotifyRecipient(int id)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Forbid();

        var giftCard = await giftCardService.GetGiftCardByIdAsync(id);
        if (giftCard is null)
            return RedirectToAction("List");

        var model = new GiftCardModel();
        await PrepareGiftCardModelAsync(model, giftCard);

        try
        {
            if (!CommonHelper.IsValidEmail(giftCard.RecipientEmail))
                throw new NopException("Recipient email is not valid");
            if (!CommonHelper.IsValidEmail(giftCard.SenderEmail))
                throw new NopException("Sender email is not valid");

            var languageId = 0;
            if (giftCard.PurchasedWithOrderItemId.HasValue && giftCard.PurchasedWithOrderItemId.Value > 0)
            {
                var orderItem = await orderService.GetOrderItemByIdAsync(giftCard.PurchasedWithOrderItemId.Value);
                if (orderItem is not null)
                {
                    var order = await orderService.GetOrderByIdAsync(orderItem.OrderId);
                    if (order is not null)
                    {
                        var customerLang = await languageService.GetLanguageByIdAsync(order.CustomerLanguageId);
                        customerLang ??= (await languageService.GetAllLanguagesAsync()).FirstOrDefault();
                        if (customerLang is not null)
                            languageId = customerLang.Id;
                    }
                }
            }

            if (languageId == 0)
                languageId = localizationSettings.DefaultAdminLanguageId;

            var queuedEmailId = await workflowMessageService.SendGiftCardNotificationAsync(giftCard, languageId);
            if (queuedEmailId > 0)
            {
                giftCard.IsRecipientNotified = true;
                await giftCardService.UpdateGiftCardAsync(giftCard);
                model.IsRecipientNotified = true;
            }
        }
        catch (Exception exc)
        {
            ErrorNotification(exc);
        }

        return View("Edit", model);
    }

    #endregion

    #region Usage History

    [HttpPost]
    public async Task<JsonResult> UsageHistoryList(int giftCardId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var giftCard = await giftCardService.GetGiftCardByIdAsync(giftCardId);
        if (giftCard is null)
            return Json(new DataSourceResult { Errors = "Gift card not found" });

        var usageHistory = await giftCardService.GetGiftCardUsageHistoryAsync(giftCard);

        var items = new List<GiftCardUsageHistoryModel>();
        foreach (var h in usageHistory)
        {
            var order = await orderService.GetOrderByIdAsync(h.UsedWithOrderId);
            items.Add(new GiftCardUsageHistoryModel
            {
                Id = h.Id,
                OrderId = h.UsedWithOrderId,
                CustomOrderNumber = order?.CustomOrderNumber,
                UsedValue = await priceFormatter.FormatPriceAsync(h.UsedValue, true, false),
                CreatedOn = dateTimeHelper.ConvertToUserTime(h.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        // In-memory paging (matching legacy pattern)
        var pagedItems = items
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize)
            .ToList();

        return Json(new DataSourceResult { Data = pagedItems, Total = items.Count });
    }

    [HttpPost]
    public async Task<JsonResult> UsageHistoryDelete(int id, int giftCardId)
    {
        if (!permissionService.Authorize("ManageGiftCards"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var giftCard = await giftCardService.GetGiftCardByIdAsync(giftCardId);
        if (giftCard is null)
            return Json(new DataSourceResult { Errors = "Gift card not found" });

        var usageHistory = await giftCardService.GetGiftCardUsageHistoryAsync(giftCard);
        var entry = usageHistory.FirstOrDefault(h => h.Id == id);
        if (entry is not null)
            await giftCardService.DeleteGiftCardUsageHistoryAsync(entry);

        return Json(new DataSourceResult());
    }

    #endregion
}
