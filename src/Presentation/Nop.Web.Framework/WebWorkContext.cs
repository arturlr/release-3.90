using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Fakes;
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
    /// <remarks>
    /// Task 6.2: re-based from <c>System.Web.HttpContextBase</c> onto
    /// <see cref="IHttpContextAccessor"/>.
    /// <list type="bullet">
    /// <item>BREAKING CONSTRUCTOR CHANGE: <c>WebWorkContext(HttpContextBase, …)</c> -&gt;
    /// <c>WebWorkContext(IHttpContextAccessor, …)</c>. This matches the six Nop.Services
    /// constructors already moved in task 4.2 (runtime-deferrals section 9a) and Nop.Core's
    /// <c>WebHelper</c>/<c>PerRequestCacheManager</c> (section 3). <c>DependencyRegistrar</c>
    /// registers this type reflectively (<c>RegisterType&lt;WebWorkContext&gt;</c>), so Autofac
    /// picks the new constructor up automatically PROVIDED task 6.4 calls
    /// <c>services.AddHttpContextAccessor()</c> - deferral 7.14.</item>
    /// <item>COOKIES: <c>HttpCookie</c> / <c>HttpCookieCollection</c> have no ASP.NET Core
    /// counterpart. Reading is via <c>Request.Cookies</c>
    /// (<see cref="IRequestCookieCollection"/>, a flat string map) and writing via
    /// <c>Response.Cookies.Append/Delete</c> (<see cref="IResponseCookies"/>). The cookie name
    /// (<c>Nop.customer</c>), the <c>HttpOnly</c> flag and the 24*365-hour lifetime are
    /// preserved. <c>Response.Cookies.Remove(name)</c> + <c>Add(cookie)</c> becomes
    /// <c>Delete(name)</c> + <c>Append(name, value, options)</c>. The "expire in the past to
    /// clear" branch for <see cref="Guid.Empty"/> is expressed as a <c>Delete</c>, which is what
    /// ASP.NET Core emits for that case anyway (an empty value with a past expiry).</item>
    /// <item><c>Request.AppRelativeCurrentExecutionFilePath</c> ("~/en/...") -&gt; rebuilt as
    /// <c>"~" + Request.Path</c>; <c>Request.ApplicationPath</c> -&gt; <c>Request.PathBase</c>
    /// mapped to "/" when empty, because <c>LocalizedUrlExtenstions</c> treats "/" as "not a
    /// virtual directory" and throws on an empty string.</item>
    /// <item><c>Request.UserLanguages</c> (a pre-sorted string[]) -&gt; the
    /// <c>Accept-Language</c> header parsed with
    /// <see cref="Microsoft.Net.Http.Headers.StringWithQualityHeaderValue"/> and ordered by
    /// quality, which is what System.Web did internally. Only the first entry is used, exactly as
    /// before.</item>
    /// <item>The <c>_httpContext is FakeHttpContext</c> background-task test is PRESERVED - see
    /// the FakeHttpContext note in runtime-deferrals section 3; the type was re-implemented over a
    /// composed <c>DefaultHttpContext</c> specifically so this keeps working. It now tests the
    /// resolved <see cref="HttpContext"/> rather than the injected wrapper.</item>
    /// </list>
    /// </remarks>
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

        /// <summary>
        /// The current ASP.NET Core <see cref="HttpContext"/>, or null outside a request
        /// (scheduled tasks, installation)
        /// </summary>
        protected virtual HttpContext HttpContext
        {
            get { return _httpContextAccessor != null ? _httpContextAccessor.HttpContext : null; }
        }

        protected virtual string GetCustomerCookie()
        {
            var httpContext = HttpContext;
            if (httpContext == null || httpContext.Request == null)
                return null;

            return httpContext.Request.Cookies[CustomerCookieName];
        }

        protected virtual void SetCustomerCookie(Guid customerGuid)
        {
            var httpContext = HttpContext;
            if (httpContext != null && httpContext.Response != null && !httpContext.Response.HasStarted)
            {
                //remove the existing cookie first (System.Web did Cookies.Remove + Cookies.Add)
                httpContext.Response.Cookies.Delete(CustomerCookieName);

                if (customerGuid == Guid.Empty)
                {
                    //nothing to write - Delete() already emits an expired cookie, which is what
                    //the old "Expires = DateTime.Now.AddMonths(-1)" branch produced
                    return;
                }

                int cookieExpires = 24*365; //TODO make configurable
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddHours(cookieExpires)
                };
                httpContext.Response.Cookies.Append(CustomerCookieName, customerGuid.ToString(), options);
            }
        }

        protected virtual Language GetLanguageFromUrl()
        {
            var httpContext = HttpContext;
            if (httpContext == null || httpContext.Request == null)
                return null;

            //System.Web's AppRelativeCurrentExecutionFilePath was "~" + the app-relative path
            string virtualPath = "~" + httpContext.Request.Path.Value;
            string applicationPath = httpContext.Request.PathBase.HasValue
                ? httpContext.Request.PathBase.Value
                : "/";
            if (!virtualPath.IsLocalizedUrl(applicationPath, false))
                return null;

            var seoCode = virtualPath.GetLanguageSeoCodeFromUrl(applicationPath, false);
            if (String.IsNullOrEmpty(seoCode))
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
            var httpContext = HttpContext;
            if (httpContext == null || httpContext.Request == null)
                return null;

            //System.Web's Request.UserLanguages was the Accept-Language header split and
            //ordered by quality; reproduce that here
            var acceptLanguage = httpContext.Request.GetTypedHeaders().AcceptLanguage;
            if (acceptLanguage == null || !acceptLanguage.Any())
                return null;

            var userLanguage = acceptLanguage
                .OrderByDescending(x => x.Quality ?? 1)
                .Select(x => x.Value.HasValue ? x.Value.Value : null)
                .FirstOrDefault(x => !String.IsNullOrEmpty(x));
            if (String.IsNullOrEmpty(userLanguage))
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
                var httpContext = HttpContext;
                if (httpContext == null || httpContext is FakeHttpContext)
                {
                    //check whether request is made by a background task
                    //in this case return built-in customer record for background task
                    customer = _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);
                }

                //check whether request is made by a search engine
                //in this case return built-in customer record for search engines 
                //or comment the following two lines of code in order to disable this functionality
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

                //impersonate user if required (currently used for 'phone order' support)
                if (customer != null && !customer.Deleted && customer.Active && !customer.RequireReLogin)
                {
                    var impersonatedCustomerId = customer.GetAttribute<int?>(SystemCustomerAttributeNames.ImpersonatedCustomerId);
                    if (impersonatedCustomerId.HasValue && impersonatedCustomerId.Value > 0)
                    {
                        var impersonatedCustomer = _customerService.GetCustomerById(impersonatedCustomerId.Value);
                        if (impersonatedCustomer != null && !impersonatedCustomer.Deleted && impersonatedCustomer.Active && !impersonatedCustomer.RequireReLogin)
                        {
                            //set impersonated customer
                            _originalCustomerIfImpersonated = customer;
                            customer = impersonatedCustomer;
                        }
                    }
                }

                //load guest customer
                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    var customerCookie = GetCustomerCookie();
                    if (!String.IsNullOrEmpty(customerCookie))
                    {
                        Guid customerGuid;
                        if (Guid.TryParse(customerCookie, out customerGuid))
                        {
                            var customerByCookie = _customerService.GetCustomerByGuid(customerGuid);
                            if (customerByCookie != null &&
                                //this customer (from cookie) should not be registered
                                !customerByCookie.IsRegistered())
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
        public virtual Customer OriginalCustomerIfImpersonated
        {
            get
            {
                return _originalCustomerIfImpersonated;
            }
        }

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

                //validation
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
                    //get language from URL
                    detectedLanguage = GetLanguageFromUrl();
                }
                if (detectedLanguage == null && _localizationSettings.AutomaticallyDetectLanguage)
                {
                    //get language from browser settings
                    //but we do it only once
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
                    //the language is detected. now we need to save it
                    if (this.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId,
                        _genericAttributeService, _storeContext.CurrentStore.Id) != detectedLanguage.Id)
                    {
                        _genericAttributeService.SaveAttribute(this.CurrentCustomer, SystemCustomerAttributeNames.LanguageId,
                            detectedLanguage.Id, _storeContext.CurrentStore.Id);
                    }
                }

                var allLanguages = _languageService.GetAllLanguages(storeId: _storeContext.CurrentStore.Id);
                //find current customer language
                var languageId = this.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.LanguageId,
                    _genericAttributeService, _storeContext.CurrentStore.Id);
                var language = allLanguages.FirstOrDefault(x => x.Id == languageId);
                if (language == null)
                {
                    //it not found, then let's load the default currency for the current language (if specified)
                    languageId = _storeContext.CurrentStore.DefaultLanguageId;
                    language = allLanguages.FirstOrDefault(x => x.Id == languageId);
                }
                if (language == null)
                {
                    //it not specified, then return the first (filtered by current store) found one
                    language = allLanguages.FirstOrDefault();
                }
                if (language == null)
                {
                    //it not specified, then return the first found one
                    language = _languageService.GetAllLanguages().FirstOrDefault();
                }

                //cache
                _cachedLanguage = language;
                return _cachedLanguage;
            }
            set
            {
                var languageId = value != null ? value.Id : 0;
                _genericAttributeService.SaveAttribute(this.CurrentCustomer,
                    SystemCustomerAttributeNames.LanguageId,
                    languageId, _storeContext.CurrentStore.Id);

                //reset cache
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
                
                //return primary store currency when we're in admin area/mode
                if (this.IsAdmin)
                {
                    var primaryStoreCurrency =  _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
                    if (primaryStoreCurrency != null)
                    {
                        //cache
                        _cachedCurrency = primaryStoreCurrency;
                        return primaryStoreCurrency;
                    }
                }

                var allCurrencies = _currencyService.GetAllCurrencies(storeId: _storeContext.CurrentStore.Id);
                //find a currency previously selected by a customer
                var currencyId = this.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.CurrencyId,
                    _genericAttributeService, _storeContext.CurrentStore.Id);
                var currency = allCurrencies.FirstOrDefault(x => x.Id == currencyId);
                if (currency == null)
                {
                    //it not found, then let's load the default currency for the current language (if specified)
                    currencyId = this.WorkingLanguage.DefaultCurrencyId;
                    currency = allCurrencies.FirstOrDefault(x => x.Id == currencyId);
                }
                if (currency == null)
                {
                    //it not found, then return the first (filtered by current store) found one
                    currency = allCurrencies.FirstOrDefault();
                }
                if (currency == null)
                {
                    //it not specified, then return the first found one
                    currency = _currencyService.GetAllCurrencies().FirstOrDefault();
                }

                //cache
                _cachedCurrency = currency;
                return _cachedCurrency;
            }
            set
            {
                var currencyId = value != null ? value.Id : 0;
                _genericAttributeService.SaveAttribute(this.CurrentCustomer,
                    SystemCustomerAttributeNames.CurrencyId,
                    currencyId, _storeContext.CurrentStore.Id);

                //reset cache
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
                //cache
                if (_cachedTaxDisplayType != null)
                    return _cachedTaxDisplayType.Value;

                TaxDisplayType taxDisplayType;
                if (_taxSettings.AllowCustomersToSelectTaxDisplayType && this.CurrentCustomer != null)
                {
                    taxDisplayType = (TaxDisplayType) this.CurrentCustomer.GetAttribute<int>(
                        SystemCustomerAttributeNames.TaxDisplayTypeId,
                        _genericAttributeService,
                        _storeContext.CurrentStore.Id);
                }
                else
                {
                    taxDisplayType = _taxSettings.TaxDisplayType;
                }

                //cache
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

                //reset cache
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
