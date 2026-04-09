namespace Nop.Core.Domain.Catalog
{

    public class BackInStockSubscription : BaseEntity
    {

        public int StoreId { get; set; }

        public int ProductId { get; set; }

        public int CustomerId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

    }

}
