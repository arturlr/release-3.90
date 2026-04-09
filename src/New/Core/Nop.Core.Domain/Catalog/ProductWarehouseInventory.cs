namespace Nop.Core.Domain.Catalog
{

    public class ProductWarehouseInventory : BaseEntity
    {

        public int ProductId { get; set; }

        public int WarehouseId { get; set; }

        public int StockQuantity { get; set; }

        public int ReservedQuantity { get; set; }

    }
}
