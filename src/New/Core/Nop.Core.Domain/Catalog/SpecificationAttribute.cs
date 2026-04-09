using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class SpecificationAttribute : BaseEntity, ILocalizedEntity
    {
        public string? Name { get; set; }

        public int DisplayOrder { get; set; }
}
}
