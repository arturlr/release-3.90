using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class ProductAttributeValue : BaseEntity, ILocalizedEntity
    {

        public int ProductAttributeMappingId { get; set; }

        public int AttributeValueTypeId { get; set; }

        public int AssociatedProductId { get; set; }

        public string? Name { get; set; }

        public string? ColorSquaresRgb { get; set; }

        public int ImageSquaresPictureId { get; set; }

        public decimal PriceAdjustment { get; set; }

        public decimal WeightAdjustment { get; set; }

        public decimal Cost { get; set; }

        public bool CustomerEntersQty { get; set; }

        public int Quantity { get; set; }

        public bool IsPreSelected { get; set; }

        public int DisplayOrder { get; set; }

        public int PictureId { get; set; }
        public AttributeValueType AttributeValueType
        {
            get
            {
                return (AttributeValueType)AttributeValueTypeId;
            }
            set
            {
                AttributeValueTypeId = (int)value;
            }
        }
    }
}
