using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Authentication;

/// <summary>
/// Cookie-based authentication service using ASP.NET Core authentication middleware.
/// Replaces legacy FormsAuthenticationService.
/// </summary>
public class CookieAuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<CustomerRole> _customerRoleRepository;

    private Customer? _cachedCustomer;

    public CookieAuthenticationService(
        IHttpContextAccessor httpContextAccessor,
        IRepository<Customer> customerRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<CustomerRole> customerRoleRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _customerRepository = customerRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _customerRoleRepository = customerRoleRepository;
    }

    public async Task SignInAsync(Customer customer, bool isPersistent)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var claims = new List<Claim>
        {
            new(NopAuthenticationDefaults.CustomerGuidClaimType, customer.CustomerGuid.ToString())
        };

        var identity = new ClaimsIdentity(claims, NopAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            IssuedUtc = DateTimeOffset.UtcNow
        };

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        await httpContext.SignInAsync(NopAuthenticationDefaults.AuthenticationScheme, principal, properties);
        _cachedCustomer = customer;
    }

    public async Task SignOutAsync()
    {
        _cachedCustomer = null;

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        await httpContext.SignOutAsync(NopAuthenticationDefaults.AuthenticationScheme);
    }

    public Task<Customer?> GetAuthenticatedCustomerAsync()
    {
        if (_cachedCustomer is not null)
            return Task.FromResult<Customer?>(_cachedCustomer);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return Task.FromResult<Customer?>(null);

        var guidClaim = httpContext.User.FindFirst(NopAuthenticationDefaults.CustomerGuidClaimType);
        if (guidClaim is null || !Guid.TryParse(guidClaim.Value, out var customerGuid))
            return Task.FromResult<Customer?>(null);

        var customer = _customerRepository.Table
            .FirstOrDefault(c => c.CustomerGuid == customerGuid);

        if (customer is null || !customer.Active || customer.RequireReLogin || customer.Deleted)
            return Task.FromResult<Customer?>(null);

        // Verify customer has the Registered role
        var isRegistered = (
            from crm in _customerRoleMappingRepository.TableNoTracking
            join cr in _customerRoleRepository.TableNoTracking on crm.CustomerRoleId equals cr.Id
            where crm.CustomerId == customer.Id
                  && cr.Active
                  && cr.SystemName == SystemCustomerRoleNames.Registered
            select crm.Id
        ).Any();

        if (!isRegistered)
            return Task.FromResult<Customer?>(null);

        _cachedCustomer = customer;
        return Task.FromResult<Customer?>(_cachedCustomer);
    }
}
