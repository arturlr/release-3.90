using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class SpecificationAttributeOption : BaseEntity, ILocalizedEntity
    {

        public int SpecificationAttributeId { get; set; }

        public string? Name { get; set; }

        public string? ColorSquaresRgb { get; set; }

        public int DisplayOrder { get; set; }
    }
}
