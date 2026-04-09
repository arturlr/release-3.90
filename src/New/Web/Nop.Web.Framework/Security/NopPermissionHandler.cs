using Microsoft.AspNetCore.Authorization;
using Nop.Services.Security;

namespace Nop.Web.Framework.Security;

/// <summary>
/// Handles NopPermissionRequirement by delegating to IPermissionService.
/// Checks if the current customer (via IWorkContext) has the requested permission.
/// </summary>
public class NopPermissionHandler(IPermissionService permissionService) : AuthorizationHandler<NopPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NopPermissionRequirement requirement)
    {
        if (permissionService.Authorize(requirement.PermissionSystemName))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
