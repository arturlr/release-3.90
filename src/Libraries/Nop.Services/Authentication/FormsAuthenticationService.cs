using System;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;

namespace Nop.Services.Authentication
{
    public partial class FormsAuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;
        private Customer _cachedCustomer;

        private const string CustomerCookieName = ".Nop.Customer";

        public FormsAuthenticationService(IHttpContextAccessor httpContextAccessor,
            ICustomerService customerService, CustomerSettings customerSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _customerService = customerService;
            _customerSettings = customerSettings;
        }

        public virtual void SignIn(Customer customer, bool createPersistentCookie)
        {
            var usernameOrEmail = _customerSettings.UsernamesEnabled ? customer.Username : customer.Email;
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };
            
            if (createPersistentCookie)
            {
                cookieOptions.Expires = DateTime.UtcNow.AddDays(365);
            }

            _httpContextAccessor.HttpContext?.Response.Cookies.Append(CustomerCookieName, usernameOrEmail, cookieOptions);
            _cachedCustomer = customer;
        }

        public virtual void SignOut()
        {
            _cachedCustomer = null;
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(CustomerCookieName);
        }

        public virtual Customer GetAuthenticatedCustomer()
        {
            if (_cachedCustomer != null)
                return _cachedCustomer;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            if (!httpContext.Request.Cookies.TryGetValue(CustomerCookieName, out var usernameOrEmail))
                return null;

            if (string.IsNullOrWhiteSpace(usernameOrEmail))
                return null;

            var customer = _customerSettings.UsernamesEnabled
                ? _customerService.GetCustomerByUsername(usernameOrEmail)
                : _customerService.GetCustomerByEmail(usernameOrEmail);

            if (customer != null && customer.Active && !customer.RequireReLogin && !customer.Deleted && customer.IsRegistered())
                _cachedCustomer = customer;

            return _cachedCustomer;
        }
    }
}
