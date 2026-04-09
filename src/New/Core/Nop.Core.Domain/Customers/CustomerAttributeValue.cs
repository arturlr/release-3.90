using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Customers
{

    public class CustomerAttributeValue : BaseEntity, ILocalizedEntity
    {

        public int CustomerAttributeId { get; set; }

        public string? Name { get; set; }

        public bool IsPreSelected { get; set; }

        public int DisplayOrder { get; set; }
    }

}
