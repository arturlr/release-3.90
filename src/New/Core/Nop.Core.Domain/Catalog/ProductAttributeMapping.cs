using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class ProductAttributeMapping : BaseEntity, ILocalizedEntity
    {
        public int ProductId { get; set; }

        public int ProductAttributeId { get; set; }

        public string? TextPrompt { get; set; }

        public bool IsRequired { get; set; }

        public int AttributeControlTypeId { get; set; }

        public int DisplayOrder { get; set; }

        //validation fields

        public int? ValidationMinLength { get; set; }

        public int? ValidationMaxLength { get; set; }

        public string? ValidationFileAllowedExtensions { get; set; }

        public int? ValidationFileMaximumSize { get; set; }

        public string? DefaultValue { get; set; }

        public string? ConditionAttributeXml { get; set; }

        public AttributeControlType AttributeControlType
        {
            get
            {
                return (AttributeControlType)AttributeControlTypeId;
            }
            set
            {
                AttributeControlTypeId = (int)value;
            }
        }

    }

}
