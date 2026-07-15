using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;

namespace Nop.Services.Authentication
{
    /// <summary>
    /// Authentication service (cookie-based for ASP.NET Core)
    /// </summary>
    public partial class FormsAuthenticationService : IAuthenticationService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;
        private readonly TimeSpan _expirationTimeSpan;

        private Customer _cachedCustomer;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <param name="customerService">Customer service</param>
        /// <param name="customerSettings">Customer settings</param>
        public FormsAuthenticationService(IHttpContextAccessor httpContextAccessor,
            ICustomerService customerService, CustomerSettings customerSettings)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._customerService = customerService;
            this._customerSettings = customerSettings;
            this._expirationTimeSpan = TimeSpan.FromDays(14);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get authenticated customer from claims
        /// </summary>
        /// <param name="usernameOrEmail">Username or email from claim</param>
        /// <returns>Customer</returns>
        protected virtual Customer GetAuthenticatedCustomerFromClaim(string usernameOrEmail)
        {
            if (String.IsNullOrWhiteSpace(usernameOrEmail))
                return null;

            var customer = _customerSettings.UsernamesEnabled
                ? _customerService.GetCustomerByUsername(usernameOrEmail)
                : _customerService.GetCustomerByEmail(usernameOrEmail);
            return customer;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sign in
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="createPersistentCookie">A value indicating whether to create a persistent cookie</param>
        public virtual void SignIn(Customer customer, bool createPersistentCookie)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var usernameOrEmail = _customerSettings.UsernamesEnabled ? customer.Username : customer.Email;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usernameOrEmail ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = createPersistentCookie,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(_expirationTimeSpan)
            };

            httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties).GetAwaiter().GetResult();
            _cachedCustomer = customer;
        }

        /// <summary>
        /// Sign out
        /// </summary>
        public virtual void SignOut()
        {
            _cachedCustomer = null;
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get authenticated customer
        /// </summary>
        /// <returns>Customer</returns>
        public virtual Customer GetAuthenticatedCustomer()
        {
            if (_cachedCustomer != null)
                return _cachedCustomer;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User == null || !httpContext.User.Identity.IsAuthenticated)
            {
                return null;
            }

            var usernameOrEmail = httpContext.User.FindFirstValue(ClaimTypes.Name);
            var customer = GetAuthenticatedCustomerFromClaim(usernameOrEmail);

            if (customer != null && customer.Active && !customer.RequireReLogin && !customer.Deleted && customer.IsRegistered())
                _cachedCustomer = customer;

            return _cachedCustomer;
        }

        #endregion
    }
}
