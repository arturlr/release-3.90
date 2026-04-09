using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class ProductAttribute : BaseEntity, ILocalizedEntity
    {

        public string? Name { get; set; }

        public string? Description { get; set; }
    }
}
