using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;

namespace Nop.Services.Authentication
{
    /// <summary>
    /// Authentication service
    /// </summary>
    /// <remarks>
    /// Task 4.2 (design section 5). This type was built on <c>System.Web.Security</c>
    /// forms authentication, which has NO ASP.NET Core counterpart:
    /// <c>FormsAuthentication</c>, <c>FormsAuthenticationTicket</c> and
    /// <c>FormsIdentity</c> were all deleted from the platform. ASP.NET Core replaces the
    /// encrypted forms-auth ticket with **cookie authentication over a claims principal**
    /// (<see cref="CookieAuthenticationDefaults"/> /
    /// <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, string, ClaimsPrincipal, AuthenticationProperties)"/>).
    ///
    /// The <see cref="IAuthenticationService"/> surface is UNCHANGED so all eight
    /// call sites in <c>Nop.Web/Controllers/CustomerController.cs</c> and the read in
    /// <c>Nop.Web.Framework/WebWorkContext.cs</c> compile untouched.
    ///
    /// SEMANTIC DIFFERENCES - see the "FormsAuthentication" entry in
    /// runtime-deferrals.md. In short:
    ///   * the ticket's <c>UserData</c> field is now the claim named
    ///     <see cref="UsernameOrEmailClaimType"/> on the signed-in principal;
    ///   * ticket encryption/validation is performed by the cookie authentication
    ///     handler's data protection, not by <c>FormsAuthentication.Encrypt</c>;
    ///   * cookie name / path / domain / RequireSSL / timeout are no longer readable from
    ///     this class - they are host-side <c>CookieAuthenticationOptions</c> and MUST be
    ///     configured in task 6.4/7.2. Nothing here weakens them: HttpOnly and
    ///     SameSite defaults come from the handler, and <c>Secure</c> is the handler's
    ///     <c>CookieSecurePolicy</c>.
    /// </remarks>
    public partial class FormsAuthenticationService : IAuthenticationService
    {
        #region Constants

        /// <summary>
        /// The claim that carries the username-or-email previously stored in
        /// <c>FormsAuthenticationTicket.UserData</c>.
        /// </summary>
        public const string UsernameOrEmailClaimType = "Nop.Customer.UsernameOrEmail";

        /// <summary>
        /// Issuer recorded on the claims this service mints. Mirrors the "this ticket is
        /// ours" role that <c>FormsIdentity</c> played in the check inside
        /// <see cref="GetAuthenticatedCustomer"/>.
        /// </summary>
        public const string ClaimsIssuer = "Nop.Services.Authentication";

        #endregion

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
            //System.Web read this from <forms timeout="..."/> in web.config. There is no
            //ambient equivalent; the authoritative value now lives in
            //CookieAuthenticationOptions.ExpireTimeSpan on the host. 30 minutes is the
            //ASP.NET Framework forms-auth default and is restated here so the
            //AuthenticationProperties this class emits are never open-ended.
            this._expirationTimeSpan = TimeSpan.FromMinutes(30);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the current HTTP context, or null when there is no current request
        /// </summary>
        protected virtual HttpContext HttpContext
        {
            get { return _httpContextAccessor == null ? null : _httpContextAccessor.HttpContext; }
        }

        /// <summary>
        /// Gets the authentication scheme used to sign customers in and out.
        /// </summary>
        protected virtual string AuthenticationScheme
        {
            get { return CookieAuthenticationDefaults.AuthenticationScheme; }
        }

        /// <summary>
        /// Get authenticated customer from a signed-in claims principal
        /// </summary>
        /// <param name="principal">Claims principal</param>
        /// <returns>Customer</returns>
        /// <remarks>
        /// Replaces <c>GetAuthenticatedCustomerFromTicket(FormsAuthenticationTicket)</c>.
        /// The username-or-email that used to travel in the ticket's <c>UserData</c> is
        /// read from the <see cref="UsernameOrEmailClaimType"/> claim, falling back to the
        /// principal's <see cref="ClaimsIdentity.Name"/> (which this service also sets, and
        /// which is what the ticket's <c>Name</c> field held).
        /// </remarks>
        protected virtual Customer GetAuthenticatedCustomerFromPrincipal(ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException("principal");

            var claim = principal.Claims.FirstOrDefault(x => x.Type == UsernameOrEmailClaimType);
            var usernameOrEmail = claim != null ? claim.Value : principal.Identity.Name;

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
            var httpContext = HttpContext;
            if (httpContext == null)
                return;

            var usernameOrEmail = _customerSettings.UsernamesEnabled ? customer.Username : customer.Email;
            var now = DateTime.UtcNow;

            //the two claims below carry exactly what FormsAuthenticationTicket carried:
            //Name (ticket.Name) and UserData (ticket.UserData). In 3.90 both held the same
            //username-or-email value, and that is preserved.
            var identity = new ClaimsIdentity(AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, usernameOrEmail, ClaimValueTypes.String, ClaimsIssuer));
            identity.AddClaim(new Claim(UsernameOrEmailClaimType, usernameOrEmail, ClaimValueTypes.String, ClaimsIssuer));

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = createPersistentCookie,
                IssuedUtc = now,
                //FormsAuthenticationTicket.Expiration was always set; the cookie itself only
                //carried it when the ticket was persistent. AuthenticationProperties
                //reproduces that: ExpiresUtc drives ticket lifetime, IsPersistent drives
                //whether the browser keeps the cookie past the session.
                ExpiresUtc = now.Add(_expirationTimeSpan)
            };

            //the interface is synchronous (8 call sites in Nop.Web) and SignInAsync is not.
            //ASP.NET Core installs no SynchronizationContext, so blocking here cannot
            //deadlock - it only occupies the request thread. Recorded as a runtime deferral.
            httpContext.SignInAsync(AuthenticationScheme, principal, properties)
                .GetAwaiter().GetResult();

            _cachedCustomer = customer;
        }

        /// <summary>
        /// Sign out
        /// </summary>
        public virtual void SignOut()
        {
            _cachedCustomer = null;

            var httpContext = HttpContext;
            if (httpContext == null)
                return;

            //replaces FormsAuthentication.SignOut(), which removed the forms-auth cookie.
            httpContext.SignOutAsync(AuthenticationScheme).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get authenticated customer
        /// </summary>
        /// <returns>Customer</returns>
        public virtual Customer GetAuthenticatedCustomer()
        {
            if (_cachedCustomer != null)
                return _cachedCustomer;

            var httpContext = HttpContext;
            if (httpContext == null)
                return null;

            var principal = httpContext.User;

            //"Request.IsAuthenticated" plus "User.Identity is FormsIdentity" becomes
            //"there is an authenticated identity issued by the scheme we sign in with".
            //Checking the authentication type keeps the original intent - a principal
            //established by some OTHER handler is not treated as a nopCommerce customer.
            if (principal == null ||
                principal.Identity == null ||
                !principal.Identity.IsAuthenticated ||
                !String.Equals(principal.Identity.AuthenticationType, AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var customer = GetAuthenticatedCustomerFromPrincipal(principal);
            if (customer != null && customer.Active && !customer.RequireReLogin && !customer.Deleted && customer.IsRegistered())
                _cachedCustomer = customer;
            return _cachedCustomer;
        }

        #endregion

    }
}
