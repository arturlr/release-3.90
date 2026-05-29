using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Discounts;

namespace Nop.Data.Mapping.Discounts
{
    public partial class DiscountMap : NopEntityTypeConfiguration<Discount>
    {
        public override void Configure(EntityTypeBuilder<Discount> builder)
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

            builder.HasMany(dr => dr.DiscountRequirements)
                .WithOne(d => d.Discount)
                .HasForeignKey(dr => dr.DiscountId)
                .IsRequired();

            builder.HasMany(dr => dr.AppliedToCategories)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity(j => j.ToTable("Discount_AppliedToCategories"));

            builder.HasMany(dr => dr.AppliedToManufacturers)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity(j => j.ToTable("Discount_AppliedToManufacturers"));

            builder.HasMany(dr => dr.AppliedToProducts)
                .WithMany(p => p.AppliedDiscounts)
                .UsingEntity(j => j.ToTable("Discount_AppliedToProducts"));
        }
    }
}
