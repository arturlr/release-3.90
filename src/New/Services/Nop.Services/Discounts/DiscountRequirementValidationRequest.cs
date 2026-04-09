using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;

namespace Nop.Services.Discounts;

public class DiscountRequirementValidationRequest
{
    public int DiscountRequirementId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
