using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductCategoryMap : NopEntityTypeConfiguration<ProductCategory>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.ToTable("Product_Category_Mapping");
            builder.HasKey(pc => pc.Id);

            builder.HasOne(pc => pc.Category)
                .WithMany()
                .HasForeignKey(pc => pc.CategoryId)
                .IsRequired();

            builder.HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(pc => pc.ProductId)
                .IsRequired();
        }
    }
}
