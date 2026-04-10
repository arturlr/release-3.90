using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Controllers;

public partial class ShoppingCartController
{
    // --- Model preparation ---

    private async Task<ShoppingCartModel> PrepareShoppingCartModelAsync(
        IList<ShoppingCartItem> cart, bool isEditable,
        ShoppingCartModel.DiscountBoxModel? discountBox = null,
        ShoppingCartModel.GiftCardBoxModel? giftCardBox = null)
    {
        var customer = workContext.CurrentCustomer;
        var model = new ShoppingCartModel
        {
            IsEditable = isEditable,
            ShowSku = catalogSettings.ShowSkuOnProductDetailsPage,
            ShowProductImages = shoppingCartSettings.ShowProductImagesOnShoppingCart,
            TermsOfServiceOnShoppingCartPage = orderSettings.TermsOfServiceOnShoppingCartPage,
            OnePageCheckoutEnabled = orderSettings.OnePageCheckoutEnabled,
            DiscountBox = discountBox ?? new ShoppingCartModel.DiscountBoxModel { Display = shoppingCartSettings.ShowDiscountBox },
            GiftCardBox = giftCardBox ?? new ShoppingCartModel.GiftCardBoxModel { Display = shoppingCartSettings.ShowGiftCardBox }
        };

        if (discountBox == null)
            model.DiscountBox.Display = shoppingCartSettings.ShowDiscountBox;

        // Applied discount codes
        if (model.DiscountBox.Display)
        {
            var discountCodes = await GetAppliedDiscountCouponCodesAsync(customer);
            foreach (var code in discountCodes)
            {
                var discounts = (await discountService.GetAllDiscountsAsync(couponCode: code))
                    .Where(d => d.RequiresCouponCode).ToList();
                foreach (var d in discounts)
                    model.DiscountBox.AppliedDiscountsWithCodes.Add(new ShoppingCartModel.DiscountBoxModel.DiscountInfoModel { Id = d.Id, CouponCode = d.CouponCode });
            }
        }

        // Cart items
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null) continue;

            var itemModel = new ShoppingCartModel.ShoppingCartItemModel
            {
                Id = sci.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSeName = SeoExtensions.GetSeName(product.Name ?? "", false, false),
                Sku = product.Sku,
                Quantity = sci.Quantity,
                AllowItemEditing = isEditable,
                AttributeInfo = await productAttributeFormatter.FormatAttributesAsync(product, sci.AttributesXml ?? "")
            };

            // Pricing
            var (unitPrice, _) = await taxService.GetProductPriceAsync(product,
                await priceCalculationService.GetUnitPriceAsync(sci));
            itemModel.UnitPrice = await priceFormatter.FormatPriceAsync(unitPrice);

            var (subTotal, _) = await taxService.GetProductPriceAsync(product,
                await priceCalculationService.GetSubTotalAsync(sci));
            itemModel.SubTotal = await priceFormatter.FormatPriceAsync(subTotal);

            // Warnings
            var warnings = await shoppingCartService.GetShoppingCartItemWarningsAsync(customer,
                sci.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart ? ShoppingCartType.ShoppingCart : ShoppingCartType.Wishlist,
                product, storeContext.CurrentStore.Id, sci.AttributesXml, sci.CustomerEnteredPrice,
                sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false);
            itemModel.Warnings.AddRange(warnings);

