using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public partial class ShoppingCartService
{
    public virtual async Task<IList<string>> GetShoppingCartItemAttributeWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product,
        int quantity = 1, string? attributesXml = "",
        bool ignoreNonCombinableAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(product);
        var warnings = new List<string>();
        var xml = attributesXml ?? string.Empty;

        var attributes1 = await productAttributeParser.ParseProductAttributeMappingsAsync(xml);
        if (ignoreNonCombinableAttributes)
            attributes1 = attributes1.Where(x => ShouldHaveValues(x.AttributeControlTypeId)).ToList();
        foreach (var attr in attributes1)
        {
            if (attr.ProductId != product.Id)
            {
                warnings.Add("Attribute error");
                return warnings;
            }
        }

        var attributes2 = await productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
        if (ignoreNonCombinableAttributes)
            attributes2 = attributes2.Where(x => ShouldHaveValues(x.AttributeControlTypeId)).ToList();
        var condFiltered = new List<ProductAttributeMapping>();
        foreach (var a in attributes2)
        {
            var conditionMet = await productAttributeParser.IsConditionMetAsync(a, xml);
            if (!conditionMet.HasValue || conditionMet.Value)
                condFiltered.Add(a);
        }
        attributes2 = condFiltered;

        foreach (var a2 in attributes2)
        {
            if (a2.IsRequired)
            {
                bool found = attributes1.Where(a1 => a1.Id == a2.Id)
                    .SelectMany(a1 => productAttributeParser.ParseValues(xml, a1.Id))
                    .Any(v => !string.IsNullOrWhiteSpace(v));

                if (!found)
                {
                    var pa = await productAttributeService.GetProductAttributeByIdAsync(a2.ProductAttributeId);
                    warnings.Add(!string.IsNullOrEmpty(a2.TextPrompt)
                        ? a2.TextPrompt
                        : string.Format(await localizationService.GetResourceAsync("ShoppingCart.SelectAttribute"),
                            pa?.Name ?? string.Empty));
                }
            }

            if (a2.AttributeControlType == AttributeControlType.ReadonlyCheckboxes)
            {
                var allowedValues = (await productAttributeService.GetProductAttributeValuesAsync(a2.Id))
                    .Where(x => x.IsPreSelected).Select(x => x.Id).ToArray();
                var selectedValues = (await productAttributeParser.ParseProductAttributeValuesAsync(xml, a2.Id))
                    .Select(x => x.Id).ToArray();
                if (!allowedValues.SequenceEqual(selectedValues))
                    warnings.Add("You cannot change read-only values");
            }
        }

        foreach (var pam in attributes2)
        {
            if (pam.AttributeControlType != AttributeControlType.TextBox &&
                pam.AttributeControlType != AttributeControlType.MultilineTextbox)
                continue;

            var enteredText = productAttributeParser.ParseValues(xml, pam.Id).FirstOrDefault() ?? string.Empty;

            if (pam.ValidationMinLength.HasValue && pam.ValidationMinLength.Value > enteredText.Length)
            {
                var pa = await productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);
                warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.TextboxMinimumLength"),
                    pa?.Name ?? string.Empty, pam.ValidationMinLength.Value));
            }
            if (pam.ValidationMaxLength.HasValue && pam.ValidationMaxLength.Value < enteredText.Length)
            {
                var pa = await productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);
                warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.TextboxMaximumLength"),
                    pa?.Name ?? string.Empty, pam.ValidationMaxLength.Value));
            }
        }

        if (warnings.Any()) return warnings;

        var attributeValues = await productAttributeParser.ParseProductAttributeValuesAsync(xml);
        foreach (var av in attributeValues)
        {
            if (av.AttributeValueType != AttributeValueType.AssociatedToProduct) continue;
            if (ignoreNonCombinableAttributes)
            {
                var mapping = await productAttributeService.GetProductAttributeMappingByIdAsync(av.ProductAttributeMappingId);
                if (mapping != null && !ShouldHaveValues(mapping.AttributeControlTypeId)) continue;
            }
            var associatedProduct = await productService.GetProductByIdAsync(av.AssociatedProductId);
            if (associatedProduct != null)
            {
                var assocWarnings = await GetShoppingCartItemWarningsAsync(customer,
                    shoppingCartType, associatedProduct, storeContext.CurrentStore.Id,
                    "", decimal.Zero, null, null, quantity * av.Quantity, false);
                foreach (var w in assocWarnings)
                {
                    var mapping = await productAttributeService.GetProductAttributeMappingByIdAsync(av.ProductAttributeMappingId);
                    var pa = mapping != null ? await productAttributeService.GetProductAttributeByIdAsync(mapping.ProductAttributeId) : null;
                    warnings.Add(string.Format(
                        await localizationService.GetResourceAsync("ShoppingCart.AssociatedAttributeWarning"),
                        pa?.Name ?? string.Empty, av.Name ?? string.Empty, w));
                }
            }
            else
                warnings.Add($"Associated product cannot be loaded - {av.AssociatedProductId}");
        }
        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartItemGiftCardWarningsAsync(
        ShoppingCartType shoppingCartType, Product product, string? attributesXml)
    {
        ArgumentNullException.ThrowIfNull(product);
        var warnings = new List<string>();
        if (!product.IsGiftCard) return warnings;

        productAttributeParser.GetGiftCardAttribute(attributesXml ?? string.Empty,
            out var recipientName, out var recipientEmail,
            out var senderName, out var senderEmail, out _);

        if (string.IsNullOrEmpty(recipientName))
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.RecipientNameError"));
        if (product.GiftCardType == GiftCardType.Virtual &&
            (string.IsNullOrEmpty(recipientEmail) || !CommonHelper.IsValidEmail(recipientEmail)))
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.RecipientEmailError"));
        if (string.IsNullOrEmpty(senderName))
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.SenderNameError"));
        if (product.GiftCardType == GiftCardType.Virtual &&
            (string.IsNullOrEmpty(senderEmail) || !CommonHelper.IsValidEmail(senderEmail)))
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.SenderEmailError"));
        return warnings;
    }

    public virtual async Task<IList<string>> GetRentalProductWarningsAsync(Product product,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null)
    {
        ArgumentNullException.ThrowIfNull(product);
        var warnings = new List<string>();
        if (!product.IsRental) return warnings;

        if (!rentalStartDate.HasValue)
        {
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.Rental.EnterStartDate"));
            return warnings;
        }
        if (!rentalEndDate.HasValue)
        {
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.Rental.EnterEndDate"));
            return warnings;
        }
        if (rentalStartDate.Value > rentalEndDate.Value)
        {
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.Rental.StartDateLessEndDate"));
            return warnings;
        }

        var nowInStoreTimeZone = dateTimeHelper.ConvertToUserTime(DateTime.Now,
            TimeZoneInfo.Local, dateTimeHelper.DefaultStoreTimeZone);
        var todayDt = new DateTime(nowInStoreTimeZone.Year, nowInStoreTimeZone.Month, nowInStoreTimeZone.Day);
        var todayUtc = dateTimeHelper.ConvertToUtcTime(todayDt, dateTimeHelper.DefaultStoreTimeZone);
        var startDateUtc = dateTimeHelper.ConvertToUtcTime(rentalStartDate.Value, dateTimeHelper.DefaultStoreTimeZone);
        if (todayUtc > startDateUtc)
        {
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.Rental.StartDateShouldBeFuture"));
            return warnings;
        }
        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartItemWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product, int storeId,
        string? attributesXml, decimal customerEnteredPrice,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true,
        bool getStandardWarnings = true, bool getAttributesWarnings = true,
        bool getGiftCardWarnings = true, bool getRequiredProductWarnings = true,
        bool getRentalWarnings = true)
    {
        ArgumentNullException.ThrowIfNull(product);
        var warnings = new List<string>();
        if (getStandardWarnings)
            warnings.AddRange(await GetStandardWarningsAsync(customer, shoppingCartType, product, attributesXml, customerEnteredPrice, quantity));
        if (getAttributesWarnings)
            warnings.AddRange(await GetShoppingCartItemAttributeWarningsAsync(customer, shoppingCartType, product, quantity, attributesXml));
        if (getGiftCardWarnings)
            warnings.AddRange(await GetShoppingCartItemGiftCardWarningsAsync(shoppingCartType, product, attributesXml));
        if (getRequiredProductWarnings)
            warnings.AddRange(await GetRequiredProductWarningsAsync(customer, shoppingCartType, product, storeId, automaticallyAddRequiredProductsIfEnabled));
        if (getRentalWarnings)
            warnings.AddRange(await GetRentalProductWarningsAsync(product, rentalStartDate, rentalEndDate));
        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartWarningsAsync(IList<ShoppingCartItem> shoppingCart,
        string? checkoutAttributesXml, bool validateCheckoutAttributes)
    {
        var warnings = new List<string>();
        bool hasStandard = false, hasRecurring = false;

        foreach (var sci in shoppingCart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null)
            {
                warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.CannotLoadProduct"), sci.ProductId));
                return warnings;
            }
            if (product.IsRecurring) hasRecurring = true;
            else hasStandard = true;
        }

        if (hasStandard && hasRecurring)
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.CannotMixStandardAndAutoshipProducts"));

        if (hasRecurring)
        {
            var cyclesError = await GetRecurringCycleInfoAsync(shoppingCart);
            if (!string.IsNullOrEmpty(cyclesError))
            {
                warnings.Add(cyclesError);
                return warnings;
            }
        }

        if (validateCheckoutAttributes)
            await ValidateCheckoutAttributesAsync(shoppingCart, checkoutAttributesXml ?? string.Empty, warnings);

        return warnings;
    }

    private async Task<string?> GetRecurringCycleInfoAsync(IList<ShoppingCartItem> cart)
    {
        int? cycleLength = null;
        RecurringProductCyclePeriod? cyclePeriod = null;
        int? totalCycles = null;

        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null || !product.IsRecurring) continue;

            if (cycleLength.HasValue)
            {
                if (cycleLength.Value != product.RecurringCycleLength ||
                    cyclePeriod!.Value != product.RecurringCyclePeriod ||
                    totalCycles!.Value != product.RecurringTotalCycles)
                    return await localizationService.GetResourceAsync("ShoppingCart.ConflictingShipmentSchedules");
            }
            else
            {
                cycleLength = product.RecurringCycleLength;
                cyclePeriod = product.RecurringCyclePeriod;
                totalCycles = product.RecurringTotalCycles;
            }
        }
        return null;
    }

    private async Task ValidateCheckoutAttributesAsync(IList<ShoppingCartItem> cart,
        string checkoutAttributesXml, List<string> warnings)
    {
        var selected = await checkoutAttributeParser.ParseCheckoutAttributesAsync(checkoutAttributesXml);

        bool requiresShipping = false;
        foreach (var sci in cart)
        {
            var p = await productService.GetProductByIdAsync(sci.ProductId);
            if (p != null && p.IsShipEnabled) { requiresShipping = true; break; }
        }

        var all = await checkoutAttributeService.GetAllCheckoutAttributesAsync(storeContext.CurrentStore.Id, !requiresShipping);
        var filtered = new List<CheckoutAttribute>();
        foreach (var a in all)
        {
            var conditionMet = await checkoutAttributeParser.IsConditionMetAsync(a, checkoutAttributesXml);
            if (!conditionMet.HasValue || conditionMet.Value)
                filtered.Add(a);
        }

        foreach (var a2 in filtered)
        {
            if (a2.IsRequired)
            {
                bool found = selected.Where(a1 => a1.Id == a2.Id)
                    .SelectMany(a1 => checkoutAttributeParser.ParseValues(checkoutAttributesXml, a1.Id))
                    .Any(v => !string.IsNullOrWhiteSpace(v));

                if (!found)
                    warnings.Add(!string.IsNullOrEmpty(a2.TextPrompt)
                        ? a2.TextPrompt
                        : string.Format(await localizationService.GetResourceAsync("ShoppingCart.SelectAttribute"), a2.Name ?? string.Empty));
            }

            if (a2.AttributeControlType is AttributeControlType.TextBox or AttributeControlType.MultilineTextbox)
            {
                var text = checkoutAttributeParser.ParseValues(checkoutAttributesXml, a2.Id).FirstOrDefault() ?? string.Empty;
                if (a2.ValidationMinLength.HasValue && a2.ValidationMinLength.Value > text.Length)
                    warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.TextboxMinimumLength"),
                        a2.Name ?? string.Empty, a2.ValidationMinLength.Value));
                if (a2.ValidationMaxLength.HasValue && a2.ValidationMaxLength.Value < text.Length)
                    warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.TextboxMaximumLength"),
                        a2.Name ?? string.Empty, a2.ValidationMaxLength.Value));
            }
        }
    }
}
