namespace Nop.Core.Domain.Catalog
{

    public class RelatedProduct : BaseEntity
    {

        public int ProductId1 { get; set; }

        public int ProductId2 { get; set; }

        public int DisplayOrder { get; set; }
    }

}
