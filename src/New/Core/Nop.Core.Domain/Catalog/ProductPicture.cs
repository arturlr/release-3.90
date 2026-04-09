namespace Nop.Core.Domain.Catalog
{

    public class ProductPicture : BaseEntity
    {

        public int ProductId { get; set; }

        public int PictureId { get; set; }

        public int DisplayOrder { get; set; }

    }

}
