namespace Nop.Core.Domain.Catalog
{

    public class CrossSellProduct : BaseEntity
    {

        public int ProductId1 { get; set; }

        public int ProductId2 { get; set; }
    }

}
