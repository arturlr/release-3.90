using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Shipping
{

    public class ProductAvailabilityRange : BaseEntity, ILocalizedEntity
    {

        public string? Name { get; set; }

        public int DisplayOrder { get; set; }
    }
}
