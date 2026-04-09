using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;

namespace Nop.Services.Catalog;

public interface IPriceFormatter
{
    Task<string> FormatPriceAsync(decimal price);
    Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency);
    Task<string> FormatPriceAsync(decimal price, bool showCurrency, bool showTax);
    Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, bool showTax, Language language);
    Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax);
    Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax);
    Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);
    string FormatRentalProductPeriod(Product product, string price);
    Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency);
    Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency);
    string FormatTaxRate(decimal taxRate);
}
