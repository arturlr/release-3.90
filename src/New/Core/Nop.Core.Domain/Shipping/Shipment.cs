namespace Nop.Core.Domain.Shipping
{

    public class Shipment : BaseEntity
    {
        public int OrderId { get; set; }

        public string? TrackingNumber { get; set; }

        public decimal? TotalWeight { get; set; }

        public DateTime? ShippedDateUtc { get; set; }

        public DateTime? DeliveryDateUtc { get; set; }

        public string? AdminComment { get; set; }

        public DateTime CreatedOnUtc { get; set; }
}
}
