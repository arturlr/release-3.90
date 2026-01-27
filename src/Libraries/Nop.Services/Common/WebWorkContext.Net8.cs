using System;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Vendors;
using Nop.Core.Domain.Tax;
using Microsoft.AspNetCore.Http;

namespace Nop.Services.Common
{
    /// <summary>
    /// Work context implementation for ASP.NET Core
    /// </summary>
    public class WebWorkContext : IWorkContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Customer? _cachedCustomer;

        public WebWorkContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the current customer
        /// </summary>
        public virtual Customer CurrentCustomer
        {
            get
            {
                if (_cachedCustomer != null)
                    return _cachedCustomer;

                // TODO: Load from authentication
                // For now, return a guest customer
                _cachedCustomer = new Customer
                {
                    Id = 0,
                    CustomerGuid = Guid.NewGuid(),
                    Active = true,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastActivityDateUtc = DateTime.UtcNow
                };

                return _cachedCustomer;
            }
            set => _cachedCustomer = value;
        }

        /// <summary>
        /// Gets the current language
        /// </summary>
        public virtual Language WorkingLanguage
        {
            get
            {
                // TODO: Load from settings/cookie
                return new Language
                {
                    Id = 1,
                    Name = "English",
                    LanguageCulture = "en-US",
                    Published = true
                };
            }
            set { }
        }

        /// <summary>
        /// Gets the current currency
        /// </summary>
        public virtual Currency WorkingCurrency
        {
            get
            {
                // TODO: Load from settings/cookie
                return new Currency
                {
                    Id = 1,
                    Name = "US Dollar",
                    CurrencyCode = "USD",
                    Rate = 1.00M,
                    Published = true
                };
            }
            set { }
        }

        /// <summary>
        /// Gets the current store
        /// </summary>
        public virtual Store CurrentStore
        {
            get
            {
                // TODO: Load from configuration
                return new Store
                {
                    Id = 1,
                    Name = "Your store name",
                    Url = "http://localhost:5001"
                };
            }
        }

        /// <summary>
        /// Gets the original customer (if impersonated)
        /// </summary>
        public virtual Customer OriginalCustomerIfImpersonated => null;

        /// <summary>
        /// Gets the current vendor
        /// </summary>
        public virtual Vendor CurrentVendor => null;

        /// <summary>
        /// Gets the tax display type
        /// </summary>
        public virtual TaxDisplayType TaxDisplayType { get; set; } = TaxDisplayType.IncludingTax;

        /// <summary>
        /// Gets whether current user is admin
        /// </summary>
        public virtual bool IsAdmin { get; set; }
    }
}
