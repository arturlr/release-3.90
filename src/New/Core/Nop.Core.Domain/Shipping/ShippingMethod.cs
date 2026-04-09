using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Shipping
{

    public class ShippingMethod : BaseEntity, ILocalizedEntity
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public int DisplayOrder { get; set; }
    }
}
