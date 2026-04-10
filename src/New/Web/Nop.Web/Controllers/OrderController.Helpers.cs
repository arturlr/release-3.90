using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Seo;
using Nop.Web.Models.Order;

namespace Nop.Web.Controllers;

public partial class OrderController
{
    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        return registeredRole != null && roleIds.Contains(registeredRole.Id);
    }

    private async Task<CustomerOrderListModel> PrepareCustomerOrderListModelAsync()
    {
        var customer = workContext.CurrentCustomer;
        var orders = await orderService.SearchOrdersAsync(customerId: customer.Id);

        var model = new CustomerOrderListModel();

        foreach (var order in orders)
        {
            model.Orders.Add(new CustomerOrderListModel.OrderBriefModel
            {
                Id = order.Id,
                CustomOrderNumber = order.CustomOrderNumber,
                OrderTotal = await priceFormatter.FormatPriceAsync(order.OrderTotal),
                IsReturnRequestAllowed = await orderProcessingService.IsReturnRequestAllowedAsync(order),
                OrderStatusEnum = (OrderStatus)order.OrderStatusId,
                OrderStatus = ((OrderStatus)order.OrderStatusId).ToString(),
                PaymentStatus = ((PaymentStatus)order.PaymentStatusId).ToString(),
                ShippingStatus = ((ShippingStatus)order.ShippingStatusId).ToString(),
                CreatedOn = dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
            });
        }

        // Recurring payments
        var recurringPayments = await orderService.SearchRecurringPaymentsAsync(customerId: customer.Id);

        foreach (var rp in recurringPayments)
        {
            var initialOrder = await orderService.GetOrderByIdAsync(rp.InitialOrderId);
            var history = await orderService.GetRecurringPaymentHistoryAsync(rp);
            var cyclesRemaining = rp.TotalCycles - history.Count;

            var nextPayment = ComputeNextPaymentDate(rp, history);

            model.RecurringOrders.Add(new CustomerOrderListModel.RecurringOrderModel
            {
                Id = rp.Id,
                StartDate = dateTimeHelper.ConvertToUserTime(rp.StartDateUtc, DateTimeKind.Utc).ToString(),
                CycleInfo = $"{rp.CycleLength} {rp.CyclePeriod}(s)",
                NextPayment = nextPayment.HasValue
                    ? dateTimeHelper.ConvertToUserTime(nextPayment.Value, DateTimeKind.Utc).ToString()
                    : "",
                TotalCycles = rp.TotalCycles,
                CyclesRemaining = cyclesRemaining > 0 ? cyclesRemaining : 0,
                InitialOrderId = rp.InitialOrderId,
                InitialOrderNumber = initialOrder?.CustomOrderNumber,
                CanCancel = orderProcessingService.CanCancelRecurringPayment(customer, rp, initialOrder),
                CanRetryLastPayment = orderProcessingService.CanRetryLastRecurringPayment(customer, rp, initialOrder),
            });
        }

        return model;
    }

    private async Task<OrderDetailsModel> PrepareOrderDetailsModelAsync(Order order)
    {
        var model = new OrderDetailsModel
        {
            Id = order.Id,
            CustomOrderNumber = order.CustomOrderNumber,
            CreatedOn = dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
            OrderStatus = ((OrderStatus)order.OrderStatusId).ToString(),
            IsReOrderAllowed = orderSettings.IsReOrderAllowed,
            IsReturnRequestAllowed = await orderProcessingService.IsReturnRequestAllowedAsync(order),
            PdfInvoiceDisabled = pdfSettings.DisablePdfInvoicesForPendingOrders
                                 && order.OrderStatusId == (int)OrderStatus.Pending,
            ShowSku = catalogSettings.ShowSkuOnProductDetailsPage,

            // Shipping
            IsShippable = !string.IsNullOrEmpty(order.ShippingMethod),
            ShippingStatus = ((ShippingStatus)order.ShippingStatusId).ToString(),
            ShippingMethod = order.ShippingMethod,

            // Payment
            PaymentMethod = order.PaymentMethodSystemName,
            CanRePostProcessPayment = await paymentService.CanRePostProcessPaymentAsync(order),

            // Totals
            OrderSubtotal = await priceFormatter.FormatPriceAsync(order.OrderSubtotalInclTax),
            OrderSubTotalDiscount = order.OrderSubTotalDiscountInclTax > 0
                ? await priceFormatter.FormatPriceAsync(order.OrderSubTotalDiscountInclTax)
                : null,
            OrderShipping = await priceFormatter.FormatPriceAsync(order.OrderShippingInclTax),
            PaymentMethodAdditionalFee = order.PaymentMethodAdditionalFeeInclTax > 0
                ? await priceFormatter.FormatPriceAsync(order.PaymentMethodAdditionalFeeInclTax)
                : null,
            CheckoutAttributeInfo = !string.IsNullOrEmpty(order.CheckoutAttributeDescription)
                ? order.CheckoutAttributeDescription
                : null,
            Tax = await priceFormatter.FormatPriceAsync(order.OrderTax),
            DisplayTax = order.OrderTax > 0,
            DisplayTaxRates = false,
            OrderTotalDiscount = order.OrderDiscount > 0
                ? await priceFormatter.FormatPriceAsync(order.OrderDiscount)
                : null,
            OrderTotal = await priceFormatter.FormatPriceAsync(order.OrderTotal),
        };

        // Reward points
        if (order.RewardPointsHistoryEntryId is > 0)
        {
            var rpEntry = await rewardPointService.GetRewardPointsHistoryEntryByIdAsync(
                order.RewardPointsHistoryEntryId.Value);
            if (rpEntry != null)
            {
                model.RedeemedRewardPoints = -rpEntry.Points;
                model.RedeemedRewardPointsAmount = await priceFormatter.FormatPriceAsync(-rpEntry.UsedAmount);
            }
        }

        // Gift cards — query all gift cards used with this order
        var giftCards = await giftCardService.GetAllGiftCardsAsync(usedWithOrderId: order.Id);
        foreach (var gc in giftCards)
        {
            var usageHistory = await giftCardService.GetGiftCardUsageHistoryAsync(gc);
            var usedWithThisOrder = usageHistory.FirstOrDefault(h => h.UsedWithOrderId == order.Id);
            if (usedWithThisOrder != null)
            {
                model.GiftCards.Add(new OrderDetailsModel.GiftCardModel
                {
                    CouponCode = gc.GiftCardCouponCode,
                    Amount = await priceFormatter.FormatPriceAsync(-usedWithThisOrder.UsedValue),
                });
            }
        }

        // Order items
        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        foreach (var oi in orderItems)
        {
            var product = await productService.GetProductByIdAsync(oi.ProductId);
            model.Items.Add(new OrderDetailsModel.OrderItemModel
            {
                Id = oi.Id,
                OrderItemGuid = oi.OrderItemGuid,
                Sku = product?.Sku,
                ProductId = oi.ProductId,
                ProductName = product?.Name,
                ProductSeName = product != null ? await product.GetSeNameAsync(0, urlRecordService) : null,
                UnitPrice = await priceFormatter.FormatPriceAsync(oi.UnitPriceInclTax),
                SubTotal = await priceFormatter.FormatPriceAsync(oi.PriceInclTax),
                Quantity = oi.Quantity,
                AttributeInfo = oi.AttributeDescription,
            });
        }

        // Order notes (visible to customer)
        var orderNotes = await orderService.GetOrderNotesByOrderIdAsync(order.Id, displayToCustomer: true);
        foreach (var note in orderNotes)
        {
            model.OrderNotes.Add(new OrderDetailsModel.OrderNoteModel
            {
                Id = note.Id,
                HasDownload = note.DownloadId > 0,
                Note = note.Note,
                CreatedOn = dateTimeHelper.ConvertToUserTime(note.CreatedOnUtc, DateTimeKind.Utc),
            });
        }

        // Shipments
        var shipments = await shipmentService.GetAllShipmentsAsync();
        foreach (var shipment in shipments.Where(s => s.OrderId == order.Id))
        {
            model.Shipments.Add(new OrderDetailsModel.ShipmentBriefModel
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                ShippedDate = shipment.ShippedDateUtc.HasValue
                    ? dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)
                    : null,
                DeliveryDate = shipment.DeliveryDateUtc.HasValue
                    ? dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)
                    : null,
            });
        }

        return model;
    }

    private async Task<ShipmentDetailsModel> PrepareShipmentDetailsModelAsync(Shipment shipment, Order order)
    {
        var model = new ShipmentDetailsModel
        {
            Id = shipment.Id,
            TrackingNumber = shipment.TrackingNumber,
            ShippedDate = shipment.ShippedDateUtc.HasValue
                ? dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)
                : null,
            DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)
                : null,
            ShowSku = catalogSettings.ShowSkuOnProductDetailsPage,
            OrderId = order.Id,
        };

        var shipmentItems = await shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
        foreach (var si in shipmentItems)
        {
            var orderItem = await orderService.GetOrderItemByIdAsync(si.OrderItemId);
            var product = orderItem != null ? await productService.GetProductByIdAsync(orderItem.ProductId) : null;

            model.Items.Add(new ShipmentDetailsModel.ShipmentItemModel
            {
                Id = si.Id,
                Sku = product?.Sku,
                ProductId = orderItem?.ProductId ?? 0,
                ProductName = product?.Name,
                ProductSeName = product != null ? await product.GetSeNameAsync(0, urlRecordService) : null,
                QuantityOrdered = orderItem?.Quantity ?? 0,
                QuantityShipped = si.Quantity,
            });
        }

        return model;
    }

    private async Task<CustomerRewardPointsModel> PrepareCustomerRewardPointsModelAsync(int page)
    {
        var customer = workContext.CurrentCustomer;
        var storeId = storeContext.CurrentStore.Id;
        var pageSize = rewardPointsSettings.PageSize > 0 ? rewardPointsSettings.PageSize : 10;

        var rewardPoints = await rewardPointService.GetRewardPointsHistoryAsync(
            customerId: customer.Id, storeId: storeId, pageIndex: page, pageSize: pageSize);

        var balance = await rewardPointService.GetRewardPointsBalanceAsync(customer.Id, storeId);

        var model = new CustomerRewardPointsModel
        {
            RewardPointsBalance = balance,
            RewardPointsAmount = await priceFormatter.FormatPriceAsync(balance * rewardPointsSettings.ExchangeRate),
            MinimumRewardPointsBalance = rewardPointsSettings.MinimumRewardPointsToUse,
            MinimumRewardPointsAmount = await priceFormatter.FormatPriceAsync(
                rewardPointsSettings.MinimumRewardPointsToUse * rewardPointsSettings.ExchangeRate),
        };

        var runningBalance = balance;
        foreach (var rph in rewardPoints)
        {
            model.RewardPoints.Add(new CustomerRewardPointsModel.RewardPointsHistoryModel
            {
                Id = rph.Id,
                Points = rph.Points,
                PointsBalance = runningBalance.ToString(),
                Message = rph.Message,
                CreatedOn = dateTimeHelper.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc),
            });
            runningBalance -= rph.Points;
        }

        return model;
    }

    private static DateTime? ComputeNextPaymentDate(RecurringPayment rp, IList<RecurringPaymentHistory> history)
    {
        if (!rp.IsActive)
            return null;

        if (history.Count >= rp.TotalCycles)
            return null;

        // Next payment = start date + cycle * payments made
        var lastPayment = history.OrderByDescending(h => h.CreatedOnUtc).FirstOrDefault();
        if (lastPayment == null)
            return rp.StartDateUtc;

        return rp.CyclePeriod switch
        {
            RecurringProductCyclePeriod.Days => lastPayment.CreatedOnUtc.AddDays(rp.CycleLength),
            RecurringProductCyclePeriod.Weeks => lastPayment.CreatedOnUtc.AddDays(7 * rp.CycleLength),
            RecurringProductCyclePeriod.Months => lastPayment.CreatedOnUtc.AddMonths(rp.CycleLength),
            RecurringProductCyclePeriod.Years => lastPayment.CreatedOnUtc.AddYears(rp.CycleLength),
            _ => (DateTime?)null,
        };
    }
}
