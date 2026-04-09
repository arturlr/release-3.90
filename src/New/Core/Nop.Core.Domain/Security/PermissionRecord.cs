namespace Nop.Core.Domain.Security
{

    public class PermissionRecord : BaseEntity
    {
        public string? Name { get; set; }

        public string? SystemName { get; set; }

        public string? Category { get; set; }
}
}
