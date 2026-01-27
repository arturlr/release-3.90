using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class CategoryTemplateMap : NopEntityTypeConfiguration<CategoryTemplate>
    {
        public override void Configure(EntityTypeBuilder<CategoryTemplate> builder)
        {
            builder.ToTable("CategoryTemplate");
            builder.HasKey(ct => ct.Id);
            builder.Property(ct => ct.Name).IsRequired().HasMaxLength(400);
            builder.Property(ct => ct.ViewPath).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
