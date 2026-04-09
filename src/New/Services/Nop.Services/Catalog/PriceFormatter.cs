using System.Globalization;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Services.Directory;
using Nop.Services.Localization;

namespace Nop.Services.Catalog;

public class PriceFormatter : IPriceFormatter
{
    private readonly IWorkContext _workContext;
    private readonly ICurrencyService _currencyService;
    private readonly ILocalizationService _localizationService;
    private readonly TaxSettings _taxSettings;
    private readonly CurrencySettings _currencySettings;

    public PriceFormatter(
        IWorkContext workContext,
        ICurrencyService currencyService,
        ILocalizationService localizationService,
        TaxSettings taxSettings,
        CurrencySettings currencySettings)
    {
        _workContext = workContext;
        _currencyService = currencyService;
        _localizationService = localizationService;
        _taxSettings = taxSettings;
        _currencySettings = currencySettings;
    }

    public virtual async Task<string> FormatPriceAsync(decimal price)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return FormatPriceInternal(price, true, currency);
    }

    public virtual Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency) =>
        Task.FromResult(FormatPriceInternal(price, showCurrency, targetCurrency));

    public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, bool showTax)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return FormatPriceInternal(price, showCurrency, currency);
    }

    public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, bool showTax, Language language)
    {
        var currency = await GetCurrencyByCodeAsync(currencyCode);
        return FormatPriceInternal(price, showCurrency, currency);
    }

    public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax)
    {
        var currency = await GetCurrencyByCodeAsync(currencyCode);
        return FormatPriceInternal(price, showCurrency, currency);
    }

    public virtual Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax) =>
        Task.FromResult(FormatPriceInternal(price, showCurrency, targetCurrency));

    public virtual Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax) =>
        Task.FromResult(FormatPriceInternal(price, showCurrency, targetCurrency));

    public virtual string FormatRentalProductPeriod(Product product, string price)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (!product.IsRental) return price;

        var period = (RentalPricePeriod)product.RentalPricePeriodId;
        var periodStr = period switch
        {
            RentalPricePeriod.Days => "day",
            RentalPricePeriod.Weeks => "week",
            RentalPricePeriod.Months => "month",
            RentalPricePeriod.Years => "year",
            _ => string.Empty
        };

        var length = product.RentalPriceLength;
        return length > 1 ? $"{price} per {length} {periodStr}s" : $"{price} per {periodStr}";
    }

    public virtual async Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return FormatPriceInternal(price, showCurrency, currency);
    }

    public virtual async Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return FormatPriceInternal(price, showCurrency, currency);
    }

    public virtual string FormatTaxRate(decimal taxRate) =>
        taxRate.ToString("G29", CultureInfo.InvariantCulture);

    private static string FormatPriceInternal(decimal price, bool showCurrency, Currency? currency)
    {
        if (currency == null)
            return price.ToString("C2", CultureInfo.CurrentCulture);

        if (!string.IsNullOrEmpty(currency.CustomFormatting))
            return price.ToString(currency.CustomFormatting);

        var formatted = price.ToString("N2");
        return showCurrency ? $"{formatted} {currency.CurrencyCode}" : formatted;
    }

    private async Task<Currency?> GetCurrencyByCodeAsync(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);

        var currencies = await _currencyService.GetAllCurrenciesAsync();
        return currencies.FirstOrDefault(c => c.CurrencyCode == currencyCode)
               ?? await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
    }
}
