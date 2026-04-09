namespace Nop.Core.Domain.Security;

/// <summary>
/// Join entity for PermissionRecord ↔ CustomerRole many-to-many (table: PermissionRecord_Role_Mapping).
/// </summary>
public class PermissionRecordRoleMapping : BaseEntity
{
    public int PermissionRecordId { get; set; }
    public int CustomerRoleId { get; set; }
}
