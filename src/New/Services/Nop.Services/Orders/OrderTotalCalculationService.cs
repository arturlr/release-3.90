using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Payments;
using Nop.Services.Tax;

namespace Nop.Services.Orders;

public partial class OrderTotalCalculationService(
    IWorkContext workContext,
    IStoreContext storeContext,
    IPriceCalculationService priceCalculationService,
    ITaxService taxService,
    IPaymentService paymentService,
    ICheckoutAttributeParser checkoutAttributeParser,
    IDiscountService discountService,
    IGiftCardService giftCardService,
    IGenericAttributeService genericAttributeService,
    IRewardPointService rewardPointService,
    IProductService productService,
    ICustomerService customerService,
    TaxSettings taxSettings,
    RewardPointsSettings rewardPointsSettings,
    ShippingSettings shippingSettings,
    ShoppingCartSettings shoppingCartSettings,
    CatalogSettings catalogSettings) : IOrderTotalCalculationService
{
    #region Utilities

    protected virtual async Task<(decimal DiscountAmount, List<Discount> AppliedDiscounts)>
        GetOrderSubtotalDiscountAsync(Customer customer, decimal orderSubTotal)
    {
        if (catalogSettings.IgnoreDiscounts)
            return (0m, []);

        var allDiscounts = await discountService.GetAllDiscountsAsync(DiscountType.AssignedToOrderSubTotal, showHidden: false);
        var allowed = new List<Discount>();
        foreach (var d in allDiscounts)
        {
            var result = await discountService.ValidateDiscountAsync(d, customer);
            if (result.IsValid && !allowed.ContainsDiscount(d))
                allowed.Add(d);
        }

        var appliedDiscounts = allowed.GetPreferredDiscount(orderSubTotal, out var discountAmount);
        return (Math.Max(discountAmount, 0m), appliedDiscounts);
    }

    protected virtual async Task<(decimal DiscountAmount, List<Discount> AppliedDiscounts)>
        GetShippingDiscountAsync(Customer customer, decimal shippingTotal)
    {
        if (catalogSettings.IgnoreDiscounts)
            return (0m, []);

        var allDiscounts = await discountService.GetAllDiscountsAsync(DiscountType.AssignedToShipping, showHidden: false);
        var allowed = new List<Discount>();
        foreach (var d in allDiscounts)
        {
            var result = await discountService.ValidateDiscountAsync(d, customer);
            if (result.IsValid && !allowed.ContainsDiscount(d))
                allowed.Add(d);
        }

        var appliedDiscounts = allowed.GetPreferredDiscount(shippingTotal, out var discountAmount);
        if (discountAmount < 0m) discountAmount = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            discountAmount = Math.Round(discountAmount, 2);
        return (discountAmount, appliedDiscounts);
    }

    protected virtual async Task<(decimal DiscountAmount, List<Discount> AppliedDiscounts)>
        GetOrderTotalDiscountAsync(Customer customer, decimal orderTotal)
    {
        if (catalogSettings.IgnoreDiscounts)
            return (0m, []);

        var allDiscounts = await discountService.GetAllDiscountsAsync(DiscountType.AssignedToOrderTotal, showHidden: false);
        var allowed = new List<Discount>();
        foreach (var d in allDiscounts)
        {
            var result = await discountService.ValidateDiscountAsync(d, customer);
            if (result.IsValid && !allowed.ContainsDiscount(d))
                allowed.Add(d);
        }

        var appliedDiscounts = allowed.GetPreferredDiscount(orderTotal, out var discountAmount);
        if (discountAmount < 0m) discountAmount = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            discountAmount = Math.Round(discountAmount, 2);
        return (discountAmount, appliedDiscounts);
    }

    /// <summary>
    /// Check if cart requires shipping by loading products.
    /// </summary>
    private async Task<bool> CartRequiresShippingAsync(IList<ShoppingCartItem> cart)
    {
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product is { IsShipEnabled: true })
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if customer has a role with FreeShipping.
    /// </summary>
    private async Task<bool> CustomerHasFreeShippingRoleAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        foreach (var roleId in roleIds)
        {
            var role = await customerService.GetCustomerRoleByIdAsync(roleId);
            if (role is { Active: true, FreeShipping: true })
                return true;
        }
        return false;
    }

    #endregion

    #region Methods

    public virtual async Task<(decimal DiscountAmount, List<Discount> AppliedDiscounts,
        decimal SubTotalWithoutDiscount, decimal SubTotalWithDiscount,
        SortedDictionary<decimal, decimal> TaxRates)>
        GetShoppingCartSubTotalAsync(IList<ShoppingCartItem> cart, bool includingTax)
    {
        var taxRates = new SortedDictionary<decimal, decimal>();
        if (cart.Count == 0)
            return (0m, [], 0m, 0m, taxRates);

        var customer = workContext.CurrentCustomer;

        // sub totals per item
        var subTotalExclTax = 0m;
        var subTotalInclTax = 0m;
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null) continue;

            var sciSubTotal = await priceCalculationService.GetSubTotalAsync(sci);
            var (exclTax, taxRate1) = await taxService.GetProductPriceAsync(product, sciSubTotal, false, customer, taxSettings.PricesIncludeTax);
            var (inclTax, _) = await taxService.GetProductPriceAsync(product, sciSubTotal, true, customer, taxSettings.PricesIncludeTax);

            subTotalExclTax += exclTax;
            subTotalInclTax += inclTax;

            var sciTax = inclTax - exclTax;
            if (taxRate1 > 0m && sciTax > 0m)
            {
                if (!taxRates.ContainsKey(taxRate1))
                    taxRates.Add(taxRate1, sciTax);
                else
                    taxRates[taxRate1] += sciTax;
            }
        }

        // checkout attributes
        var checkoutAttributesXml = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, storeContext.CurrentStore.Id);
        if (!string.IsNullOrEmpty(checkoutAttributesXml))
        {
            var attributeValues = await checkoutAttributeParser.ParseCheckoutAttributeValuesAsync(checkoutAttributesXml);
            foreach (var av in attributeValues)
            {
                // load parent CheckoutAttribute for tax category
                var attributes = await checkoutAttributeParser.ParseCheckoutAttributesAsync(checkoutAttributesXml);
                var ca = attributes.FirstOrDefault(a => a.Id == av.CheckoutAttributeId);
                if (ca == null) continue;

                var (caExclTax, caTaxRate) = await taxService.GetCheckoutAttributePriceAsync(av, ca, false, customer);
                var (caInclTax, _) = await taxService.GetCheckoutAttributePriceAsync(av, ca, true, customer);
                subTotalExclTax += caExclTax;
                subTotalInclTax += caInclTax;

                var caTax = caInclTax - caExclTax;
                if (caTaxRate > 0m && caTax > 0m)
                {
                    if (!taxRates.ContainsKey(caTaxRate))
                        taxRates.Add(caTaxRate, caTax);
                    else
                        taxRates[caTaxRate] += caTax;
                }
            }
        }

        // subtotal without discount
        var subTotalWithoutDiscount = includingTax ? subTotalInclTax : subTotalExclTax;
        if (subTotalWithoutDiscount < 0m) subTotalWithoutDiscount = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            subTotalWithoutDiscount = Math.Round(subTotalWithoutDiscount, 2);

        // discount on excl-tax subtotal
        var (discountAmountExclTax, appliedDiscounts) = await GetOrderSubtotalDiscountAsync(customer, subTotalExclTax);
        if (subTotalExclTax < discountAmountExclTax)
            discountAmountExclTax = subTotalExclTax;
        var discountAmountInclTax = discountAmountExclTax;

        // subtotal with discount (excl tax)
        var subTotalExclTaxWithDiscount = subTotalExclTax - discountAmountExclTax;
        var subTotalInclTaxWithDiscount = subTotalExclTaxWithDiscount;

        // adjust tax rates for discount and compute incl-tax subtotal
        var tempTaxRates = new Dictionary<decimal, decimal>(taxRates);
        foreach (var kvp in tempTaxRates)
        {
            if (kvp.Value != 0m && subTotalExclTax > 0m)
            {
                var discountTax = taxRates[kvp.Key] * (discountAmountExclTax / subTotalExclTax);
                discountAmountInclTax += discountTax;
                var taxValue = taxRates[kvp.Key] - discountTax;
                if (shoppingCartSettings.RoundPricesDuringCalculation)
                    taxValue = Math.Round(taxValue, 2);
                taxRates[kvp.Key] = taxValue;
                subTotalInclTaxWithDiscount += taxValue;
            }
        }

        if (shoppingCartSettings.RoundPricesDuringCalculation)
        {
            discountAmountInclTax = Math.Round(discountAmountInclTax, 2);
            discountAmountExclTax = Math.Round(discountAmountExclTax, 2);
        }

        var subTotalWithDiscount = includingTax ? subTotalInclTaxWithDiscount : subTotalExclTaxWithDiscount;
        var discountAmount = includingTax ? discountAmountInclTax : discountAmountExclTax;

        if (subTotalWithDiscount < 0m) subTotalWithDiscount = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            subTotalWithDiscount = Math.Round(subTotalWithDiscount, 2);

        return (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates);
    }

    public virtual async Task<decimal> GetShoppingCartAdditionalShippingChargeAsync(IList<ShoppingCartItem> cart)
    {
        if (await IsFreeShippingAsync(cart))
            return 0m;

        var charge = 0m;
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product is { IsShipEnabled: true, IsFreeShipping: false })
                charge += product.AdditionalShippingCharge;
        }
        return charge;
    }

    public virtual async Task<bool> IsFreeShippingAsync(IList<ShoppingCartItem> cart, decimal? subTotal = null)
    {
        if (!await CartRequiresShippingAsync(cart))
            return true;

        var customer = workContext.CurrentCustomer;
        if (await CustomerHasFreeShippingRoleAsync(customer))
            return true;

        // all items marked free shipping?
        var allFree = true;
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product is { IsShipEnabled: true, IsFreeShipping: false })
            {
                allFree = false;
                break;
            }
        }
        if (allFree) return true;

        // free shipping over X
        if (shippingSettings.FreeShippingOverXEnabled)
        {
            if (!subTotal.HasValue)
            {
                var result = await GetShoppingCartSubTotalAsync(cart, shippingSettings.FreeShippingOverXIncludingTax);
                subTotal = result.SubTotalWithDiscount;
            }
            if (subTotal.Value > shippingSettings.FreeShippingOverXValue)
                return true;
        }

        return false;
    }

    public virtual async Task<(decimal AdjustedRate, List<Discount> AppliedDiscounts)>
        AdjustShippingRateAsync(decimal shippingRate, IList<ShoppingCartItem> cart)
    {
        if (await IsFreeShippingAsync(cart))
            return (0m, []);

        var additionalCharge = await GetShoppingCartAdditionalShippingChargeAsync(cart);
        var adjustedRate = shippingRate + additionalCharge;

        var customer = workContext.CurrentCustomer;
        var (discountAmount, appliedDiscounts) = await GetShippingDiscountAsync(customer, adjustedRate);
        adjustedRate -= discountAmount;

        if (adjustedRate < 0m) adjustedRate = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            adjustedRate = Math.Round(adjustedRate, 2);

        return (adjustedRate, appliedDiscounts);
    }

    public virtual async Task<(decimal? ShippingTotal, decimal TaxRate, List<Discount> AppliedDiscounts)>
        GetShoppingCartShippingTotalAsync(IList<ShoppingCartItem> cart, bool includingTax)
    {
        var customer = workContext.CurrentCustomer;

        if (await IsFreeShippingAsync(cart))
            return (0m, 0m, []);

        // try to get selected shipping option from customer generic attributes
        var shippingOption = await customer.GetAttributeAsync<ShippingOption>(
            SystemCustomerAttributeNames.SelectedShippingOption, genericAttributeService, storeContext.CurrentStore.Id);

        decimal? shippingTotal = null;
        var appliedDiscounts = new List<Discount>();

        if (shippingOption != null)
        {
            var (adjusted, discounts) = await AdjustShippingRateAsync(shippingOption.Rate, cart);
            shippingTotal = adjusted;
            appliedDiscounts = discounts;
        }
        // else: no shipping option selected — shipping rate computation methods are plugin-dependent (deferred to [2.10])
        // return null to indicate shipping total cannot be calculated

        if (!shippingTotal.HasValue)
            return (null, 0m, appliedDiscounts);

        if (shippingTotal.Value < 0m) shippingTotal = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            shippingTotal = Math.Round(shippingTotal.Value, 2);

        var (taxedPrice, taxRate) = await taxService.GetShippingPriceAsync(shippingTotal.Value, includingTax, customer);
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            taxedPrice = Math.Round(taxedPrice, 2);

        return (taxedPrice, taxRate, appliedDiscounts);
    }

    public virtual async Task<(decimal TaxTotal, SortedDictionary<decimal, decimal> TaxRates)>
        GetTaxTotalAsync(IList<ShoppingCartItem> cart, bool usePaymentMethodAdditionalFee = true)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var taxRates = new SortedDictionary<decimal, decimal>();
        var customer = workContext.CurrentCustomer;

        // subtotal tax
        var subTotalResult = await GetShoppingCartSubTotalAsync(cart, false);
        var subTotalTaxTotal = 0m;
        foreach (var kvp in subTotalResult.TaxRates)
        {
            subTotalTaxTotal += kvp.Value;
            if (kvp.Key > 0m && kvp.Value > 0m)
            {
                if (!taxRates.ContainsKey(kvp.Key))
                    taxRates.Add(kvp.Key, kvp.Value);
                else
                    taxRates[kvp.Key] += kvp.Value;
            }
        }

        // shipping tax
        var shippingTax = 0m;
        if (taxSettings.ShippingIsTaxable)
        {
            var (shippingExclTax, _, _) = await GetShoppingCartShippingTotalAsync(cart, false);
            var (shippingInclTax, shippingTaxRate, _) = await GetShoppingCartShippingTotalAsync(cart, true);
            if (shippingExclTax.HasValue && shippingInclTax.HasValue)
            {
                shippingTax = shippingInclTax.Value - shippingExclTax.Value;
                if (shippingTax < 0m) shippingTax = 0m;

                if (shippingTaxRate > 0m && shippingTax > 0m)
                {
                    if (!taxRates.ContainsKey(shippingTaxRate))
                        taxRates.Add(shippingTaxRate, shippingTax);
                    else
                        taxRates[shippingTaxRate] += shippingTax;
                }
            }
        }

        // payment method additional fee tax
        var paymentFeeTax = 0m;
        if (usePaymentMethodAdditionalFee && taxSettings.PaymentMethodAdditionalFeeIsTaxable)
        {
            var paymentMethodSystemName = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.SelectedPaymentMethod, genericAttributeService, storeContext.CurrentStore.Id);

            if (!string.IsNullOrEmpty(paymentMethodSystemName))
            {
                var fee = await paymentService.GetAdditionalHandlingFeeAsync(cart, paymentMethodSystemName);
                var (feeExclTax, feeTaxRate) = await taxService.GetPaymentMethodAdditionalFeeAsync(fee, false, customer);
                var (feeInclTax, _) = await taxService.GetPaymentMethodAdditionalFeeAsync(fee, true, customer);

                paymentFeeTax = feeInclTax - feeExclTax;
                if (paymentFeeTax < 0m) paymentFeeTax = 0m;

                if (feeTaxRate > 0m && paymentFeeTax > 0m)
                {
                    if (!taxRates.ContainsKey(feeTaxRate))
                        taxRates.Add(feeTaxRate, paymentFeeTax);
                    else
                        taxRates[feeTaxRate] += paymentFeeTax;
                }
            }
        }

        // at least one tax rate
        if (taxRates.Count == 0)
            taxRates.Add(0m, 0m);

        var taxTotal = subTotalTaxTotal + shippingTax + paymentFeeTax;
        if (taxTotal < 0m) taxTotal = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            taxTotal = Math.Round(taxTotal, 2);

        return (taxTotal, taxRates);
    }

    public virtual async Task<ShoppingCartTotal?> GetShoppingCartTotalAsync(IList<ShoppingCartItem> cart,
        bool? useRewardPoints = null, bool usePaymentMethodAdditionalFee = true)
    {
        var customer = workContext.CurrentCustomer;

        // subtotal without tax
        var subTotalResult = await GetShoppingCartSubTotalAsync(cart, false);
        var subtotalBase = subTotalResult.SubTotalWithDiscount;

        // shipping without tax
        var (shippingTotal, _, _) = await GetShoppingCartShippingTotalAsync(cart, false);

        // payment method additional fee without tax
        var paymentFeeWithoutTax = 0m;
        if (usePaymentMethodAdditionalFee)
        {
            var paymentMethodSystemName = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.SelectedPaymentMethod, genericAttributeService, storeContext.CurrentStore.Id);
            if (!string.IsNullOrEmpty(paymentMethodSystemName))
            {
                var fee = await paymentService.GetAdditionalHandlingFeeAsync(cart, paymentMethodSystemName);
                var (feeExclTax, _) = await taxService.GetPaymentMethodAdditionalFeeAsync(fee, false, customer);
                paymentFeeWithoutTax = feeExclTax;
            }
        }

        // tax
        var (taxTotal, _) = await GetTaxTotalAsync(cart, usePaymentMethodAdditionalFee);

        // order total
        var resultTemp = subtotalBase;
        if (shippingTotal.HasValue)
            resultTemp += shippingTotal.Value;
        resultTemp += paymentFeeWithoutTax;
        resultTemp += taxTotal;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            resultTemp = Math.Round(resultTemp, 2);

        // order total discount
        var (discountAmount, appliedDiscounts) = await GetOrderTotalDiscountAsync(customer, resultTemp);
        if (resultTemp < discountAmount)
            discountAmount = resultTemp;
        resultTemp -= discountAmount;
        if (resultTemp < 0m) resultTemp = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            resultTemp = Math.Round(resultTemp, 2);

        // gift cards (not for recurring orders)
        var appliedGiftCards = new List<AppliedGiftCard>();
        var hasRecurring = cart.Any(sci => sci.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart);
        // check if any item is recurring
        var isRecurring = false;
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product is { IsRecurring: true }) { isRecurring = true; break; }
        }

        if (!isRecurring)
        {
            var giftCards = await giftCardService.GetActiveGiftCardsAppliedByCustomerAsync(customer.Id);
            foreach (var gc in giftCards)
            {
                if (resultTemp <= 0m) break;
                var remaining = await giftCardService.GetGiftCardRemainingAmountAsync(gc);
                var amountCanBeUsed = resultTemp > remaining ? remaining : resultTemp;
                resultTemp -= amountCanBeUsed;
                appliedGiftCards.Add(new AppliedGiftCard { GiftCard = gc, AmountCanBeUsed = amountCanBeUsed });
            }
        }

        if (resultTemp < 0m) resultTemp = 0m;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            resultTemp = Math.Round(resultTemp, 2);

        // if shipping not selected, return null
        if (!shippingTotal.HasValue && await CartRequiresShippingAsync(cart))
            return null;

        var orderTotal = resultTemp;

        // reward points
        var redeemedRewardPoints = 0;
        var redeemedRewardPointsAmount = 0m;
        if (rewardPointsSettings.Enabled)
        {
            if (!useRewardPoints.HasValue)
                useRewardPoints = await customer.GetAttributeAsync<bool>(
                    SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, genericAttributeService, storeContext.CurrentStore.Id);

            if (useRewardPoints == true)
            {
                var balance = await rewardPointService.GetRewardPointsBalanceAsync(customer.Id, storeContext.CurrentStore.Id);
                if (CheckMinimumRewardPointsToUseRequirement(balance))
                {
                    var balanceAmount = ConvertRewardPointsToAmount(balance);
                    if (orderTotal > 0m)
                    {
                        if (orderTotal > balanceAmount)
                        {
                            redeemedRewardPoints = balance;
                            redeemedRewardPointsAmount = balanceAmount;
                        }
                        else
                        {
                            redeemedRewardPointsAmount = orderTotal;
                            redeemedRewardPoints = ConvertAmountToRewardPoints(redeemedRewardPointsAmount);
                        }
                    }
                }
            }
        }

        orderTotal -= redeemedRewardPointsAmount;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            orderTotal = Math.Round(orderTotal, 2);

        return new ShoppingCartTotal
        {
            OrderTotal = orderTotal,
            DiscountAmount = discountAmount,
            AppliedDiscounts = appliedDiscounts,
            AppliedGiftCards = appliedGiftCards,
            RedeemedRewardPoints = redeemedRewardPoints,
            RedeemedRewardPointsAmount = redeemedRewardPointsAmount
        };
    }

    public virtual decimal ConvertRewardPointsToAmount(int rewardPoints)
    {
        if (rewardPoints <= 0) return 0m;
        var result = rewardPoints * rewardPointsSettings.ExchangeRate;
        if (shoppingCartSettings.RoundPricesDuringCalculation)
            result = Math.Round(result, 2);
        return result;
    }

    public virtual int ConvertAmountToRewardPoints(decimal amount)
    {
        if (amount <= 0m || rewardPointsSettings.ExchangeRate <= 0m) return 0;
        return (int)Math.Ceiling(amount / rewardPointsSettings.ExchangeRate);
    }

    public virtual bool CheckMinimumRewardPointsToUseRequirement(int rewardPoints)
    {
        if (rewardPointsSettings.MinimumRewardPointsToUse <= 0) return true;
        return rewardPoints >= rewardPointsSettings.MinimumRewardPointsToUse;
    }

    public virtual decimal CalculateApplicableOrderTotalForRewardPoints(decimal orderShippingInclTax, decimal orderTotal)
        => orderTotal - orderShippingInclTax;

    public virtual async Task<int> CalculateRewardPointsAsync(Customer customer, decimal amount)
    {
        if (!rewardPointsSettings.Enabled || rewardPointsSettings.PointsForPurchases_Amount <= 0m)
            return 0;

        // only for registered customers
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole != null && roleIds.Contains(guestRole.Id) && roleIds.Length == 1)
            return 0;

        return (int)Math.Truncate(amount / rewardPointsSettings.PointsForPurchases_Amount * rewardPointsSettings.PointsForPurchases_Points);
    }

    #endregion
}
