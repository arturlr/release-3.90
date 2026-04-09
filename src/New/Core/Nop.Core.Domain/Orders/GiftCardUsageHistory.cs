namespace Nop.Core.Domain.Orders
{

    public class GiftCardUsageHistory : BaseEntity
    {

        public int GiftCardId { get; set; }

        public int UsedWithOrderId { get; set; }

        public decimal UsedValue { get; set; }

        public DateTime CreatedOnUtc { get; set; }

    }
}
