namespace Nop.Core.Domain.Shipping
{

    public class ShippingOption
    {

        public string? ShippingRateComputationMethodSystemName { get; set; }

        public decimal Rate { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }
    }

}
