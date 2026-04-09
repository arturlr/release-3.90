using Nop.Core.Domain.Directory;

namespace Nop.Services.Directory;

public interface ICurrencyService
{
    Task DeleteCurrencyAsync(Currency currency);
    Task<Currency?> GetCurrencyByIdAsync(int currencyId);
    Task<Currency?> GetCurrencyByCodeAsync(string currencyCode);
    Task<IList<Currency>> GetAllCurrenciesAsync(bool showHidden = false, int storeId = 0);
    Task InsertCurrencyAsync(Currency currency);
    Task UpdateCurrencyAsync(Currency currency);

    // Conversions
    decimal ConvertCurrency(decimal amount, decimal exchangeRate);
    decimal ConvertCurrency(decimal amount, Currency sourceCurrency, Currency targetCurrency);
    decimal ConvertToPrimaryExchangeRateCurrency(decimal amount, Currency sourceCurrency);
    decimal ConvertFromPrimaryExchangeRateCurrency(decimal amount, Currency targetCurrency);
    decimal ConvertToPrimaryStoreCurrency(decimal amount, Currency sourceCurrency);
    decimal ConvertFromPrimaryStoreCurrency(decimal amount, Currency targetCurrency);
}
