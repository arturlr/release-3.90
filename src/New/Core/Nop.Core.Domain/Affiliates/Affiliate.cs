namespace Nop.Core.Domain.Affiliates
{

    public class Affiliate : BaseEntity
    {

        public int AddressId { get; set; }

        public string? AdminComment { get; set; }

        public string? FriendlyUrlName { get; set; }

        public bool Deleted { get; set; }

        public bool Active { get; set; }
    }
}
