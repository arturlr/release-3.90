namespace Nop.Core.Domain.Customers
{

    public class RewardPointsHistory : BaseEntity
    {

        public int CustomerId { get; set; }

        public int StoreId { get; set; }

        public int Points { get; set; }

        public int? PointsBalance { get; set; }

        public decimal UsedAmount { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedOnUtc { get; set; }

    }
}
