namespace Nop.Core.Domain.Stores
{

    public class StoreMapping : BaseEntity
    {

        public int EntityId { get; set; }

        public string? EntityName { get; set; }

        public int StoreId { get; set; }
    }
}
