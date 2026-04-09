namespace Nop.Core.Domain.Catalog
{

    public class StockQuantityHistory : BaseEntity
    {

        public int QuantityAdjustment { get; set; }

        public int StockQuantity { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public int ProductId { get; set; }

        public int? CombinationId { get; set; }

        public int? WarehouseId { get; set; }
    }
}
