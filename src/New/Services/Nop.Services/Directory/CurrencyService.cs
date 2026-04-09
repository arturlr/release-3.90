using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Directory;
using Nop.Services.Events;
using Nop.Services.Stores;

namespace Nop.Services.Directory;

public class CurrencyService : ICurrencyService
{
    private const string CurrenciesByIdKey = "Nop.currency.id-{0}";
    private const string CurrenciesAllKey = "Nop.currency.all-{0}";
    private const string CurrenciesPrefix = "Nop.currency.";

    private readonly IRepository<Currency> _currencyRepository;
    private readonly IStoreMappingService _storeMappingService;
    private readonly CurrencySettings _currencySettings;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public CurrencyService(
        IRepository<Currency> currencyRepository,
        IStoreMappingService storeMappingService,
        CurrencySettings currencySettings,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _currencyRepository = currencyRepository;
        _storeMappingService = storeMappingService;
        _currencySettings = currencySettings;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task DeleteCurrencyAsync(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        _currencyRepository.Delete(currency);
        await _cacheManager.RemoveByPrefixAsync(CurrenciesPrefix);
        await _eventPublisher.EntityDeletedAsync(currency);
    }

    public virtual async Task<Currency?> GetCurrencyByIdAsync(int currencyId)
    {
        if (currencyId == 0)
            return null;

        var key = new CacheKey(string.Format(CurrenciesByIdKey, currencyId), CurrenciesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_currencyRepository.GetById(currencyId)));
    }

    public virtual async Task<Currency?> GetCurrencyByCodeAsync(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        var currencies = await GetAllCurrenciesAsync(showHidden: true);
        return currencies.FirstOrDefault(c =>
            string.Equals(c.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
    }

    public virtual async Task<IList<Currency>> GetAllCurrenciesAsync(bool showHidden = false, int storeId = 0)
    {
        var key = new CacheKey(string.Format(CurrenciesAllKey, showHidden), CurrenciesPrefix);
        var currencies = await _cacheManager.GetAsync(key, () =>
        {
            var query = _currencyRepository.TableNoTracking;
            if (!showHidden)
                query = query.Where(c => c.Published);

            query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id);
            return Task.FromResult<IList<Currency>>(query.ToList());
        }) ?? [];

        // post-cache store mapping filter
        if (storeId > 0)
            currencies = currencies
                .Where(c => _storeMappingService.AuthorizeAsync(c, storeId).GetAwaiter().GetResult())
                .ToList();

        return currencies;
    }

    public virtual async Task InsertCurrencyAsync(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        _currencyRepository.Insert(currency);
        await _cacheManager.RemoveByPrefixAsync(CurrenciesPrefix);
        await _eventPublisher.EntityInsertedAsync(currency);
    }

    public virtual async Task UpdateCurrencyAsync(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        _currencyRepository.Update(currency);
        await _cacheManager.RemoveByPrefixAsync(CurrenciesPrefix);
        await _eventPublisher.EntityUpdatedAsync(currency);
    }

    #region Conversions

    public virtual decimal ConvertCurrency(decimal amount, decimal exchangeRate)
    {
        if (amount != decimal.Zero && exchangeRate != decimal.Zero)
            return amount * exchangeRate;
        return decimal.Zero;
    }

    public virtual decimal ConvertCurrency(decimal amount, Currency sourceCurrency, Currency targetCurrency)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrency);
        ArgumentNullException.ThrowIfNull(targetCurrency);

        if (amount == decimal.Zero || sourceCurrency.Id == targetCurrency.Id)
            return amount;

        var result = ConvertToPrimaryExchangeRateCurrency(amount, sourceCurrency);
        return ConvertFromPrimaryExchangeRateCurrency(result, targetCurrency);
    }

    public virtual decimal ConvertToPrimaryExchangeRateCurrency(decimal amount, Currency sourceCurrency)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrency);

        var primary = GetCurrencyByIdAsync(_currencySettings.PrimaryExchangeRateCurrencyId).GetAwaiter().GetResult()
            ?? throw new NopException("Primary exchange rate currency cannot be loaded");

        if (amount == decimal.Zero || sourceCurrency.Id == primary.Id)
            return amount;

        var rate = sourceCurrency.Rate;
        if (rate == decimal.Zero)
            throw new NopException($"Exchange rate not found for currency [{sourceCurrency.Name}]");

        return amount / rate;
    }

    public virtual decimal ConvertFromPrimaryExchangeRateCurrency(decimal amount, Currency targetCurrency)
    {
        ArgumentNullException.ThrowIfNull(targetCurrency);

        var primary = GetCurrencyByIdAsync(_currencySettings.PrimaryExchangeRateCurrencyId).GetAwaiter().GetResult()
            ?? throw new NopException("Primary exchange rate currency cannot be loaded");

        if (amount == decimal.Zero || targetCurrency.Id == primary.Id)
            return amount;

        var rate = targetCurrency.Rate;
        if (rate == decimal.Zero)
            throw new NopException($"Exchange rate not found for currency [{targetCurrency.Name}]");

        return amount * rate;
    }

    public virtual decimal ConvertToPrimaryStoreCurrency(decimal amount, Currency sourceCurrency)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrency);

        var primaryStore = GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId).GetAwaiter().GetResult()
            ?? throw new NopException("Primary store currency cannot be loaded");

        return ConvertCurrency(amount, sourceCurrency, primaryStore);
    }

    public virtual decimal ConvertFromPrimaryStoreCurrency(decimal amount, Currency targetCurrency)
    {
        ArgumentNullException.ThrowIfNull(targetCurrency);

        var primaryStore = GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId).GetAwaiter().GetResult()
            ?? throw new NopException("Primary store currency cannot be loaded");

        return ConvertCurrency(amount, primaryStore, targetCurrency);
    }

    #endregion
}
