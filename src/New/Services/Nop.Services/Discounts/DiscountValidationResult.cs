namespace Nop.Services.Discounts;

public class DiscountValidationResult
{
    public bool IsValid { get; set; }
    public IList<string> Errors { get; set; } = [];
}
