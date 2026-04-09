using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;

namespace Nop.Services.Tax;

public interface ITaxService
{
    /// <summary>
    /// Gets product price with tax applied.
    /// </summary>
    /// <returns>(price, taxRate)</returns>
    Task<(decimal price, decimal taxRate)> GetProductPriceAsync(Product product, decimal price,
        bool includingTax, Customer customer, bool priceIncludesTax);

    /// <summary>
    /// Gets product price using current customer and TaxDisplayType from IWorkContext.
    /// </summary>
    Task<(decimal price, decimal taxRate)> GetProductPriceAsync(Product product, decimal price);

    /// <summary>
    /// Gets product price for a specific customer using TaxDisplayType from IWorkContext.
    /// </summary>
    Task<(decimal price, decimal taxRate)> GetProductPriceAsync(Product product, decimal price, Customer customer);

    /// <summary>
    /// Gets product price with explicit taxCategoryId override.
    /// </summary>
    Task<(decimal price, decimal taxRate)> GetProductPriceAsync(Product? product, int taxCategoryId,
        decimal price, bool includingTax, Customer customer, bool priceIncludesTax);

    /// <summary>
    /// Gets shipping price with tax.
    /// </summary>
    Task<(decimal price, decimal taxRate)> GetShippingPriceAsync(decimal price, bool includingTax, Customer customer);

    /// <summary>
    /// Gets payment method additional fee with tax.
    /// </summary>
    Task<(decimal price, decimal taxRate)> GetPaymentMethodAdditionalFeeAsync(decimal price, bool includingTax, Customer customer);

    /// <summary>
    /// Gets checkout attribute value price with tax.
    /// CheckoutAttribute must be passed explicitly (no nav properties).
    /// </summary>
    Task<(decimal price, decimal taxRate)> GetCheckoutAttributePriceAsync(
        CheckoutAttributeValue cav, CheckoutAttribute checkoutAttribute,
        bool includingTax, Customer customer);

    /// <summary>
    /// Gets VAT number status from a full VAT number string (e.g. "GB 111 1111 111").
    /// </summary>
    Task<(VatNumberStatus status, string name, string address)> GetVatNumberStatusAsync(string fullVatNumber);

    /// <summary>
    /// Gets VAT number status from country code and VAT number.
    /// </summary>
    Task<(VatNumberStatus status, string name, string address)> GetVatNumberStatusAsync(
        string twoLetterIsoCode, string vatNumber);

    /// <summary>
    /// Performs VAT check against VIES service.
    /// Deferred to [7.12] — returns Unknown until VIES integration is built.
    /// </summary>
    Task<(VatNumberStatus status, string name, string address, Exception? exception)> DoVatCheckAsync(
        string twoLetterIsoCode, string vatNumber);

    /// <summary>
    /// Checks if a product is tax exempt for a given customer.
    /// </summary>
    Task<bool> IsTaxExemptAsync(Product? product, Customer? customer);

    /// <summary>
    /// Checks if EU VAT exempt (European Union Value Added Tax).
    /// </summary>
    Task<bool> IsVatExemptAsync(Address? address, Customer? customer);
}
