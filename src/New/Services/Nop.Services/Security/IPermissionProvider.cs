using Nop.Core.Domain.Security;

namespace Nop.Services.Security;

/// <summary>
/// Permission provider — extension point for plugins to register custom permissions.
/// </summary>
public interface IPermissionProvider
{
    IEnumerable<PermissionRecord> GetPermissions();
    IEnumerable<DefaultPermissionRecord> GetDefaultPermissions();
}
