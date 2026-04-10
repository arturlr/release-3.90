using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Vendors;

namespace Nop.Web.Framework;

/// <summary>
/// Work context for web application — resolves current customer, language, currency, tax display type from HTTP context.
/// Registered as Scoped — per-request caching via private fields.
/// </summary>
public partial class WebWorkContext(
    IHttpContextAccessor httpContextAccessor,
    ICustomerService customerService,
    IVendorService vendorService,
    IStoreContext storeContext,
    IAuthenticationService authenticationService,
    ILanguageService languageService,
    ICurrencyService currencyService,
    IGenericAttributeService genericAttributeService,
    IUserAgentHelper userAgentHelper,
    TaxSettings taxSettings,
    CurrencySettings currencySettings) : IWorkContext
{
    private const string CustomerCookieName = "Nop.customer";

    private Customer? _cachedCustomer;
    private Customer? _originalCustomerIfImpersonated;
    private Vendor? _cachedVendor;
    private Language? _cachedLanguage;
    private Currency? _cachedCurrency;
    private TaxDisplayType? _cachedTaxDisplayType;

    public virtual Customer CurrentCustomer
    {
        get
        {
            if (_cachedCustomer != null)
                return _cachedCustomer;

            var customer = ResolveCustomerAsync().GetAwaiter().GetResult();

            if (customer != null && !customer.Deleted && customer.Active && !customer.RequireReLogin)
            {
                SetCustomerCookie(customer.CustomerGuid);
                _cachedCustomer = customer;
            }

            return _cachedCustomer!;
        }
        set
        {
            SetCustomerCookie(value.CustomerGuid);
            _cachedCustomer = value;
        }
    }

    public virtual Customer? OriginalCustomerIfImpersonated => _originalCustomerIfImpersonated;

    public virtual Vendor? CurrentVendor
    {
        get
        {
            if (_cachedVendor != null)
                return _cachedVendor;

            var current = CurrentCustomer;
            if (current.VendorId == 0)
                return null;

            var vendor = vendorService.GetVendorByIdAsync(current.VendorId).GetAwaiter().GetResult();
            if (vendor is { Deleted: false, Active: true })
                _cachedVendor = vendor;

            return _cachedVendor;
        }
    }

    public virtual Language WorkingLanguage
    {
        get
        {
            if (_cachedLanguage != null)
                return _cachedLanguage;

            _cachedLanguage = ResolveLanguageAsync().GetAwaiter().GetResult();
            return _cachedLanguage!;
        }
        set
        {
            var languageId = value?.Id ?? 0;
            genericAttributeService.SaveAttributeAsync(CurrentCustomer,
                SystemCustomerAttributeNames.LanguageId, languageId, storeContext.CurrentStore.Id)
                .GetAwaiter().GetResult();
            _cachedLanguage = null;
        }
    }

    public virtual Currency WorkingCurrency
    {
        get
        {
            if (_cachedCurrency != null)
                return _cachedCurrency;

            _cachedCurrency = ResolveCurrencyAsync().GetAwaiter().GetResult();
            return _cachedCurrency!;
        }
        set
        {
            var currencyId = value?.Id ?? 0;
            genericAttributeService.SaveAttributeAsync(CurrentCustomer,
                SystemCustomerAttributeNames.CurrencyId, currencyId, storeContext.CurrentStore.Id)
                .GetAwaiter().GetResult();
            _cachedCurrency = null;
        }
    }

    public virtual TaxDisplayType TaxDisplayType
    {
        get
        {
            if (_cachedTaxDisplayType.HasValue)
                return _cachedTaxDisplayType.Value;

            if (taxSettings.AllowCustomersToSelectTaxDisplayType)
            {
                var taxDisplayTypeId = CurrentCustomer.GetAttributeAsync<int>(
                    SystemCustomerAttributeNames.TaxDisplayTypeId, genericAttributeService,
                    storeContext.CurrentStore.Id).GetAwaiter().GetResult();
                _cachedTaxDisplayType = (TaxDisplayType)taxDisplayTypeId;
            }
            else
            {
                _cachedTaxDisplayType = taxSettings.TaxDisplayType;
            }

            return _cachedTaxDisplayType.Value;
        }
        set
        {
            if (!taxSettings.AllowCustomersToSelectTaxDisplayType)
                return;

            genericAttributeService.SaveAttributeAsync(CurrentCustomer,
                SystemCustomerAttributeNames.TaxDisplayTypeId, (int)value, storeContext.CurrentStore.Id)
                .GetAwaiter().GetResult();
            _cachedTaxDisplayType = null;
        }
    }

    public virtual bool IsAdmin { get; set; }

    #region Utilities

    private async Task<Customer?> ResolveCustomerAsync()
    {
        Customer? customer = null;

        // Background task context (no HTTP context)
        if (httpContextAccessor.HttpContext == null)
            customer = await customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.BackgroundTask);

        // Search engine
        if (!IsValidCustomer(customer) && userAgentHelper.IsSearchEngine())
            customer = await customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.SearchEngine);

        // Authenticated user
        if (!IsValidCustomer(customer))
            customer = await authenticationService.GetAuthenticatedCustomerAsync();

        // Impersonation
        if (IsValidCustomer(customer))
        {
            var impersonatedId = await customer!.GetAttributeAsync<int?>(
                SystemCustomerAttributeNames.ImpersonatedCustomerId, genericAttributeService);
            if (impersonatedId is > 0)
            {
                var impersonated = await customerService.GetCustomerByIdAsync(impersonatedId.Value);
                if (IsValidCustomer(impersonated))
                {
                    _originalCustomerIfImpersonated = customer;
                    customer = impersonated;
                }
            }
        }

        // Guest from cookie
        if (!IsValidCustomer(customer))
        {
            var customerGuid = GetCustomerGuidFromCookie();
            if (customerGuid.HasValue)
            {
                var cookieCustomer = await customerService.GetCustomerByGuidAsync(customerGuid.Value);
                if (cookieCustomer != null)
                {
                    // Cookie customer must NOT be registered
                    var roleIds = await customerService.GetCustomerRoleIdsAsync(cookieCustomer);
                    var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
                    if (registeredRole == null || !roleIds.Contains(registeredRole.Id))
                        customer = cookieCustomer;
                }
            }
        }

        // Create guest
        if (!IsValidCustomer(customer))
            customer = await customerService.InsertGuestCustomerAsync();

        return customer;
    }

    private async Task<Language> ResolveLanguageAsync()
    {
        var storeId = storeContext.CurrentStore.Id;
        var allLanguages = await languageService.GetAllLanguagesAsync(storeId: storeId);

        // Customer's saved language
        var languageId = await CurrentCustomer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.LanguageId, genericAttributeService, storeId);
        var language = allLanguages.FirstOrDefault(x => x.Id == languageId);

        // Store default language
        language ??= allLanguages.FirstOrDefault(x => x.Id == storeContext.CurrentStore.DefaultLanguageId);

        // First available for store
        language ??= allLanguages.FirstOrDefault();

        // Any language at all
        language ??= (await languageService.GetAllLanguagesAsync()).FirstOrDefault();

        return language!;
    }

    private async Task<Currency> ResolveCurrencyAsync()
    {
        var storeId = storeContext.CurrentStore.Id;

        // Admin area → primary store currency
        if (IsAdmin)
        {
            var primary = currencyService.GetCurrencyByIdAsync(currencySettings.PrimaryStoreCurrencyId)
                .GetAwaiter().GetResult();
            if (primary != null)
                return primary;
        }

        var allCurrencies = await currencyService.GetAllCurrenciesAsync(storeId: storeId);

        // Customer's saved currency
        var currencyId = await CurrentCustomer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.CurrencyId, genericAttributeService, storeId);
        var currency = allCurrencies.FirstOrDefault(x => x.Id == currencyId);

        // Language default currency
        currency ??= allCurrencies.FirstOrDefault(x => x.Id == WorkingLanguage.DefaultCurrencyId);

        // First available for store
        currency ??= allCurrencies.FirstOrDefault();

        // Any currency at all
        currency ??= (await currencyService.GetAllCurrenciesAsync()).FirstOrDefault();

        return currency!;
    }

    private Guid? GetCustomerGuidFromCookie()
    {
        var cookieValue = httpContextAccessor.HttpContext?.Request.Cookies[CustomerCookieName];
        return Guid.TryParse(cookieValue, out var guid) ? guid : null;
    }

    private void SetCustomerCookie(Guid customerGuid)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return;

        var options = new CookieOptions
        {
            HttpOnly = true,
            Expires = customerGuid == Guid.Empty
                ? DateTimeOffset.UtcNow.AddMonths(-1)
                : DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true
        };

        context.Response.Cookies.Delete(CustomerCookieName);
        context.Response.Cookies.Append(CustomerCookieName, customerGuid.ToString(), options);
    }

    private static bool IsValidCustomer(Customer? customer) =>
        customer is { Deleted: false, Active: true, RequireReLogin: false };

    #endregion
}
