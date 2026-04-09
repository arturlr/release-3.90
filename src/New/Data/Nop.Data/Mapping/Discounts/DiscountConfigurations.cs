using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Discounts;

namespace Nop.Data.Mapping.Discounts;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discount");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.CouponCode).HasMaxLength(100);
        builder.Property(d => d.DiscountPercentage).HasPrecision(18, 4);
        builder.Property(d => d.DiscountAmount).HasPrecision(18, 4);
        builder.Property(d => d.MaximumDiscountAmount).HasPrecision(18, 4);
        builder.Ignore(d => d.DiscountType);
        builder.Ignore(d => d.DiscountLimitation);
    }
}

public class DiscountRequirementConfiguration : IEntityTypeConfiguration<DiscountRequirement>
{
    public void Configure(EntityTypeBuilder<DiscountRequirement> builder)
    {
        builder.ToTable("DiscountRequirement");
        builder.HasKey(dr => dr.Id);
        builder.Ignore(dr => dr.InteractionType);
    }
}

public class DiscountUsageHistoryConfiguration : IEntityTypeConfiguration<DiscountUsageHistory>
{
    public void Configure(EntityTypeBuilder<DiscountUsageHistory> builder)
    {
        builder.ToTable("DiscountUsageHistory");
        builder.HasKey(duh => duh.Id);
    }
}
