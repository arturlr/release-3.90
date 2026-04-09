using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Shipping
{

    public class DeliveryDate : BaseEntity, ILocalizedEntity
    {

        public string? Name { get; set; }

        public int DisplayOrder { get; set; }
    }
}
