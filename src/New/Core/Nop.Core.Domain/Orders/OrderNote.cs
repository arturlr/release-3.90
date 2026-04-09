namespace Nop.Core.Domain.Orders
{

    public class OrderNote : BaseEntity
    {

        public int OrderId { get; set; }

        public string? Note { get; set; }

        public int DownloadId { get; set; }

        public bool DisplayToCustomer { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }

}
