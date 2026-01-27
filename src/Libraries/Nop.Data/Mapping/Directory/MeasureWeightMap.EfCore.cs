using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Directory;

namespace Nop.Data.Mapping.Directory
{
    public partial class MeasureWeightMap : NopEntityTypeConfiguration<MeasureWeight>
    {
        public override void Configure(EntityTypeBuilder<MeasureWeight> builder)
        {
            builder.ToTable("MeasureWeight");
            builder.HasKey(mw => mw.Id);
            builder.Property(mw => mw.Name).IsRequired().HasMaxLength(100);
            builder.Property(mw => mw.SystemKeyword).IsRequired().HasMaxLength(100);
            builder.Property(mw => mw.Ratio).HasPrecision(18, 8);
            base.Configure(builder);
        }
    }
}
