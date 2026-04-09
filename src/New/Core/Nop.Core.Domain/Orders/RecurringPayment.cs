using Nop.Core.Domain.Catalog;

namespace Nop.Core.Domain.Orders;

public class RecurringPayment : BaseEntity
{
    public int CycleLength { get; set; }
    public int CyclePeriodId { get; set; }
    public int TotalCycles { get; set; }
    public DateTime StartDateUtc { get; set; }
    public bool IsActive { get; set; }
    public bool LastPaymentFailed { get; set; }
    public bool Deleted { get; set; }
    public int InitialOrderId { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public RecurringProductCyclePeriod CyclePeriod
    {
        get => (RecurringProductCyclePeriod)CyclePeriodId;
        set => CyclePeriodId = (int)value;
    }
}
