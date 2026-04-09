using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Orders
{

    public class CheckoutAttribute : BaseEntity, ILocalizedEntity, IStoreMappingSupported
    {
        public string? Name { get; set; }

        public string? TextPrompt { get; set; }

        public bool IsRequired { get; set; }

        public bool ShippableProductRequired { get; set; }

        public bool IsTaxExempt { get; set; }

        public int TaxCategoryId { get; set; }

        public int AttributeControlTypeId { get; set; }

        public int DisplayOrder { get; set; }

        public bool LimitedToStores { get; set; }

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
