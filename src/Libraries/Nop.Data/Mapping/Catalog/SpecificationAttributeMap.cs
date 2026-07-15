using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class SpecificationAttributeMap : NopEntityTypeConfiguration<SpecificationAttribute>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<SpecificationAttribute> builder)
        {
            builder.ToTable("SpecificationAttribute");
            builder.HasKey(sa => sa.Id);
            builder.Property(sa => sa.Name).IsRequired();
        }
    }
}
