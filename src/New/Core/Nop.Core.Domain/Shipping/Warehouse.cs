namespace Nop.Core.Domain.Shipping
{

    public class Warehouse : BaseEntity
    {

        public string? Name { get; set; }

        public string? AdminComment { get; set; }

        public int AddressId { get; set; }
    }
}
