namespace Nop.Services.Discounts;

public class DiscountRequirementValidationResult
{
    public bool IsValid { get; set; }
    public string? UserError { get; set; }
}
