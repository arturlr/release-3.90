namespace Nop.Core.Domain.Orders
{

    public class RecurringPaymentHistory : BaseEntity
    {

        public int RecurringPaymentId { get; set; }

        public int OrderId { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
