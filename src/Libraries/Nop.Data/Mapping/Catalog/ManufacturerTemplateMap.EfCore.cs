using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ManufacturerTemplateMap : NopEntityTypeConfiguration<ManufacturerTemplate>
    {
        public override void Configure(EntityTypeBuilder<ManufacturerTemplate> builder)
        {
            builder.ToTable("ManufacturerTemplate");
            builder.HasKey(mt => mt.Id);
            builder.Property(mt => mt.Name).IsRequired().HasMaxLength(400);
            builder.Property(mt => mt.ViewPath).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
