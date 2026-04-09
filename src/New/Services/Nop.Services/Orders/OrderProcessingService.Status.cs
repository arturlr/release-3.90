using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Payments;

namespace Nop.Services.Orders;

public partial class OrderProcessingService
{
    #region Order Status

    public virtual async Task CheckOrderStatusAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.PaymentStatus == PaymentStatus.Paid && !order.PaidDateUtc.HasValue)
        {
            order.PaidDateUtc = DateTime.UtcNow;
            await orderService.UpdateOrderAsync(order);
        }

        if (order.OrderStatus == OrderStatus.Pending &&
            (order.PaymentStatus == PaymentStatus.Authorized || order.PaymentStatus == PaymentStatus.Paid))
        {
            await SetOrderStatusAsync(order, OrderStatus.Processing, false);
        }

        if (order.OrderStatus == OrderStatus.Pending &&
            (order.ShippingStatus == ShippingStatus.PartiallyShipped ||
             order.ShippingStatus == ShippingStatus.Shipped ||
             order.ShippingStatus == ShippingStatus.Delivered))
        {
            await SetOrderStatusAsync(order, OrderStatus.Processing, false);
        }

        if (order.OrderStatus != OrderStatus.Cancelled && order.OrderStatus != OrderStatus.Complete &&
            order.PaymentStatus == PaymentStatus.Paid)
        {
            var completed = order.ShippingStatus == ShippingStatus.ShippingNotRequired ||
                (orderSettings.CompleteOrderWhenDelivered
                    ? order.ShippingStatus == ShippingStatus.Delivered
                    : order.ShippingStatus is ShippingStatus.Shipped or ShippingStatus.Delivered);

            if (completed)
                await SetOrderStatusAsync(order, OrderStatus.Complete, true);
        }
    }

    protected virtual async Task SetOrderStatusAsync(Order order, OrderStatus os, bool notifyCustomer)
    {
        var prev = order.OrderStatus;
        if (prev == os) return;

        order.OrderStatusId = (int)os;
        await orderService.UpdateOrderAsync(order);

        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = $"Order status has been changed to {os}",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });

        if (prev != OrderStatus.Complete && os == OrderStatus.Complete && notifyCustomer)
            await workflowMessageService.SendOrderCompletedCustomerNotificationAsync(order, order.CustomerLanguageId);

        if (prev != OrderStatus.Cancelled && os == OrderStatus.Cancelled && notifyCustomer)
            await workflowMessageService.SendOrderCancelledCustomerNotificationAsync(order, order.CustomerLanguageId);

        // Reward points
        if (os == OrderStatus.Complete)
            await AwardRewardPointsAsync(order);
        if (os == OrderStatus.Cancelled)
            await ReduceRewardPointsAsync(order);

        // Gift cards
        if (orderSettings.ActivateGiftCardsAfterCompletingOrder && os == OrderStatus.Complete)
            await SetActivatedValueForPurchasedGiftCardsAsync(order, true);
        if (orderSettings.DeactivateGiftCardsAfterCancellingOrder && os == OrderStatus.Cancelled)
            await SetActivatedValueForPurchasedGiftCardsAsync(order, false);
    }

    protected virtual async Task ProcessOrderPaidAsync(Order order)
    {
        await eventPublisher.PublishAsync(new OrderPaidEvent(order));

        if (order.OrderTotal != decimal.Zero)
        {
            await workflowMessageService.SendOrderPaidCustomerNotificationAsync(order, order.CustomerLanguageId);
            await workflowMessageService.SendOrderPaidStoreOwnerNotificationAsync(order, localizationSettings.DefaultAdminLanguageId);

            // Vendor notifications
            var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
            var vendorIds = new HashSet<int>();
            foreach (var oi in orderItems)
            {
                var product = await productService.GetProductByIdAsync(oi.ProductId);
                if (product?.VendorId > 0 && vendorIds.Add(product.VendorId))
                {
                    var vendor = await vendorService.GetVendorByIdAsync(product.VendorId);
                    if (vendor is { Deleted: false, Active: true })
                        await workflowMessageService.SendOrderPaidVendorNotificationAsync(order, vendor, localizationSettings.DefaultAdminLanguageId);
                }
            }
        }
    }

    #endregion

    #region Reward Points

    protected virtual async Task AwardRewardPointsAsync(Order order)
    {
        var totalForRewardPoints = orderTotalCalculationService.CalculateApplicableOrderTotalForRewardPoints(
            order.OrderShippingInclTax, order.OrderTotal);
        var customer = await customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer == null) return;
        var points = await orderTotalCalculationService.CalculateRewardPointsAsync(customer, totalForRewardPoints);
        if (points == 0) return;
        if (order.RewardPointsHistoryEntryId.HasValue) return;

        DateTime? activatingDate = null;
        if (rewardPointsSettings.ActivationDelay > 0)
        {
            var delayHours = ((RewardPointsActivatingDelayPeriod)rewardPointsSettings.ActivationDelayPeriodId)
                .ToHours(rewardPointsSettings.ActivationDelay);
            activatingDate = DateTime.UtcNow.AddHours(delayHours);
        }

        var entryId = await rewardPointService.AddRewardPointsHistoryEntryAsync(customer, points, order.StoreId,
            string.Format(await localizationService.GetResourceAsync("RewardPoints.Message.EarnedForOrder"), order.CustomOrderNumber),
            activatingDate: activatingDate);
        order.RewardPointsHistoryEntryId = entryId;
        await orderService.UpdateOrderAsync(order);
    }

    protected virtual async Task ReduceRewardPointsAsync(Order order)
    {
        var totalForRewardPoints = orderTotalCalculationService.CalculateApplicableOrderTotalForRewardPoints(
            order.OrderShippingInclTax, order.OrderTotal);
        var customer = await customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer == null) return;
        var points = await orderTotalCalculationService.CalculateRewardPointsAsync(customer, totalForRewardPoints);
        if (points == 0 || !order.RewardPointsHistoryEntryId.HasValue) return;

        var entry = await rewardPointService.GetRewardPointsHistoryEntryByIdAsync(order.RewardPointsHistoryEntryId.Value);
        if (entry != null && entry.CreatedOnUtc > DateTime.UtcNow)
            await rewardPointService.DeleteRewardPointsHistoryEntryAsync(entry);
        else
            await rewardPointService.AddRewardPointsHistoryEntryAsync(customer, -points, order.StoreId,
                string.Format(await localizationService.GetResourceAsync("RewardPoints.Message.ReducedForOrder"), order.CustomOrderNumber));
    }

    protected virtual async Task ReturnBackRedeemedRewardPointsAsync(Order order)
    {
        if (!order.RewardPointsHistoryEntryId.HasValue) return;
        var entry = await rewardPointService.GetRewardPointsHistoryEntryByIdAsync(order.RewardPointsHistoryEntryId.Value);
        if (entry == null || entry.Points >= 0) return; // Points should be negative (redeemed)
        var customer = await customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer == null) return;
        await rewardPointService.AddRewardPointsHistoryEntryAsync(customer, -entry.Points, order.StoreId,
            string.Format(await localizationService.GetResourceAsync("RewardPoints.Message.ReturnedForOrder"), order.CustomOrderNumber));
    }

    #endregion

    #region Gift Cards

    protected virtual async Task SetActivatedValueForPurchasedGiftCardsAsync(Order order, bool activate)
    {
        var giftCards = await giftCardService.GetAllGiftCardsAsync(purchasedWithOrderId: order.Id,
            isGiftCardActivated: !activate);
        foreach (var gc in giftCards)
        {
            if (activate)
            {
                gc.IsGiftCardActivated = true;
                if (gc.GiftCardTypeId == (int)GiftCardType.Virtual &&
                    !string.IsNullOrEmpty(gc.RecipientEmail) && !string.IsNullOrEmpty(gc.SenderEmail))
                {
                    var langId = order.CustomerLanguageId;
                    if (langId == 0)
                    {
                        var langs = await languageService.GetAllLanguagesAsync();
                        langId = langs.FirstOrDefault()?.Id ?? 0;
                    }
                    var queuedId = await workflowMessageService.SendGiftCardNotificationAsync(gc, langId);
                    if (queuedId > 0)
                        gc.IsRecipientNotified = true;
                }
            }
            else
            {
                gc.IsGiftCardActivated = false;
            }
            await giftCardService.UpdateGiftCardAsync(gc);
        }
    }

    #endregion

    #region Cancel

    public virtual bool CanCancelOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.OrderStatus != OrderStatus.Cancelled;
    }

    public virtual async Task CancelOrderAsync(Order order, bool notifyCustomer)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanCancelOrder(order))
            throw new NopException("Cannot do cancel for order.");

        await SetOrderStatusAsync(order, OrderStatus.Cancelled, notifyCustomer);

        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id, Note = "Order has been cancelled",
            DisplayToCustomer = false, CreatedOnUtc = DateTime.UtcNow
        });

        await ReturnBackRedeemedRewardPointsAsync(order);

        // Cancel recurring payments
        var recurringPayments = await orderService.SearchRecurringPaymentsAsync(initialOrderId: order.Id);
        foreach (var rp in recurringPayments)
            await CancelRecurringPaymentAsync(rp);

        // Adjust inventory
        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        foreach (var oi in orderItems)
        {
            var product = await productService.GetProductByIdAsync(oi.ProductId);
            if (product != null)
                await productService.AdjustInventoryAsync(product, oi.Quantity, oi.AttributesXml ?? string.Empty,
                    string.Format(await localizationService.GetResourceAsync("Admin.StockQuantityHistory.Messages.CancelOrder"), order.Id));
        }

        await eventPublisher.PublishAsync(new OrderCancelledEvent(order));
    }

    #endregion

    #region Delete Order

    public virtual async Task DeleteOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.OrderStatus != OrderStatus.Cancelled)
        {
            await ReturnBackRedeemedRewardPointsAsync(order);
            await ReduceRewardPointsAsync(order);

            var recurringPayments = await orderService.SearchRecurringPaymentsAsync(initialOrderId: order.Id);
            foreach (var rp in recurringPayments)
                await CancelRecurringPaymentAsync(rp);

            var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
            foreach (var oi in orderItems)
            {
                var product = await productService.GetProductByIdAsync(oi.ProductId);
                if (product != null)
                    await productService.AdjustInventoryAsync(product, oi.Quantity, oi.AttributesXml ?? string.Empty,
                        string.Format(await localizationService.GetResourceAsync("Admin.StockQuantityHistory.Messages.DeleteOrder"), order.Id));
            }
        }

        if (orderSettings.DeactivateGiftCardsAfterDeletingOrder)
            await SetActivatedValueForPurchasedGiftCardsAsync(order, false);

        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id, Note = "Order has been deleted",
            DisplayToCustomer = false, CreatedOnUtc = DateTime.UtcNow
        });

        await orderService.DeleteOrderAsync(order);
    }

    #endregion
}
