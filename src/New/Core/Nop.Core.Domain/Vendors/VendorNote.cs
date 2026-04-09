namespace Nop.Core.Domain.Vendors
{

    public class VendorNote : BaseEntity
    {

        public int VendorId { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }

}
