using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Directory;

namespace Nop.Data.Mapping.Directory
{
    public partial class MeasureDimensionMap : NopEntityTypeConfiguration<MeasureDimension>
    {
        public override void Configure(EntityTypeBuilder<MeasureDimension> builder)
        {
            builder.ToTable("MeasureDimension");
            builder.HasKey(md => md.Id);
            builder.Property(md => md.Name).IsRequired().HasMaxLength(100);
            builder.Property(md => md.SystemKeyword).IsRequired().HasMaxLength(100);
            builder.Property(md => md.Ratio).HasPrecision(18, 8);
            base.Configure(builder);
        }
    }
}