            model.Items.Add(itemModel);
        }

        // Cart-level warnings
        var checkoutAttributesXml = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, storeContext.CurrentStore.Id);
        var cartWarnings = await shoppingCartService.GetShoppingCartWarningsAsync(cart, checkoutAttributesXml, false);
        model.Warnings.AddRange(cartWarnings);

        return model;
    }

    private async Task<WishlistModel> PrepareWishlistModelAsync(IList<ShoppingCartItem> cart, bool isEditable)
    {
        var customer = workContext.CurrentCustomer;
        var model = new WishlistModel
        {
            IsEditable = isEditable,
            EmailWishlistEnabled = shoppingCartSettings.EmailWishlistEnabled,
            ShowSku = catalogSettings.ShowSkuOnProductDetailsPage,
            ShowProductImages = shoppingCartSettings.ShowProductImagesOnWishList,
            DisplayAddToCart = permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart),
            CustomerGuid = customer.CustomerGuid
        };

        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null) continue;

            var itemModel = new WishlistModel.ShoppingCartItemModel
            {
                Id = sci.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSeName = SeoExtensions.GetSeName(product.Name ?? "", false, false),
                Sku = product.Sku,
                Quantity = sci.Quantity,
                AllowItemEditing = isEditable,
                AttributeInfo = await productAttributeFormatter.FormatAttributesAsync(product, sci.AttributesXml ?? "")
            };

            var (unitPrice, _) = await taxService.GetProductPriceAsync(product,
                await priceCalculationService.GetUnitPriceAsync(sci));
            itemModel.UnitPrice = await priceFormatter.FormatPriceAsync(unitPrice);

            var (subTotal, _) = await taxService.GetProductPriceAsync(product,
                await priceCalculationService.GetSubTotalAsync(sci));
            itemModel.SubTotal = await priceFormatter.FormatPriceAsync(subTotal);

            model.Items.Add(itemModel);
        }

        return model;
    }

    private async Task<MiniShoppingCartModel> PrepareMiniShoppingCartModelAsync()
    {
        var customer = workContext.CurrentCustomer;
        var storeId = storeContext.CurrentStore.Id;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);

        var model = new MiniShoppingCartModel
        {
            ShowProductImages = shoppingCartSettings.ShowProductImagesInMiniShoppingCart,
            DisplayShoppingCartButton = true,
            DisplayCheckoutButton = true,
            CurrentCustomerIsGuest = !await IsRegisteredAsync(customer),
            AnonymousCheckoutAllowed = orderSettings.AnonymousCheckoutAllowed,
            TotalProducts = cart.Sum(x => x.Quantity)
        };

        if (cart.Count != 0)
        {
            var subTotal = await orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, false);
            model.SubTotal = await priceFormatter.FormatPriceAsync(subTotal.SubTotalWithoutDiscount);
        }

        var itemsToShow = shoppingCartSettings.MiniShoppingCartProductNumber;
        foreach (var sci in cart.Take(itemsToShow))
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null) continue;

            model.Items.Add(new MiniShoppingCartModel.ShoppingCartItemModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSeName = SeoExtensions.GetSeName(product.Name ?? "", false, false),
                Quantity = sci.Quantity,
                UnitPrice = await priceFormatter.FormatPriceAsync(
                    (await taxService.GetProductPriceAsync(product, await priceCalculationService.GetUnitPriceAsync(sci))).price),
                AttributeInfo = await productAttributeFormatter.FormatAttributesAsync(product, sci.AttributesXml ?? "")
            });
        }

        return model;
    }

    private async Task<OrderTotalsModel> PrepareOrderTotalsModelAsync(IList<ShoppingCartItem> cart, bool isEditable)
    {
        var customer = workContext.CurrentCustomer;
        var model = new OrderTotalsModel { IsEditable = isEditable };

        var subTotalResult = await orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, false);
        model.SubTotal = await priceFormatter.FormatPriceAsync(subTotalResult.SubTotalWithoutDiscount);
        if (subTotalResult.DiscountAmount > 0)
            model.SubTotalDiscount = await priceFormatter.FormatPriceAsync(subTotalResult.DiscountAmount);

        // Shipping
        var requiresShipping = cart.Any(sci =>
        {
            var p = productService.GetProductByIdAsync(sci.ProductId).GetAwaiter().GetResult();
            return p?.IsShipEnabled == true;
        });
        model.RequiresShipping = requiresShipping;
        if (requiresShipping)
        {
            var shippingTotal = await orderTotalCalculationService.GetShoppingCartShippingTotalAsync(cart, false);
            if (shippingTotal.ShippingTotal.HasValue)
            {
                model.Shipping = await priceFormatter.FormatPriceAsync(shippingTotal.ShippingTotal.Value);
                model.SelectedShippingMethod = (await customer.GetAttributeAsync<string>(
                    SystemCustomerAttributeNames.SelectedShippingOption, genericAttributeService, storeContext.CurrentStore.Id)) ?? "";
            }
            else
            {
                model.HideShippingTotal = true;
            }
        }

        // Tax
        var taxTotal = await orderTotalCalculationService.GetTaxTotalAsync(cart);
        model.Tax = await priceFormatter.FormatPriceAsync(taxTotal.TaxTotal);

        // Order total
        var cartTotal = await orderTotalCalculationService.GetShoppingCartTotalAsync(cart);
        if (cartTotal != null)
            model.OrderTotal = await priceFormatter.FormatPriceAsync(cartTotal.OrderTotal);

        // Gift cards
        if (cartTotal?.AppliedGiftCards != null)
        {
            foreach (var gc in cartTotal.AppliedGiftCards)
            {
                model.GiftCards.Add(new OrderTotalsModel.GiftCard
                {
                    Id = gc.GiftCard.Id,
                    CouponCode = gc.GiftCard.GiftCardCouponCode,
                    Amount = await priceFormatter.FormatPriceAsync(-gc.AmountCanBeUsed),
                    Remaining = await priceFormatter.FormatPriceAsync(await giftCardService.GetGiftCardRemainingAmountAsync(gc.GiftCard) - gc.AmountCanBeUsed)
                });
            }
        }

        // Reward points
        if (cartTotal is { RedeemedRewardPoints: > 0 })
        {
            model.RedeemedRewardPoints = cartTotal.RedeemedRewardPoints;
            model.RedeemedRewardPointsAmount = await priceFormatter.FormatPriceAsync(-cartTotal.RedeemedRewardPointsAmount);
        }

        return model;
    }

    // --- Parsing helpers ---

    private async Task ParseAndSaveCheckoutAttributesAsync(IList<ShoppingCartItem> cart, IFormCollection form)
    {
        var attributesXml = "";
        var requiresShipping = false;
        foreach (var sci in cart)
        {
            var p = await productService.GetProductByIdAsync(sci.ProductId);
            if (p?.IsShipEnabled == true) { requiresShipping = true; break; }
        }

        var checkoutAttributes = await checkoutAttributeService.GetAllCheckoutAttributesAsync(storeContext.CurrentStore.Id, !requiresShipping);
        foreach (var attribute in checkoutAttributes)
        {
            var controlId = $"checkout_attribute_{attribute.Id}";
            switch ((AttributeControlType)attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                    if (form.ContainsKey(controlId) && int.TryParse(form[controlId], out var selectedId) && selectedId > 0)
                        attributesXml = checkoutAttributeParser.AddCheckoutAttribute(attributesXml, attribute, selectedId.ToString());
                    break;
                case AttributeControlType.Checkboxes:
                    if (form.ContainsKey(controlId))
                        foreach (var item in form[controlId].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                            if (int.TryParse(item, out var cbId) && cbId > 0)
                                attributesXml = checkoutAttributeParser.AddCheckoutAttribute(attributesXml, attribute, cbId.ToString());
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                    var roValues = await checkoutAttributeService.GetCheckoutAttributeValuesAsync(attribute.Id);
                    foreach (var v in roValues.Where(v => v.IsPreSelected))
                        attributesXml = checkoutAttributeParser.AddCheckoutAttribute(attributesXml, attribute, v.Id.ToString());
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                    if (form.ContainsKey(controlId))
                    {
                        var text = form[controlId].ToString().Trim();
                        if (!string.IsNullOrEmpty(text))
                            attributesXml = checkoutAttributeParser.AddCheckoutAttribute(attributesXml, attribute, text);
                    }
                    break;
                case AttributeControlType.Datepicker:
                    if (form.ContainsKey($"{controlId}_day") && form.ContainsKey($"{controlId}_month") && form.ContainsKey($"{controlId}_year"))
                    {
                        try
                        {
                            var date = new DateTime(int.Parse(form[$"{controlId}_year"]!), int.Parse(form[$"{controlId}_month"]!), int.Parse(form[$"{controlId}_day"]!));
                            attributesXml = checkoutAttributeParser.AddCheckoutAttribute(attributesXml, attribute, date.ToString("D"));
                        }
                        catch { /* invalid date — skip */ }
                    }
                    break;
                case AttributeControlType.FileUpload:
                    if (form.ContainsKey(controlId) && Guid.TryParse(form[controlId], out var downloadGuid))
                    {
                        var download = await downloadService.GetDownloadByGuidAsync(downloadGuid);
                        if (download != null)
                            attributesXml = checkoutAttributeParser.AddCheckoutAttribute(attributesXml, attribute, download.DownloadGuid.ToString());
                    }
                    break;
            }
        }

        // Validate conditional attributes
        foreach (var attribute in checkoutAttributes)
        {
            var conditionMet = await checkoutAttributeParser.IsConditionMetAsync(attribute, attributesXml);
            if (conditionMet.HasValue && !conditionMet.Value)
                attributesXml = checkoutAttributeParser.RemoveCheckoutAttribute(attributesXml, attribute);
        }

        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.CheckoutAttributes, attributesXml, storeContext.CurrentStore.Id);
    }

    private async Task<(string attributesXml, List<string> errors)> ParseProductAttributesAsync(Product product, IFormCollection form)
    {
        var attributesXml = "";
        var errors = new List<string>();
        var productAttributes = await productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);

        foreach (var attribute in productAttributes)
        {
            var controlId = $"product_attribute_{attribute.Id}";
            switch ((AttributeControlType)attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                    if (form.ContainsKey(controlId) && int.TryParse(form[controlId], out var selectedId) && selectedId > 0)
                        attributesXml = productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedId.ToString());
                    break;
                case AttributeControlType.Checkboxes:
                    if (form.ContainsKey(controlId))
                        foreach (var item in form[controlId].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                            if (int.TryParse(item, out var cbId) && cbId > 0)
                                attributesXml = productAttributeParser.AddProductAttribute(attributesXml, attribute, cbId.ToString());
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                    var roValues = await productAttributeService.GetProductAttributeValuesAsync(attribute.Id);
                    foreach (var v in roValues.Where(v => v.IsPreSelected))
                        attributesXml = productAttributeParser.AddProductAttribute(attributesXml, attribute, v.Id.ToString());
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                    if (form.ContainsKey(controlId))
                    {
                        var text = form[controlId].ToString().Trim();
                        if (!string.IsNullOrEmpty(text))
                            attributesXml = productAttributeParser.AddProductAttribute(attributesXml, attribute, text);
                    }
                    break;
                case AttributeControlType.Datepicker:
                    if (form.ContainsKey($"{controlId}_day") && form.ContainsKey($"{controlId}_month") && form.ContainsKey($"{controlId}_year"))
                    {
                        try
                        {
                            var date = new DateTime(int.Parse(form[$"{controlId}_year"]!), int.Parse(form[$"{controlId}_month"]!), int.Parse(form[$"{controlId}_day"]!));
                            attributesXml = productAttributeParser.AddProductAttribute(attributesXml, attribute, date.ToString("D"));
                        }
                        catch { /* invalid date — skip */ }
                    }
                    break;
                case AttributeControlType.FileUpload:
                    if (form.ContainsKey(controlId) && Guid.TryParse(form[controlId], out var downloadGuid))
                    {
                        var download = await downloadService.GetDownloadByGuidAsync(downloadGuid);
                        if (download != null)
                            attributesXml = productAttributeParser.AddProductAttribute(attributesXml, attribute, download.DownloadGuid.ToString());
                    }
                    break;
            }
        }

        // Conditional attributes
        foreach (var attribute in productAttributes)
        {
            var conditionMet = await productAttributeParser.IsConditionMetAsync(attribute, attributesXml);
            if (conditionMet.HasValue && !conditionMet.Value)
                attributesXml = productAttributeParser.RemoveProductAttribute(attributesXml, attribute);
        }

        // Gift card attributes
        if (product.IsGiftCard)
        {
            var recipientName = form[$"giftcard_{product.Id}.RecipientName"].ToString();
            var recipientEmail = form[$"giftcard_{product.Id}.RecipientEmail"].ToString();
            var senderName = form[$"giftcard_{product.Id}.SenderName"].ToString();
            var senderEmail = form[$"giftcard_{product.Id}.SenderEmail"].ToString();
            var giftCardMessage = form[$"giftcard_{product.Id}.Message"].ToString();
            attributesXml = productAttributeParser.AddGiftCardAttribute(attributesXml,
                recipientName, recipientEmail, senderName, senderEmail, giftCardMessage);
        }

        return (attributesXml, errors);
    }

    private static void ParseRentalDates(Product product, IFormCollection form, out DateTime? startDate, out DateTime? endDate)
    {
        startDate = null;
        endDate = null;
        const string datePickerFormat = "MM/dd/yyyy";
        try
        {
            if (form.ContainsKey($"rental_start_date_{product.Id}") && form.ContainsKey($"rental_end_date_{product.Id}"))
            {
                startDate = DateTime.ParseExact(form[$"rental_start_date_{product.Id}"]!, datePickerFormat, System.Globalization.CultureInfo.InvariantCulture);
                endDate = DateTime.ParseExact(form[$"rental_end_date_{product.Id}"]!, datePickerFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch { /* invalid dates — leave null */ }
    }

    // --- Coupon code helpers (GenericAttribute-based) ---

    private async Task<IList<string>> GetAppliedDiscountCouponCodesAsync(Customer customer)
    {
        var codes = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.DiscountCouponCode, genericAttributeService);
        return string.IsNullOrEmpty(codes) ? [] : codes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private async Task ApplyDiscountCouponCodeAsync(Customer customer, string couponCode)
    {
        var existing = await GetAppliedDiscountCouponCodesAsync(customer);
        if (!existing.Contains(couponCode, StringComparer.OrdinalIgnoreCase))
        {
            var updated = existing.Count != 0 ? string.Join(",", existing) + "," + couponCode : couponCode;
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.DiscountCouponCode, updated);
        }
    }

    private async Task RemoveDiscountCouponCodeAsync(Customer customer, string? couponCode)
    {
        if (string.IsNullOrEmpty(couponCode)) return;
        var existing = await GetAppliedDiscountCouponCodesAsync(customer);
        var updated = existing.Where(c => !c.Equals(couponCode, StringComparison.OrdinalIgnoreCase)).ToList();
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.DiscountCouponCode,
            updated.Count != 0 ? string.Join(",", updated) : "");
    }

    private async Task ApplyGiftCardCouponCodeAsync(Customer customer, string couponCode)
    {
        var existing = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.GiftCardCouponCodes, genericAttributeService);
        // Gift card codes stored as comma-separated
        var codes = string.IsNullOrEmpty(existing) ? [] : existing.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (!codes.Contains(couponCode, StringComparer.OrdinalIgnoreCase))
        {
            codes.Add(couponCode);
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.GiftCardCouponCodes, string.Join(",", codes));
        }
    }

    private async Task RemoveGiftCardCouponCodeAsync(Customer customer, string? couponCode)
    {
        if (string.IsNullOrEmpty(couponCode)) return;
        var existing = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.GiftCardCouponCodes, genericAttributeService);
        var codes = string.IsNullOrEmpty(existing) ? [] : existing.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        codes.RemoveAll(c => c.Equals(couponCode, StringComparison.OrdinalIgnoreCase));
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.GiftCardCouponCodes,
            codes.Count != 0 ? string.Join(",", codes) : "");
    }

    private async Task<bool> IsGiftCardValidAsync(Nop.Core.Domain.Orders.GiftCard giftCard)
    {
        if (!giftCard.IsGiftCardActivated) return false;
        var remaining = await giftCardService.GetGiftCardRemainingAmountAsync(giftCard);
        return remaining > 0;
    }

    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        if (registeredRole == null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(registeredRole.Id);
    }
}
