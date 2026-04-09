namespace Nop.Core.Domain.Customers
{

    public class BestCustomerReportLine
    {

        public int CustomerId { get; set; }

        public decimal OrderTotal { get; set; }

        public int OrderCount { get; set; }
    }
}
