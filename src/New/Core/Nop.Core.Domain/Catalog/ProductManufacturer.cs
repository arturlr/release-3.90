namespace Nop.Core.Domain.Catalog
{

    public class ProductManufacturer : BaseEntity
    {

        public int ProductId { get; set; }

        public int ManufacturerId { get; set; }

        public bool IsFeaturedProduct { get; set; }

        public int DisplayOrder { get; set; }

    }

}
