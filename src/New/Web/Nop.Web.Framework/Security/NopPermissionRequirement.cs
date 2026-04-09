using Microsoft.AspNetCore.Authorization;

namespace Nop.Web.Framework.Security;

/// <summary>
/// Authorization requirement for a nopCommerce permission system name.
/// Used with dynamic policy provider to create policies like [Authorize(Policy = "ManageProducts")].
/// </summary>
public class NopPermissionRequirement(string permissionSystemName) : IAuthorizationRequirement
{
    public string PermissionSystemName { get; } = permissionSystemName;
}
