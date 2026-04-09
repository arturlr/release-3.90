namespace Nop.Services.Discounts;

/// <summary>
/// Discount requirement rule — implemented by plugins (e.g., DiscountRules.CustomerRoles).
/// Plugin system [2.10] not built yet — interface defined for future use.
/// </summary>
public interface IDiscountRequirementRule
{
    DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request);
    string GetConfigurationUrl(int discountId, int? discountRequirementId);
}
