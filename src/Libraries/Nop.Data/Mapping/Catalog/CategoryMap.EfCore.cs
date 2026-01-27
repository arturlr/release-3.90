using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class CategoryMap : NopEntityTypeConfiguration<Category>
    {
        public override void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Category");
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.Name).IsRequired().HasMaxLength(400);
            builder.Property(c => c.MetaKeywords).HasMaxLength(400);
            builder.Property(c => c.MetaTitle).HasMaxLength(400);
            builder.Property(c => c.PriceRanges).HasMaxLength(400);
            builder.Property(c => c.PageSizeOptions).HasMaxLength(200);

            base.Configure(builder);
        }
    }
}
