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
                .HasForeignKey(dr => dr.DiscountId);

            //Runtime deferral 4.12, closed by task 7.7 - see the long note in
            //Mapping/Catalog/ProductMap.cs. 3.90's EF6 column names are (Discount_Id, Category_Id),
            //(Discount_Id, Manufacturer_Id) and (Discount_Id, Product_Id).
            builder.HasMany(dr => dr.AppliedToCategories)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity(j =>
                {
                    j.ToTable("Discount_AppliedToCategories");
                    j.Property<int>("AppliedDiscountsId").HasColumnName("Discount_Id");
                    j.Property<int>("AppliedToCategoriesId").HasColumnName("Category_Id");
                });

            builder.HasMany(dr => dr.AppliedToManufacturers)
                .WithMany(c => c.AppliedDiscounts)
                .UsingEntity(j =>
                {
                    j.ToTable("Discount_AppliedToManufacturers");
                    j.Property<int>("AppliedDiscountsId").HasColumnName("Discount_Id");
                    j.Property<int>("AppliedToManufacturersId").HasColumnName("Manufacturer_Id");
                });

            builder.HasMany(dr => dr.AppliedToProducts)
                .WithMany(p => p.AppliedDiscounts)
                .UsingEntity(j =>
                {
                    j.ToTable("Discount_AppliedToProducts");
                    j.Property<int>("AppliedDiscountsId").HasColumnName("Discount_Id");
                    j.Property<int>("AppliedToProductsId").HasColumnName("Product_Id");
                });

            base.Configure(builder);
        }
    }
}