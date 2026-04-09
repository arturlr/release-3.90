using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Orders
{

    public class CheckoutAttributeValue : BaseEntity, ILocalizedEntity
    {

        public int CheckoutAttributeId { get; set; }

        public string? Name { get; set; }

        public string? ColorSquaresRgb { get; set; }

        public decimal PriceAdjustment { get; set; }

        public decimal WeightAdjustment { get; set; }

        public bool IsPreSelected { get; set; }

        public int DisplayOrder { get; set; }
    }

}
