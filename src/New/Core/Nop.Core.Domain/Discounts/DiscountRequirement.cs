namespace Nop.Core.Domain.Discounts
{

    public class DiscountRequirement : BaseEntity
    {
        public int DiscountId { get; set; }

        public string? DiscountRequirementRuleSystemName { get; set; }

        public int? ParentId { get; set; }

        public int? InteractionTypeId { get; set; }

        public bool IsGroup { get; set; }

        public RequirementGroupInteractionType? InteractionType
        {
            get { return (RequirementGroupInteractionType?)InteractionTypeId; }
            set { InteractionTypeId = (int?)value; }
        }
}
}
