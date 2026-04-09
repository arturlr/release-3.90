using Nop.Core.Domain.Catalog;

namespace Nop.Services.Payments;

public class ProcessPaymentRequest
{
    public int StoreId { get; set; }
    public int CustomerId { get; set; }
    public Guid OrderGuid { get; set; }
    public decimal OrderTotal { get; set; }
    public string? PaymentMethodSystemName { get; set; }

    // Credit card properties
    public string? CreditCardType { get; set; }
    public string? CreditCardName { get; set; }
    public string? CreditCardNumber { get; set; }
    public int CreditCardExpireYear { get; set; }
    public int CreditCardExpireMonth { get; set; }
    public string? CreditCardCvv2 { get; set; }

    // Recurring payment properties
    public int InitialOrderId { get; set; }
    public int RecurringCycleLength { get; set; }
    public RecurringProductCyclePeriod RecurringCyclePeriod { get; set; }
    public int RecurringTotalCycles { get; set; }

    public Dictionary<string, object> CustomValues { get; set; } = [];
}
