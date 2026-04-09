using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Common
{

    public class AddressAttribute : BaseEntity, ILocalizedEntity
    {
        public string? Name { get; set; }

        public bool IsRequired { get; set; }

        public int AttributeControlTypeId { get; set; }

        public int DisplayOrder { get; set; }

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
