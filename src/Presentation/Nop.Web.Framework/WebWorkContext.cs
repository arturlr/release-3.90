using System;
using System.Linq;
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
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Framework.Localization;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Work context for web application
    /// </summary>
    public partial class WebWorkContext : IWorkContext
    {
        #region Const

        private const string CustomerCookieName = "Nop.customer";

        #endregion

        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly IVendorService _vendorService;
        private readonly IStoreContext _storeContext;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILanguageService _languageService;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly TaxSettings _taxSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IUserAgentHelper _userAgentHelper;
        private readonly IStoreMappingService _storeMappingService;

        private Customer _cachedCustomer;
        private Customer _originalCustomerIfImpersonated;
        private Vendor _cachedVendor;
        private Language _cachedLanguage;
        private Currency _cachedCurrency;
        private TaxDisplayType? _cachedTaxDisplayType;

        #endregion

        #region Ctor

        public WebWorkContext(IHttpContextAccessor httpContextAccessor,
            ICustomerService customerService,
            IVendorService vendorService,
            IStoreContext storeContext,
            IAuthenticationService authenticationService,
            ILanguageService languageService,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            TaxSettings taxSettings,
            CurrencySettings currencySettings,
            LocalizationSettings localizationSettings,
            IUserAgentHelper userAgentHelper,
            IStoreMappingService storeMappingService)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._customerService = customerService;
            this._vendorService = vendorService;
            this._storeContext = storeContext;
            this._authenticationService = authenticationService;
            this._languageService = languageService;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._taxSettings = taxSettings;
            this._currencySettings = currencySettings;
            this._localizationSettings = localizationSettings;
            this._userAgentHelper = userAgentHelper;
            this._storeMappingService = storeMappingService;
        }

        #endregion

        #region Utilities

        protected virtual string GetCustomerCookieValue()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request == null)
                return null;

            return httpContext.Request.Cookies[CustomerCookieName];
        }

        protected virtual void SetCustomerCookie(Guid customerGuid)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Response == null)
                return;

            if (customerGuid == Guid.Empty)
            {
                httpContext.Response.Cookies.Delete(CustomerCookieName);
            }
            else
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddHours(24 * 365)
                };
                httpContext.Response.Cookies.Append(CustomerCookieName, customerGuid.ToString(), cookieOptions);
            }
        }

        protected virtual Language GetLanguageFromUrl()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request == null)
                return null;

            var path = httpContext.Request.Path.Value;
            if (string.IsNullOrEmpty(path))
                return null;

            var pathBase = httpContext.Request.PathBase.Value ?? string.Empty;
            if (!path.IsLocalizedUrl(pathBase, false))
                return null;

            var seoCode = path.GetLanguageSeoCodeFromUrl(pathBase, false);
            if (string.IsNullOrEmpty(seoCode))
                return null;

            var language = _languageService
                .GetAllLanguages()
                .FirstOrDefault(l => seoCode.Equals(l.UniqueSeoCode, StringComparison.InvariantCultureIgnoreCase));
            if (language != null && language.Published && _storeMappingService.Authorize(language))
            {
                return language;
            }

            return null;
        }

        protected virtual Language GetLanguageFromBrowserSettings()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request == null)
                return null;

            var acceptLanguageHeader = httpContext.Request.Headers["Accept-Language"].ToString();
            if (string.IsNullOrEmpty(acceptLanguageHeader))
                return null;

            // Take the first language from the header
            var userLanguage = acceptLanguageHeader.Split(',').FirstOrDefault()?.Split(';').FirstOrDefault();
            if (string.IsNullOrEmpty(userLanguage))
                return null;

            var language = _languageService
                .GetAllLanguages()
                .FirstOrDefault(l => userLanguage.Equals(l.LanguageCulture, StringComparison.InvariantCultureIgnoreCase));
            if (language != null && language.Published && _storeMappingService.Authorize(language))
            {
                return language;
            }

            return null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current customer
        /// </summary>
        public virtual Customer CurrentCustomer
        {
            get
            {
                if (_cachedCustomer != null)
                    return _cachedCustomer;

                Customer customer = null;
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    customer = _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);
                }

                //check whether request is made by a search engine
                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    if (_userAgentHelper.IsSearchEngine())
                    {
                        customer = _customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
                    }
                }

                //registered user
                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    customer = _authenticationService.GetAuthenticatedCustomer();
                }

                //impersonate user if required
                if (customer != null && !customer.Deleted && customer.Active && !customer.RequireReLogin)
                {
                    var impersonatedCustomerId = customer.GetAttribute<int?>(SystemCustomerAttributeNames.ImpersonatedCustomerId);
                    if (impersonatedCustomerId.HasValue && impersonatedCustomerId.Value > 0)
                    {
                        var impersonatedCustomer = _customerService.GetCustomerById(impersonatedCustomerId.Value);
                        if (impersonatedCustomer != null && !impersonatedCustomer.Deleted && impersonatedCustomer.Active && !impersonatedCustomer.RequireReLogin)
                        {
                            _originalCustomerIfImpersonated = customer;
                            customer = impersonatedCustomer;
                        }
                    }
                }

                //load guest customer
                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    var customerCookieValue = GetCustomerCookieValue();
                    if (!string.IsNullOrEmpty(customerCookieValue))
                    {
                        if (Guid.TryParse(customerCookieValue, out Guid customerGuid))
                        {
                            var customerByCookie = _customerService.GetCustomerByGuid(customerGuid);
                            if (customerByCookie != null && !customerByCookie.IsRegistered())
                                customer = customerByCookie;
                        }
                    }
                }

                //create guest if not exists
                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    customer = _customerService.InsertGuestCustomer();
                }

                //validation
                if (!customer.Deleted && customer.Active && !customer.RequireReLogin)
                {
                    SetCustomerCookie(customer.CustomerGuid);
                    _cachedCustomer = customer;
                }

                return _cachedCustomer;
            }
            set
            {
                SetCustomerCookie(value.CustomerGuid);
                _cachedCustomer = value;
            }
        }

        /// <summary>
        /// Gets or sets the original customer (in case the current one is impersonated)
        /// </summary>
        public virtual Customer OriginalCustomerIfImpersonated => _originalCustomerIfImpersonated;

        /// <summary>
        /// Gets or sets the current vendor (logged-in manager)
        /// </summary>
        public virtual Vendor CurrentVendor
        {
            get
            {
                if (_cachedVendor != null)
                    return _cachedVendor;

                var currentCustomer = this.CurrentCustomer;
                if (currentCustomer == null)
                    return null;

                var vendor = _vendorService.GetVendorById(currentCustomer.VendorId);
                if (vendor != null && !vendor.Deleted && vendor.Active)
                    _cachedVendor = vendor;

                return _cachedVendor;
            }
        }

        /// <summary>
        /// Get or set current user working language
        /// </summary>
        public virtual Language WorkingLanguage
        {
            get
            {
                if (_cachedLanguage != null)
                    return _cachedLanguage;

                Language detectedLanguage = null;
                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    detectedLanguage = GetLanguageFromUrl();
                }
                if (detectedLanguage == null && _localizationSettings.AutomaticallyDetectLanguage)
                {
                    if (!this.CurrentCustomer.GetAttribute<bool>(SystemCustomerAttributeNames.LanguageAutomaticallyDetected,
                        _genericAttributeService, _storeContext.CurrentStore.Id))
                    {
                        detectedLanguage = GetLanguageFromBrowserSettings();
                        if (detectedLanguage != null)
                        {
                            _genericAttributeService.SaveAttribute(this.CurrentCustomer, SystemCustomerAttributeNames.LanguageAutomaticallyDetected,
                                 true, _storeContext.CurrentStore.Id);
                        }
                    }
                }
                if (detectedLanguage != null)
                {
                    if (this.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId,
                        _genericAttributeService, _storeContext.CurrentStore.Id) != detectedLanguage.Id)
                    {
                        _genericAttributeService.SaveAttribute(this.CurrentCustomer, SystemCustomerAttributeNames.LanguageId,
                            detectedLanguage.Id, _storeContext.CurrentStore.Id);
                    }
                }

                var allLanguages = _languageService.GetAllLanguages(storeId: _storeContext.CurrentStore.Id);
                var languageId = this.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId,
                    _genericAttributeService, _storeContext.CurrentStore.Id);
                var language = allLanguages.FirstOrDefault(x => x.Id == languageId);
                if (language == null)
                {
                    languageId = _storeContext.CurrentStore.DefaultLanguageId;
                    language = allLanguages.FirstOrDefault(x => x.Id == languageId);
                }
                if (language == null)
                    language = allLanguages.FirstOrDefault();
                if (language == null)
                    language = _languageService.GetAllLanguages().FirstOrDefault();

                _cachedLanguage = language;
                return _cachedLanguage;
            }
            set
            {
                var languageId = value != null ? value.Id : 0;
                _genericAttributeService.SaveAttribute(this.CurrentCustomer,
                    SystemCustomerAttributeNames.LanguageId,
                    languageId, _storeContext.CurrentStore.Id);
                _cachedLanguage = null;
            }
        }

        /// <summary>
        /// Get or set current user working currency
        /// </summary>
        public virtual Currency WorkingCurrency
        {
            get
            {
                if (_cachedCurrency != null)
                    return _cachedCurrency;

                if (this.IsAdmin)
                {
                    var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
                    if (primaryStoreCurrency != null)
                    {
                        _cachedCurrency = primaryStoreCurrency;
                        return primaryStoreCurrency;
                    }
                }

                var allCurrencies = _currencyService.GetAllCurrencies(storeId: _storeContext.CurrentStore.Id);
                var currencyId = this.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId,
                    _genericAttributeService, _storeContext.CurrentStore.Id);
                var currency = allCurrencies.FirstOrDefault(x => x.Id == currencyId);
                if (currency == null)
                {
                    currencyId = this.WorkingLanguage.DefaultCurrencyId;
                    currency = allCurrencies.FirstOrDefault(x => x.Id == currencyId);
                }
                if (currency == null)
                    currency = allCurrencies.FirstOrDefault();
                if (currency == null)
                    currency = _currencyService.GetAllCurrencies().FirstOrDefault();

                _cachedCurrency = currency;
                return _cachedCurrency;
            }
            set
            {
                var currencyId = value != null ? value.Id : 0;
                _genericAttributeService.SaveAttribute(this.CurrentCustomer,
                    SystemCustomerAttributeNames.CurrencyId,
                    currencyId, _storeContext.CurrentStore.Id);
                _cachedCurrency = null;
            }
        }

        /// <summary>
        /// Get or set current tax display type
        /// </summary>
        public virtual TaxDisplayType TaxDisplayType
        {
            get
            {
                if (_cachedTaxDisplayType != null)
                    return _cachedTaxDisplayType.Value;

                TaxDisplayType taxDisplayType;
                if (_taxSettings.AllowCustomersToSelectTaxDisplayType && this.CurrentCustomer != null)
                {
                    taxDisplayType = (TaxDisplayType)this.CurrentCustomer.GetAttribute<int>(
                        SystemCustomerAttributeNames.TaxDisplayTypeId,
                        _genericAttributeService,
                        _storeContext.CurrentStore.Id);
                }
                else
                {
                    taxDisplayType = _taxSettings.TaxDisplayType;
                }

                _cachedTaxDisplayType = taxDisplayType;
                return _cachedTaxDisplayType.Value;
            }
            set
            {
                if (!_taxSettings.AllowCustomersToSelectTaxDisplayType)
                    return;

                _genericAttributeService.SaveAttribute(this.CurrentCustomer,
                    SystemCustomerAttributeNames.TaxDisplayTypeId,
                    (int)value, _storeContext.CurrentStore.Id);
                _cachedTaxDisplayType = null;
            }
        }

        /// <summary>
        /// Get or set value indicating whether we're in admin area
        /// </summary>
        public virtual bool IsAdmin { get; set; }

        #endregion
    }
}
