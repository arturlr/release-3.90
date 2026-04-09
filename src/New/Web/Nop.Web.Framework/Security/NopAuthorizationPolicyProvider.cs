using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Nop.Web.Framework.Security;

/// <summary>
/// Dynamic authorization policy provider that creates a policy per permission system name.
/// Any [Authorize(Policy = "ManageProducts")] resolves to a NopPermissionRequirement("ManageProducts").
/// Falls back to the default provider for non-permission policies.
/// </summary>
public class NopAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public NopAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new NopPermissionRequirement(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
