using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Discounts;

namespace Nop.Data.Mapping.Discounts
{
    public partial class DiscountRequirementMap : NopEntityTypeConfiguration<DiscountRequirement>
    {
        public override void Configure(EntityTypeBuilder<DiscountRequirement> builder)
        {
            builder.ToTable("DiscountRequirement");
            builder.HasKey(dr => dr.Id);
            builder.HasOne(dr => dr.Discount).WithMany(d => d.DiscountRequirements).HasForeignKey(dr => dr.DiscountId).IsRequired();
            base.Configure(builder);
        }
    }
}
