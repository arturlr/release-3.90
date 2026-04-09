namespace Nop.Core.Domain.Security;

public class DefaultPermissionRecord
{
    public string? CustomerRoleSystemName { get; set; }
    public IEnumerable<PermissionRecord> PermissionRecords { get; set; } = [];
}
