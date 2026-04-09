namespace Nop.Core.Domain.Security
{

    public class AclRecord : BaseEntity
    {

        public int EntityId { get; set; }

        public string? EntityName { get; set; }

        public int CustomerRoleId { get; set; }
    }
}
