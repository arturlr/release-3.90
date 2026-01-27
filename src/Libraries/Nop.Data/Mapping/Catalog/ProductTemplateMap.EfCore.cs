using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductTemplateMap : NopEntityTypeConfiguration<ProductTemplate>
    {
        public override void Configure(EntityTypeBuilder<ProductTemplate> builder)
        {
            builder.ToTable("ProductTemplate");
            builder.HasKey(pt => pt.Id);
            builder.Property(pt => pt.Name).IsRequired().HasMaxLength(400);
            builder.Property(pt => pt.ViewPath).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
