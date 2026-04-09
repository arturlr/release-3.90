namespace Nop.Core.Domain.Orders
{

    public class OrderAverageReportLine
    {

        public int CountOrders { get; set; }

        public decimal SumShippingExclTax { get; set; }

        public decimal SumTax { get; set; }

        public decimal SumOrders { get; set; }
    }
}
