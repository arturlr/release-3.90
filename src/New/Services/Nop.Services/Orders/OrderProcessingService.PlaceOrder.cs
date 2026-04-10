using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Payments;
using System.Globalization;

namespace Nop.Services.Orders;

public partial class OrderProcessingService
{
    public virtual async Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest)
    {
        ArgumentNullException.ThrowIfNull(processPaymentRequest);
        var result = new PlaceOrderResult();
        try
        {
            if (processPaymentRequest.OrderGuid == Guid.Empty)
                processPaymentRequest.OrderGuid = Guid.NewGuid();

            var details = await PreparePlaceOrderDetailsAsync(processPaymentRequest);

            // Payment workflow
            ProcessPaymentResult processPaymentResult;
            var skipPayment = details.OrderTotal == decimal.Zero;
            if (!skipPayment)
            {
                if (details.IsRecurringShoppingCart)
                {
                    var rpType = await paymentService.GetRecurringPaymentTypeAsync(processPaymentRequest.PaymentMethodSystemName!);
                    processPaymentResult = rpType switch
                    {
                        RecurringPaymentType.Manual or RecurringPaymentType.Automatic
                            => await paymentService.ProcessRecurringPaymentAsync(processPaymentRequest),
                        _ => throw new NopException("Recurring payments are not supported by selected payment method")
                    };
                }
                else
                {
                    processPaymentResult = await paymentService.ProcessPaymentAsync(processPaymentRequest);
                }
            }
            else
            {
                processPaymentResult = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Paid };
            }

            if (processPaymentResult.Success)
            {
                var order = await SaveOrderDetailsAsync(processPaymentRequest, processPaymentResult, details);
                result.PlacedOrder = order;

                await CreateOrderItemsAsync(order, details);

                // Clear cart
                foreach (var sci in details.Cart)
                    await shoppingCartService.DeleteShoppingCartItemAsync(sci, false);

                // Discount usage history
                foreach (var discount in details.AppliedDiscounts)
                {
                    var d = await discountService.GetDiscountByIdAsync(discount.Id);
                    if (d != null)
                        await discountService.InsertDiscountUsageHistoryAsync(new DiscountUsageHistory
                        {
                            DiscountId = d.Id,
                            OrderId = order.Id,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                }

                // Gift card usage history
                foreach (var agc in details.AppliedGiftCards)
                {
                    await giftCardService.InsertGiftCardUsageHistoryAsync(new GiftCardUsageHistory
                    {
                        GiftCardId = agc.GiftCard.Id,
                        UsedWithOrderId = order.Id,
                        UsedValue = agc.AmountCanBeUsed,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                }

                // Recurring payments
                if (details.IsRecurringShoppingCart)
                {
                    var rp = new RecurringPayment
                    {
                        CycleLength = processPaymentRequest.RecurringCycleLength,
                        CyclePeriodId = (int)processPaymentRequest.RecurringCyclePeriod,
                        TotalCycles = processPaymentRequest.RecurringTotalCycles,
                        StartDateUtc = DateTime.UtcNow,
                        IsActive = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        InitialOrderId = order.Id,
                    };
                    await orderService.InsertRecurringPaymentAsync(rp);

                    var rpType = await paymentService.GetRecurringPaymentTypeAsync(processPaymentRequest.PaymentMethodSystemName!);
                    if (rpType == RecurringPaymentType.Manual)
                    {
                        await orderService.InsertRecurringPaymentHistoryAsync(new RecurringPaymentHistory
                        {
                            RecurringPaymentId = rp.Id,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }
                }

                // Notifications
                await SendNotificationsAndSaveNotesAsync(order);

                // Reset checkout
                await customerService.ResetCheckoutDataAsync(details.Customer, processPaymentRequest.StoreId,
                    clearCouponCodes: true, clearCheckoutAttributes: true);
                customerActivityService.InsertActivity("PublicStore.PlaceOrder",
                    await localizationService.GetResourceAsync("ActivityLog.PublicStore.PlaceOrder"), order.Id);

                await CheckOrderStatusAsync(order);
                await eventPublisher.PublishAsync(new OrderPlacedEvent(order));

                if (order.PaymentStatus == PaymentStatus.Paid)
                    await ProcessOrderPaidAsync(order);
            }
            else
            {
                foreach (var error in processPaymentResult.Errors)
                    result.AddError(string.Format(
                        await localizationService.GetResourceAsync("Checkout.PaymentError"), error));
            }
        }
        catch (Exception exc)
        {
            logger.Error(exc.Message, exc);
            result.AddError(exc.Message);
        }

        if (!result.Success)
        {
            var logError = string.Join("; ", result.Errors.Select((e, i) => $"Error {i + 1}: {e}"));
            logger.Error($"Error while placing order. {logError}");
        }

        return result;
    }

    protected virtual async Task<PlaceOrderContainer> PreparePlaceOrderDetailsAsync(ProcessPaymentRequest request)
    {
        var details = new PlaceOrderContainer();

        details.Customer = await customerService.GetCustomerByIdAsync(request.CustomerId)
            ?? throw new ArgumentException("Customer is not set");

        // Affiliate
        var affiliate = await affiliateService.GetAffiliateByIdAsync(details.Customer.AffiliateId);
        if (affiliate is { Active: true, Deleted: false })
            details.AffiliateId = affiliate.Id;

        // Guest check
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        var roleIds = await customerService.GetCustomerRoleIdsAsync(details.Customer);
        var isGuest = guestRole != null && roleIds.Contains(guestRole.Id);
        if (isGuest && !orderSettings.AnonymousCheckoutAllowed)
            throw new NopException("Anonymous checkout is not allowed");

        // Currency
        var custCurrencyId = await details.Customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.CurrencyId, genericAttributeService, request.StoreId);
        var custCurrency = await currencyService.GetCurrencyByIdAsync(custCurrencyId);
        if (custCurrency is not { Published: true })
            custCurrency = workContext.WorkingCurrency;
        var primaryCurrency = await currencyService.GetCurrencyByIdAsync(currencySettings.PrimaryStoreCurrencyId);
        details.CustomerCurrencyCode = custCurrency.CurrencyCode!;
        details.CustomerCurrencyRate = primaryCurrency != null && primaryCurrency.Rate != 0
            ? custCurrency.Rate / primaryCurrency.Rate : 1m;

        // Language
        var custLangId = await details.Customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.LanguageId, genericAttributeService, request.StoreId);
        details.CustomerLanguage = await languageService.GetLanguageByIdAsync(custLangId)
            ?? workContext.WorkingLanguage;
        if (!details.CustomerLanguage.Published)
            details.CustomerLanguage = workContext.WorkingLanguage;

        // Billing address
        if (!details.Customer.BillingAddressId.HasValue)
            throw new NopException("Billing address is not provided");
        var billingAddr = await addressService.GetAddressByIdAsync(details.Customer.BillingAddressId.Value)
            ?? throw new NopException("Billing address is not provided");
        if (!CommonHelper.IsValidEmail(billingAddr.Email))
            throw new NopException("Email is not valid");
        // Clone billing address
        var clonedBilling = CloneAddress(billingAddr);
        await addressService.InsertAddressAsync(clonedBilling);
        details.BillingAddressId = clonedBilling.Id;

        // Checkout attributes
        details.CheckoutAttributesXml = await details.Customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, request.StoreId);
        details.CheckoutAttributeDescription = await checkoutAttributeFormatter.FormatAttributesAsync(
            details.CheckoutAttributesXml ?? string.Empty, details.Customer);

        // Cart
        details.Cart = await shoppingCartService.GetShoppingCartAsync(details.Customer,
            ShoppingCartType.ShoppingCart, request.StoreId);
        if (details.Cart.Count == 0)
            throw new NopException("Cart is empty");

        // Validate cart
        var warnings = await shoppingCartService.GetShoppingCartWarningsAsync(details.Cart,
            details.CheckoutAttributesXml ?? string.Empty, true);
        if (warnings.Count > 0)
            throw new NopException(string.Join("; ", warnings));

        // Min totals
        if (!await ValidateMinOrderSubtotalAmountAsync(details.Cart))
            throw new NopException(await localizationService.GetResourceAsync("Checkout.MinOrderSubtotalAmount"));
        if (!await ValidateMinOrderTotalAmountAsync(details.Cart))
            throw new NopException(await localizationService.GetResourceAsync("Checkout.MinOrderTotalAmount"));

        // Tax display type
        if (taxSettings.AllowCustomersToSelectTaxDisplayType)
        {
            var taxDisplayId = await details.Customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.TaxDisplayTypeId, genericAttributeService, request.StoreId);
            details.CustomerTaxDisplayType = (TaxDisplayType)taxDisplayId;
        }
        else
            details.CustomerTaxDisplayType = taxSettings.TaxDisplayType;

        // Subtotals
        var subInclTax = await orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, true);
        details.OrderSubTotalInclTax = subInclTax.SubTotalWithoutDiscount;
        details.OrderSubTotalDiscountInclTax = subInclTax.DiscountAmount;
        AddDiscounts(details.AppliedDiscounts, subInclTax.AppliedDiscounts);

        var subExclTax = await orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, false);
        details.OrderSubTotalExclTax = subExclTax.SubTotalWithoutDiscount;
        details.OrderSubTotalDiscountExclTax = subExclTax.DiscountAmount;

        // Shipping
        var cartRequiresShipping = false;
        foreach (var sci in details.Cart)
        {
            var p = await productService.GetProductByIdAsync(sci.ProductId);
            if (p is { IsShipEnabled: true }) { cartRequiresShipping = true; break; }
        }
        if (cartRequiresShipping)
        {
            await PrepareShippingDetailsAsync(details, request);
        }
        else
        {
            details.ShippingStatus = ShippingStatus.ShippingNotRequired;
        }

        // Shipping total
        var shipInclTax = await orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, true);
        var shipExclTax = await orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, false);
        if (!shipInclTax.ShippingTotal.HasValue || !shipExclTax.ShippingTotal.HasValue)
            throw new NopException("Shipping total couldn't be calculated");
        details.OrderShippingTotalInclTax = shipInclTax.ShippingTotal.Value;
        details.OrderShippingTotalExclTax = shipExclTax.ShippingTotal.Value;
        AddDiscounts(details.AppliedDiscounts, shipInclTax.AppliedDiscounts);

        // Payment additional fee
        var paymentFee = await paymentService.GetAdditionalHandlingFeeAsync(details.Cart,
            request.PaymentMethodSystemName ?? string.Empty);
        var (feeInclTax, _) = await taxService.GetPaymentMethodAdditionalFeeAsync(paymentFee, true, details.Customer);
        var (feeExclTax, _) = await taxService.GetPaymentMethodAdditionalFeeAsync(paymentFee, false, details.Customer);
        details.PaymentAdditionalFeeInclTax = feeInclTax;
        details.PaymentAdditionalFeeExclTax = feeExclTax;

        // Tax
        var (taxTotal, taxRatesDict) = await orderTotalCalculationService.GetTaxTotalAsync(details.Cart);
        details.OrderTaxTotal = taxTotal;

        // VAT
        var vatStatusId = await details.Customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.VatNumberStatusId, genericAttributeService);
        if (taxSettings.EuVatEnabled && (VatNumberStatus)vatStatusId == VatNumberStatus.Valid)
            details.VatNumber = await details.Customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.VatNumber, genericAttributeService);

        // Tax rates string
        details.TaxRates = string.Join("   ", taxRatesDict.Select(kvp =>
            $"{kvp.Key.ToString(CultureInfo.InvariantCulture)}:{kvp.Value.ToString(CultureInfo.InvariantCulture)};"));

        // Order total
        var cartTotal = await orderTotalCalculationService.GetShoppingCartTotalAsync(details.Cart)
            ?? throw new NopException("Order total couldn't be calculated");
        details.OrderDiscountAmount = cartTotal.DiscountAmount;
        details.RedeemedRewardPoints = cartTotal.RedeemedRewardPoints;
        details.RedeemedRewardPointsAmount = cartTotal.RedeemedRewardPointsAmount;
        details.AppliedGiftCards = cartTotal.AppliedGiftCards;
        details.OrderTotal = cartTotal.OrderTotal;
        AddDiscounts(details.AppliedDiscounts, cartTotal.AppliedDiscounts);
        request.OrderTotal = details.OrderTotal;

        // Recurring?
        var isRecurring = false;
        foreach (var sci in details.Cart)
        {
            var p = await productService.GetProductByIdAsync(sci.ProductId);
            if (p is { IsRecurring: true }) { isRecurring = true; break; }
        }
        details.IsRecurringShoppingCart = isRecurring;
        if (details.IsRecurringShoppingCart)
        {
            var cycleInfo = await shoppingCartService.GetRecurringCycleInfoAsync(details.Cart);
            if (!string.IsNullOrEmpty(cycleInfo.Error))
                throw new NopException(cycleInfo.Error);
            request.RecurringCycleLength = cycleInfo.CycleLength;
            request.RecurringCyclePeriod = cycleInfo.CyclePeriod;
            request.RecurringTotalCycles = cycleInfo.TotalCycles;
        }

        return details;
    }

    private async Task PrepareShippingDetailsAsync(PlaceOrderContainer details, ProcessPaymentRequest request)
    {
        var pickupPointJson = await details.Customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.SelectedPickupPoint, genericAttributeService, request.StoreId);
        if (shippingSettings.AllowPickUpInStore && !string.IsNullOrEmpty(pickupPointJson))
        {
            details.PickUpInStore = true;
            // Pickup address will be created by checkout controller and stored as PickupAddressId
            // For now, mark as pickup in store with no separate address
            details.ShippingStatus = ShippingStatus.NotYetShipped;
        }
        else
        {
            if (!details.Customer.ShippingAddressId.HasValue)
                throw new NopException("Shipping address is not provided");
            var shippingAddr = await addressService.GetAddressByIdAsync(details.Customer.ShippingAddressId.Value)
                ?? throw new NopException("Shipping address is not provided");
            var clonedShipping = CloneAddress(shippingAddr);
            await addressService.InsertAddressAsync(clonedShipping);
            details.ShippingAddressId = clonedShipping.Id;
            details.ShippingStatus = ShippingStatus.NotYetShipped;
        }

        var shippingOptionJson = await details.Customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.SelectedShippingOption, genericAttributeService, request.StoreId);
        if (!string.IsNullOrEmpty(shippingOptionJson))
        {
            // Parse shipping option name and system name from JSON stored in generic attribute
            // Legacy stored ShippingOption as XML; we store the name/system name directly
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(shippingOptionJson);
                details.ShippingMethodName = doc.RootElement.TryGetProperty("Name", out var nameProp)
                    ? nameProp.GetString() : null;
                details.ShippingRateComputationMethodSystemName = doc.RootElement.TryGetProperty(
                    "ShippingRateComputationMethodSystemName", out var sysProp) ? sysProp.GetString() : null;
            }
            catch
            {
                // Fallback: treat as plain text name
                details.ShippingMethodName = shippingOptionJson;
            }
        }
    }

    private static Address CloneAddress(Address source) => new()
    {
        FirstName = source.FirstName,
        LastName = source.LastName,
        Email = source.Email,
        Company = source.Company,
        CountryId = source.CountryId,
        StateProvinceId = source.StateProvinceId,
        City = source.City,
        Address1 = source.Address1,
        Address2 = source.Address2,
        ZipPostalCode = source.ZipPostalCode,
        PhoneNumber = source.PhoneNumber,
        FaxNumber = source.FaxNumber,
        CreatedOnUtc = DateTime.UtcNow,
    };

    private static void AddDiscounts(List<Discount> target, List<Discount> source)
    {
        foreach (var d in source)
            if (!target.Any(x => x.Id == d.Id))
                target.Add(d);
    }

    protected virtual async Task<Order> SaveOrderDetailsAsync(ProcessPaymentRequest request,
        ProcessPaymentResult paymentResult, PlaceOrderContainer details)
    {
        var order = new Order
        {
            StoreId = request.StoreId,
            OrderGuid = request.OrderGuid,
            CustomerId = details.Customer.Id,
            CustomerLanguageId = details.CustomerLanguage.Id,
            CustomerTaxDisplayType = details.CustomerTaxDisplayType,
            CustomerIp = webHelper.GetCurrentIpAddress(),
            OrderSubtotalInclTax = details.OrderSubTotalInclTax,
            OrderSubtotalExclTax = details.OrderSubTotalExclTax,
            OrderSubTotalDiscountInclTax = details.OrderSubTotalDiscountInclTax,
            OrderSubTotalDiscountExclTax = details.OrderSubTotalDiscountExclTax,
            OrderShippingInclTax = details.OrderShippingTotalInclTax,
            OrderShippingExclTax = details.OrderShippingTotalExclTax,
            PaymentMethodAdditionalFeeInclTax = details.PaymentAdditionalFeeInclTax,
            PaymentMethodAdditionalFeeExclTax = details.PaymentAdditionalFeeExclTax,
            TaxRates = details.TaxRates,
            OrderTax = details.OrderTaxTotal,
            OrderTotal = details.OrderTotal,
            RefundedAmount = decimal.Zero,
            OrderDiscount = details.OrderDiscountAmount,
            CheckoutAttributeDescription = details.CheckoutAttributeDescription,
            CheckoutAttributesXml = details.CheckoutAttributesXml,
            CustomerCurrencyCode = details.CustomerCurrencyCode,
            CurrencyRate = details.CustomerCurrencyRate,
            AffiliateId = details.AffiliateId,
            OrderStatus = OrderStatus.Pending,
            AllowStoringCreditCardNumber = paymentResult.AllowStoringCreditCardNumber,
            CardType = paymentResult.AllowStoringCreditCardNumber ? encryptionService.EncryptText(request.CreditCardType ?? string.Empty) : string.Empty,
            CardName = paymentResult.AllowStoringCreditCardNumber ? encryptionService.EncryptText(request.CreditCardName ?? string.Empty) : string.Empty,
            CardNumber = paymentResult.AllowStoringCreditCardNumber ? encryptionService.EncryptText(request.CreditCardNumber ?? string.Empty) : string.Empty,
            MaskedCreditCardNumber = encryptionService.EncryptText(paymentService.GetMaskedCreditCardNumber(request.CreditCardNumber ?? string.Empty)),
            CardCvv2 = paymentResult.AllowStoringCreditCardNumber ? encryptionService.EncryptText(request.CreditCardCvv2 ?? string.Empty) : string.Empty,
            CardExpirationMonth = paymentResult.AllowStoringCreditCardNumber ? encryptionService.EncryptText(request.CreditCardExpireMonth.ToString()) : string.Empty,
            CardExpirationYear = paymentResult.AllowStoringCreditCardNumber ? encryptionService.EncryptText(request.CreditCardExpireYear.ToString()) : string.Empty,
            PaymentMethodSystemName = request.PaymentMethodSystemName,
            AuthorizationTransactionId = paymentResult.AuthorizationTransactionId,
            AuthorizationTransactionCode = paymentResult.AuthorizationTransactionCode,
            AuthorizationTransactionResult = paymentResult.AuthorizationTransactionResult,
            CaptureTransactionId = paymentResult.CaptureTransactionId,
            CaptureTransactionResult = paymentResult.CaptureTransactionResult,
            SubscriptionTransactionId = paymentResult.SubscriptionTransactionId,
            PaymentStatus = paymentResult.NewPaymentStatus,
            PaidDateUtc = null,
            BillingAddressId = details.BillingAddressId,
            ShippingAddressId = details.ShippingAddressId,
            PickupAddressId = details.PickupAddressId,
            ShippingStatus = details.ShippingStatus,
            ShippingMethod = details.ShippingMethodName,
            PickUpInStore = details.PickUpInStore,
            ShippingRateComputationMethodSystemName = details.ShippingRateComputationMethodSystemName,
            CustomValuesXml = request.SerializeCustomValues(),
            VatNumber = details.VatNumber,
            CreatedOnUtc = DateTime.UtcNow,
            CustomOrderNumber = string.Empty
        };

        await orderService.InsertOrderAsync(order);

        order.CustomOrderNumber = customNumberFormatter.GenerateOrderCustomNumber(order);
        await orderService.UpdateOrderAsync(order);

        // Reward points redemption
        if (details.RedeemedRewardPointsAmount > decimal.Zero)
        {
            await rewardPointService.AddRewardPointsHistoryEntryAsync(details.Customer, -details.RedeemedRewardPoints,
                order.StoreId,
                string.Format(await localizationService.GetResourceAsync("RewardPoints.Message.RedeemedForOrder",
                    order.CustomerLanguageId), order.CustomOrderNumber),
                usedWithOrderId: order.Id, usedAmount: details.RedeemedRewardPointsAmount);
        }

        return order;
    }

    private async Task CreateOrderItemsAsync(Order order, PlaceOrderContainer details)
    {
        foreach (var sc in details.Cart)
        {
            var product = await productService.GetProductByIdAsync(sc.ProductId);
            if (product == null) continue;

            var scUnitPrice = await priceCalculationService.GetUnitPriceAsync(sc);
            var scSubTotal = await priceCalculationService.GetSubTotalAsync(sc, true);
            // Discount amount from price calculation — use the difference between no-discount and with-discount
            var scSubTotalNoDiscount = await priceCalculationService.GetSubTotalAsync(sc, false);
            var discountAmount = scSubTotalNoDiscount - scSubTotal;
            if (discountAmount < 0) discountAmount = 0;

            var (unitPriceInclTax, _) = await taxService.GetProductPriceAsync(product, scUnitPrice, true, details.Customer, false);
            var (unitPriceExclTax, _) = await taxService.GetProductPriceAsync(product, scUnitPrice, false, details.Customer, false);
            var (subTotalInclTax, _) = await taxService.GetProductPriceAsync(product, scSubTotal, true, details.Customer, false);
            var (subTotalExclTax, _) = await taxService.GetProductPriceAsync(product, scSubTotal, false, details.Customer, false);
            var (discInclTax, _) = await taxService.GetProductPriceAsync(product, discountAmount, true, details.Customer, false);
            var (discExclTax, _) = await taxService.GetProductPriceAsync(product, discountAmount, false, details.Customer, false);

            var attrDesc = await productAttributeFormatter.FormatAttributesAsync(product, sc.AttributesXml ?? string.Empty, details.Customer);

            var orderItem = new OrderItem
            {
                OrderItemGuid = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = sc.ProductId,
                UnitPriceInclTax = unitPriceInclTax,
                UnitPriceExclTax = unitPriceExclTax,
                PriceInclTax = subTotalInclTax,
                PriceExclTax = subTotalExclTax,
                OriginalProductCost = await priceCalculationService.GetProductCostAsync(product, sc.AttributesXml ?? string.Empty),
                AttributeDescription = attrDesc,
                AttributesXml = sc.AttributesXml,
                Quantity = sc.Quantity,
                DiscountAmountInclTax = discInclTax,
                DiscountAmountExclTax = discExclTax,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = 0,
                RentalStartDateUtc = sc.RentalStartDateUtc,
                RentalEndDateUtc = sc.RentalEndDateUtc
            };
            await orderService.InsertOrderItemAsync(orderItem);

            // Gift cards
            if (product.IsGiftCard)
            {
                productAttributeParser.GetGiftCardAttribute(sc.AttributesXml ?? string.Empty,
                    out var recipientName, out var recipientEmail, out var senderName, out var senderEmail, out var giftCardMessage);
                for (var i = 0; i < sc.Quantity; i++)
                {
                    await giftCardService.InsertGiftCardAsync(new GiftCard
                    {
                        GiftCardTypeId = product.GiftCardTypeId,
                        PurchasedWithOrderItemId = orderItem.Id,
                        Amount = product.OverriddenGiftCardAmount ?? unitPriceExclTax,
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
            await productService.AdjustInventoryAsync(product, -sc.Quantity, sc.AttributesXml ?? string.Empty,
                string.Format(await localizationService.GetResourceAsync("Admin.StockQuantityHistory.Messages.PlaceOrder"), order.Id));
        }
    }

    protected virtual async Task SendNotificationsAndSaveNotesAsync(Order order)
    {
        // Impersonation note
        var noteText = workContext.OriginalCustomerIfImpersonated != null
            ? $"Order placed by a store owner ('{workContext.OriginalCustomerIfImpersonated.Email}'. ID = {workContext.OriginalCustomerIfImpersonated.Id}) impersonating the customer."
            : "Order placed";
        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = noteText,
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });

        // Store owner notification
        await workflowMessageService.SendOrderPlacedStoreOwnerNotificationAsync(order, localizationSettings.DefaultAdminLanguageId);

        // Customer notification
        await workflowMessageService.SendOrderPlacedCustomerNotificationAsync(order, order.CustomerLanguageId);

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
                    await workflowMessageService.SendOrderPlacedVendorNotificationAsync(order, vendor, localizationSettings.DefaultAdminLanguageId);
            }
        }
    }
}
