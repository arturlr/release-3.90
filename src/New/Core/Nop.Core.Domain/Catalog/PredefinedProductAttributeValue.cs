using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Catalog
{

    public class PredefinedProductAttributeValue : BaseEntity, ILocalizedEntity
    {

        public int ProductAttributeId { get; set; }

        public string? Name { get; set; }

        public decimal PriceAdjustment { get; set; }

        public decimal WeightAdjustment { get; set; }

        public decimal Cost { get; set; }

        public bool IsPreSelected { get; set; }

        public int DisplayOrder { get; set; }
    }
}
