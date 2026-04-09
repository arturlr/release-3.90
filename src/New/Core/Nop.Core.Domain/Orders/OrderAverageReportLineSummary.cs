namespace Nop.Core.Domain.Orders
{

    public class OrderAverageReportLineSummary
    {

        public OrderStatus OrderStatus { get; set; }

        public decimal SumTodayOrders { get; set; }

        public int CountTodayOrders { get; set; }

        public decimal SumThisWeekOrders { get; set; }

        public int CountThisWeekOrders { get; set; }

        public decimal SumThisMonthOrders { get; set; }

        public int CountThisMonthOrders { get; set; }

        public decimal SumThisYearOrders { get; set; }

        public int CountThisYearOrders { get; set; }

        public decimal SumAllTimeOrders { get; set; }

        public int CountAllTimeOrders { get; set; }
    }
}
