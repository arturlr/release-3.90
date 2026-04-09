namespace Nop.Core.Domain.Catalog
{

    public class ProductSpecificationAttribute : BaseEntity
    {

        public int ProductId { get; set; }

        public int AttributeTypeId { get; set; }

        public int SpecificationAttributeOptionId { get; set; }

        public string? CustomValue { get; set; }

        public bool AllowFiltering { get; set; }

        public bool ShowOnProductPage { get; set; }

        public int DisplayOrder { get; set; }

        public SpecificationAttributeType AttributeType
        {
            get
            {
                return (SpecificationAttributeType)AttributeTypeId;
            }
            set
            {
                AttributeTypeId = (int)value;
            }
        }
    }
}
