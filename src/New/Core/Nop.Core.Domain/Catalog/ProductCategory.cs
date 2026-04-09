namespace Nop.Core.Domain.Catalog
{

    public class ProductCategory : BaseEntity
    {

        public int ProductId { get; set; }

        public int CategoryId { get; set; }

        public bool IsFeaturedProduct { get; set; }

        public int DisplayOrder { get; set; }

    }

}
