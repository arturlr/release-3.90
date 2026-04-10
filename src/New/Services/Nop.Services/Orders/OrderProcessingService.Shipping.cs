using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Payments;

namespace Nop.Services.Orders;

public partial class OrderProcessingService
{
    #region Shipping

    public virtual async Task ShipAsync(Shipment shipment, bool notifyCustomer)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        var order = await orderService.GetOrderByIdAsync(shipment.OrderId)
            ?? throw new Exception("Order cannot be loaded");
        if (shipment.ShippedDateUtc.HasValue)
            throw new Exception("This shipment is already shipped");

        shipment.ShippedDateUtc = DateTime.UtcNow;
        await shipmentService.UpdateShipmentAsync(shipment);

        // Determine shipping status based on remaining items
        // Simplified: mark as Shipped (full shipment status tracking deferred)
        order.ShippingStatusId = (int)ShippingStatus.Shipped;
        await orderService.UpdateOrderAsync(order);

        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = $"Shipment# {shipment.Id} has been sent",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });

        if (notifyCustomer)
            await workflowMessageService.SendShipmentSentCustomerNotificationAsync(shipment, order.CustomerLanguageId);

        await CheckOrderStatusAsync(order);
    }

    public virtual async Task DeliverAsync(Shipment shipment, bool notifyCustomer)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        var order = await orderService.GetOrderByIdAsync(shipment.OrderId)
            ?? throw new Exception("Order cannot be loaded");
        if (!shipment.ShippedDateUtc.HasValue)
            throw new Exception("This shipment is not shipped yet");
        if (shipment.DeliveryDateUtc.HasValue)
            throw new Exception("This shipment is already delivered");

        shipment.DeliveryDateUtc = DateTime.UtcNow;
        await shipmentService.UpdateShipmentAsync(shipment);

        order.ShippingStatusId = (int)ShippingStatus.Delivered;
        await orderService.UpdateOrderAsync(order);

        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = $"Shipment# {shipment.Id} has been delivered",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });

        if (notifyCustomer)
            await workflowMessageService.SendShipmentDeliveredCustomerNotificationAsync(shipment, order.CustomerLanguageId);

        await CheckOrderStatusAsync(order);
    }

    #endregion

    #region Recurring Payments

    public virtual async Task<IList<string>> ProcessNextRecurringPaymentAsync(RecurringPayment recurringPayment,
        ProcessPaymentResult? paymentResult = null)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        try
        {
            if (!recurringPayment.IsActive)
                throw new NopException("Recurring payment is not active");

            var initialOrder = await orderService.GetOrderByIdAsync(recurringPayment.InitialOrderId)
                ?? throw new NopException("Initial order could not be loaded");
            var customer = await customerService.GetCustomerByIdAsync(initialOrder.CustomerId)
                ?? throw new NopException("Customer could not be loaded");

            var processPaymentRequest = new ProcessPaymentRequest
            {
                StoreId = initialOrder.StoreId,
                CustomerId = customer.Id,
                OrderGuid = Guid.NewGuid(),
                InitialOrderId = initialOrder.Id,
                RecurringCycleLength = recurringPayment.CycleLength,
                RecurringCyclePeriod = (RecurringProductCyclePeriod)recurringPayment.CyclePeriodId,
                RecurringTotalCycles = recurringPayment.TotalCycles,
                PaymentMethodSystemName = initialOrder.PaymentMethodSystemName,
            };

            // Process payment
            ProcessPaymentResult processPaymentResult;
            var skipPayment = initialOrder.OrderTotal == decimal.Zero;
            if (!skipPayment)
            {
                if (initialOrder.AllowStoringCreditCardNumber)
                {
                    processPaymentRequest.CreditCardType = encryptionService.DecryptText(initialOrder.CardType ?? string.Empty);
                    processPaymentRequest.CreditCardName = encryptionService.DecryptText(initialOrder.CardName ?? string.Empty);
                    processPaymentRequest.CreditCardNumber = encryptionService.DecryptText(initialOrder.CardNumber ?? string.Empty);
                    processPaymentRequest.CreditCardCvv2 = encryptionService.DecryptText(initialOrder.CardCvv2 ?? string.Empty);
                    try
                    {
                        processPaymentRequest.CreditCardExpireMonth = Convert.ToInt32(encryptionService.DecryptText(initialOrder.CardExpirationMonth ?? string.Empty));
                        processPaymentRequest.CreditCardExpireYear = Convert.ToInt32(encryptionService.DecryptText(initialOrder.CardExpirationYear ?? string.Empty));
                    }
                    catch { /* ignore parse errors */ }
                }

                var rpType = await paymentService.GetRecurringPaymentTypeAsync(processPaymentRequest.PaymentMethodSystemName!);
                processPaymentResult = rpType switch
                {
                    RecurringPaymentType.Manual => await paymentService.ProcessRecurringPaymentAsync(processPaymentRequest),
                    RecurringPaymentType.Automatic => paymentResult ?? new ProcessPaymentResult(),
                    _ => throw new NopException("Recurring payments are not supported by selected payment method")
                };
            }
            else
            {
                processPaymentResult = paymentResult ?? new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Paid };
            }

            if (processPaymentResult.Success)
            {
                // Create order from initial order details
                var order = await CreateRecurringOrderAsync(processPaymentRequest, processPaymentResult, initialOrder);

                await SendNotificationsAndSaveNotesAsync(order);
                await CheckOrderStatusAsync(order);
                await eventPublisher.PublishAsync(new OrderPlacedEvent(order));
                if (order.PaymentStatus == PaymentStatus.Paid)
                    await ProcessOrderPaidAsync(order);

                recurringPayment.LastPaymentFailed = false;
                await orderService.InsertRecurringPaymentHistoryAsync(new RecurringPaymentHistory
                {
                    RecurringPaymentId = recurringPayment.Id,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });
                await orderService.UpdateRecurringPaymentAsync(recurringPayment);
                return [];
            }
            else
            {
                var logError = string.Join("; ", processPaymentResult.Errors.Select((e, i) => $"Error {i + 1}: {e}"));
                logger.Error($"Error while processing recurring order. {logError}");

                if (processPaymentResult.RecurringPaymentFailed)
                {
                    recurringPayment.LastPaymentFailed = true;
                    await orderService.UpdateRecurringPaymentAsync(recurringPayment);

                    if (paymentSettings.CancelRecurringPaymentsAfterFailedPayment)
                    {
                        await CancelRecurringPaymentAsync(recurringPayment);
                        await workflowMessageService.SendRecurringPaymentCancelledCustomerNotificationAsync(
                            recurringPayment, initialOrder.CustomerLanguageId);
                    }
                    else
                    {
                        await workflowMessageService.SendRecurringPaymentFailedCustomerNotificationAsync(
                            recurringPayment, initialOrder.CustomerLanguageId);
                    }
                }
                return processPaymentResult.Errors;
            }
        }
        catch (Exception exc)
        {
            logger.Error($"Error while processing recurring order. {exc.Message}", exc);
            throw;
        }
    }

    private async Task<Order> CreateRecurringOrderAsync(ProcessPaymentRequest request,
        ProcessPaymentResult paymentResult, Order initialOrder)
    {
        var order = new Order
        {
            StoreId = initialOrder.StoreId,
            OrderGuid = request.OrderGuid,
            CustomerId = initialOrder.CustomerId,
            CustomerLanguageId = initialOrder.CustomerLanguageId,
            CustomerTaxDisplayType = initialOrder.CustomerTaxDisplayType,
            CustomerIp = webHelper.GetCurrentIpAddress(),
            OrderSubtotalInclTax = initialOrder.OrderSubtotalInclTax,
            OrderSubtotalExclTax = initialOrder.OrderSubtotalExclTax,
            OrderSubTotalDiscountInclTax = initialOrder.OrderSubTotalDiscountInclTax,
            OrderSubTotalDiscountExclTax = initialOrder.OrderSubTotalDiscountExclTax,
            OrderShippingInclTax = initialOrder.OrderShippingInclTax,
            OrderShippingExclTax = initialOrder.OrderShippingExclTax,
            PaymentMethodAdditionalFeeInclTax = initialOrder.PaymentMethodAdditionalFeeInclTax,
            PaymentMethodAdditionalFeeExclTax = initialOrder.PaymentMethodAdditionalFeeExclTax,
            TaxRates = initialOrder.TaxRates,
            OrderTax = initialOrder.OrderTax,
            OrderTotal = initialOrder.OrderTotal,
            RefundedAmount = decimal.Zero,
            OrderDiscount = initialOrder.OrderDiscount,
            CheckoutAttributeDescription = initialOrder.CheckoutAttributeDescription,
            CheckoutAttributesXml = initialOrder.CheckoutAttributesXml,
            CustomerCurrencyCode = initialOrder.CustomerCurrencyCode,
            CurrencyRate = initialOrder.CurrencyRate,
            AffiliateId = initialOrder.AffiliateId,
            OrderStatus = OrderStatus.Pending,
            PaymentMethodSystemName = initialOrder.PaymentMethodSystemName,
            AuthorizationTransactionId = paymentResult.AuthorizationTransactionId,
            AuthorizationTransactionCode = paymentResult.AuthorizationTransactionCode,
            AuthorizationTransactionResult = paymentResult.AuthorizationTransactionResult,
            CaptureTransactionId = paymentResult.CaptureTransactionId,
            CaptureTransactionResult = paymentResult.CaptureTransactionResult,
            SubscriptionTransactionId = paymentResult.SubscriptionTransactionId,
            PaymentStatus = paymentResult.NewPaymentStatus,
            BillingAddressId = initialOrder.BillingAddressId,
            ShippingAddressId = initialOrder.ShippingAddressId,
            PickupAddressId = initialOrder.PickupAddressId,
            ShippingStatus = initialOrder.ShippingStatus == ShippingStatus.ShippingNotRequired
                ? ShippingStatus.ShippingNotRequired : ShippingStatus.NotYetShipped,
            ShippingMethod = initialOrder.ShippingMethod,
            PickUpInStore = initialOrder.PickUpInStore,
            ShippingRateComputationMethodSystemName = initialOrder.ShippingRateComputationMethodSystemName,
            VatNumber = initialOrder.VatNumber,
            CreatedOnUtc = DateTime.UtcNow,
            CustomOrderNumber = string.Empty
        };

        await orderService.InsertOrderAsync(order);
        order.CustomOrderNumber = customNumberFormatter.GenerateOrderCustomNumber(order);
        await orderService.UpdateOrderAsync(order);

        // Copy order items from initial order
        var initialItems = await orderService.GetOrderItemsByOrderIdAsync(initialOrder.Id);
        foreach (var oi in initialItems)
        {
            var newItem = new OrderItem
            {
                OrderItemGuid = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = oi.ProductId,
                UnitPriceInclTax = oi.UnitPriceInclTax,
                UnitPriceExclTax = oi.UnitPriceExclTax,
                PriceInclTax = oi.PriceInclTax,
                PriceExclTax = oi.PriceExclTax,
                OriginalProductCost = oi.OriginalProductCost,
                AttributeDescription = oi.AttributeDescription,
                AttributesXml = oi.AttributesXml,
                Quantity = oi.Quantity,
                DiscountAmountInclTax = oi.DiscountAmountInclTax,
                DiscountAmountExclTax = oi.DiscountAmountExclTax,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = 0,
                ItemWeight = oi.ItemWeight,
                RentalStartDateUtc = oi.RentalStartDateUtc,
                RentalEndDateUtc = oi.RentalEndDateUtc
            };
            await orderService.InsertOrderItemAsync(newItem);

            // Gift cards for recurring
            var product = await productService.GetProductByIdAsync(oi.ProductId);
            if (product is { IsGiftCard: true })
            {
                productAttributeParser.GetGiftCardAttribute(oi.AttributesXml ?? string.Empty,
                    out var recipientName, out var recipientEmail, out var senderName, out var senderEmail, out var giftCardMessage);
                for (var i = 0; i < oi.Quantity; i++)
                {
                    await giftCardService.InsertGiftCardAsync(new GiftCard
                    {
                        GiftCardTypeId = product.GiftCardTypeId,
                        PurchasedWithOrderItemId = newItem.Id,
                        Amount = oi.UnitPriceExclTax,
                        IsGiftCardActivated = false,
                        GiftCardCouponCode = giftCardService.GenerateGiftCardCode(),
                        RecipientName = recipientName,
                        RecipientEmail = recipientEmail,
                        SenderName = senderName,
                        SenderEmail = senderEmail,
                        Message = giftCardMessage,
                        IsRecipientNotified = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                }
            }

            // Inventory
            if (product != null)
                await productService.AdjustInventoryAsync(product, -oi.Quantity, oi.AttributesXml ?? string.Empty,
                    string.Format(await localizationService.GetResourceAsync("Admin.StockQuantityHistory.Messages.PlaceOrder"), order.Id));
        }

        return order;
    }

    public virtual async Task<IList<string>> CancelRecurringPaymentAsync(RecurringPayment recurringPayment)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        var initialOrder = await orderService.GetOrderByIdAsync(recurringPayment.InitialOrderId);
        if (initialOrder == null)
            return ["Initial order could not be loaded"];

        CancelRecurringPaymentResult? result = null;
        try
        {
            result = await paymentService.CancelRecurringPaymentAsync(new CancelRecurringPaymentRequest { Order = initialOrder });
            if (result.Success)
            {
                recurringPayment.IsActive = false;
                await orderService.UpdateRecurringPaymentAsync(recurringPayment);
                await orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = initialOrder.Id,
                    Note = "Recurring payment has been cancelled",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                await workflowMessageService.SendRecurringPaymentCancelledStoreOwnerNotificationAsync(
                    recurringPayment, localizationSettings.DefaultAdminLanguageId);
            }
        }
        catch (Exception exc)
        {
            result ??= new CancelRecurringPaymentResult();
            result.AddError($"Error: {exc.Message}");
        }

        if (result.Errors.Count > 0)
        {
            var error = string.Join(". ", result.Errors.Select((e, i) => $"Error {i}: {e}"));
            await orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = initialOrder.Id,
                Note = $"Unable to cancel recurring payment. {error}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            logger.Error($"Error cancelling recurring payment. Order #{initialOrder.Id}. Error: {error}");
        }
        return result.Errors;
    }

    public virtual bool CanCancelRecurringPayment(Customer customerToValidate, RecurringPayment recurringPayment, Order? initialOrder)
    {
        if (recurringPayment == null || customerToValidate == null || initialOrder == null) return false;
        if (initialOrder.OrderStatus == OrderStatus.Cancelled) return false;
        // Non-admin must be the owner
        var adminRole = customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Administrators).GetAwaiter().GetResult();
        var roleIds = customerService.GetCustomerRoleIdsAsync(customerToValidate).GetAwaiter().GetResult();
        var isAdmin = adminRole != null && roleIds.Contains(adminRole.Id);
        if (!isAdmin && initialOrder.CustomerId != customerToValidate.Id) return false;
        return true;
    }

    public virtual bool CanRetryLastRecurringPayment(Customer customer, RecurringPayment recurringPayment, Order? initialOrder)
    {
        if (recurringPayment == null || customer == null || initialOrder == null) return false;
        if (initialOrder.OrderStatus == OrderStatus.Cancelled) return false;
        if (!recurringPayment.LastPaymentFailed) return false;
        var rpType = paymentService.GetRecurringPaymentTypeAsync(initialOrder.PaymentMethodSystemName!).GetAwaiter().GetResult();
        if (rpType != RecurringPaymentType.Manual) return false;
        var adminRole = customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Administrators).GetAwaiter().GetResult();
        var roleIds = customerService.GetCustomerRoleIdsAsync(customer).GetAwaiter().GetResult();
        var isAdmin = adminRole != null && roleIds.Contains(adminRole.Id);
        if (!isAdmin && initialOrder.CustomerId != customer.Id) return false;
        return true;
    }

    #endregion

    #region Reorder

    public virtual async Task ReOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        var customer = await customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer == null) return;

        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        foreach (var oi in orderItems)
        {
            var product = await productService.GetProductByIdAsync(oi.ProductId);
            if (product == null) continue;
            await shoppingCartService.AddToCartAsync(customer, product,
                ShoppingCartType.ShoppingCart, order.StoreId,
                oi.AttributesXml, oi.UnitPriceExclTax,
                oi.RentalStartDateUtc, oi.RentalEndDateUtc,
                oi.Quantity, false);
        }

        await genericAttributeService.SaveAttributeAsync(customer,
            SystemCustomerAttributeNames.CheckoutAttributes,
            order.CheckoutAttributesXml ?? string.Empty, order.StoreId);
    }

    #endregion

    #region Return Request

    public virtual async Task<bool> IsReturnRequestAllowedAsync(Order order)
    {
        if (!orderSettings.ReturnRequestsEnabled) return false;
        if (order is null or { Deleted: true }) return false;
        if (order.OrderStatus != OrderStatus.Complete) return false;

        if (orderSettings.NumberOfDaysReturnRequestAvailable > 0)
        {
            var daysPassed = (DateTime.UtcNow - order.CreatedOnUtc).TotalDays;
            if (daysPassed >= orderSettings.NumberOfDaysReturnRequestAvailable)
                return false;
        }

        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        foreach (var oi in orderItems)
        {
            var product = await productService.GetProductByIdAsync(oi.ProductId);
            if (product != null && !product.NotReturnable)
                return true;
        }
        return false;
    }

    #endregion

    #region Validation

    public virtual async Task<bool> ValidateMinOrderSubtotalAmountAsync(IList<ShoppingCartItem> cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        if (cart.Count > 0 && orderSettings.MinOrderSubtotalAmount > decimal.Zero)
        {
            var sub = await orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart,
                orderSettings.MinOrderSubtotalAmountIncludingTax);
            if (sub.SubTotalWithoutDiscount < orderSettings.MinOrderSubtotalAmount)
                return false;
        }
        return true;
    }

    public virtual async Task<bool> ValidateMinOrderTotalAmountAsync(IList<ShoppingCartItem> cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        if (cart.Count > 0 && orderSettings.MinOrderTotalAmount > decimal.Zero)
        {
            var total = await orderTotalCalculationService.GetShoppingCartTotalAsync(cart);
            if (total != null && total.OrderTotal < orderSettings.MinOrderTotalAmount)
                return false;
        }
        return true;
    }

    public virtual async Task<bool> IsPaymentWorkflowRequiredAsync(IList<ShoppingCartItem> cart, bool? useRewardPoints = null)
    {
        ArgumentNullException.ThrowIfNull(cart);
        var total = await orderTotalCalculationService.GetShoppingCartTotalAsync(cart, useRewardPoints: useRewardPoints);
        return total == null || total.OrderTotal != decimal.Zero;
    }

    #endregion
}
