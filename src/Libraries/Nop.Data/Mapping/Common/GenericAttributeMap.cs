using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Common;

namespace Nop.Data.Mapping.Common
{
    public partial class GenericAttributeMap : NopEntityTypeConfiguration<GenericAttribute>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<GenericAttribute> builder)
        {
            builder.ToTable("GenericAttribute");
            builder.HasKey(ga => ga.Id);

            builder.Property(ga => ga.KeyGroup).IsRequired().HasMaxLength(400);
            builder.Property(ga => ga.Key).IsRequired().HasMaxLength(400);
            builder.Property(ga => ga.Value).IsRequired();
        }
    }
}
