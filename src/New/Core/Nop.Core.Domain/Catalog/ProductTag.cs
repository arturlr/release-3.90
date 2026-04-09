using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class ProductTag : BaseEntity, ILocalizedEntity
    {
        public string? Name { get; set; }
    }
}
