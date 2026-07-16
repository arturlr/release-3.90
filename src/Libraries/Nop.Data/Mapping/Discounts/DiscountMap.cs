using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;

namespace Nop.Data.Mapping.Discounts
{
    public partial class DiscountMap : NopEntityTypeConfiguration<Discount>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Discount> builder)
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

            builder.HasMany(d => d.AppliedToCategories)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Discount_AppliedToCategories",
                    right => right.HasOne<Category>().WithMany().HasForeignKey("Category_Id"),
                    left => left.HasOne<Discount>().WithMany().HasForeignKey("Discount_Id"),
                    j => j.ToTable("Discount_AppliedToCategories"));

            builder.HasMany(d => d.AppliedToManufacturers)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Discount_AppliedToManufacturers",
                    right => right.HasOne<Manufacturer>().WithMany().HasForeignKey("Manufacturer_Id"),
                    left => left.HasOne<Discount>().WithMany().HasForeignKey("Discount_Id"),
                    j => j.ToTable("Discount_AppliedToManufacturers"));

            builder.HasMany(d => d.AppliedToProducts)
                .WithMany(p => p.AppliedDiscounts)
                .UsingEntity<Dictionary<string, object>>(
                    "Discount_AppliedToProducts",
                    right => right.HasOne<Product>().WithMany().HasForeignKey("Product_Id"),
                    left => left.HasOne<Discount>().WithMany().HasForeignKey("Discount_Id"),
                    j => j.ToTable("Discount_AppliedToProducts"));
        }
    }
}
