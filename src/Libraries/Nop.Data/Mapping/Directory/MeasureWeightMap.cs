using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Directory;

namespace Nop.Data.Mapping.Directory
{
    public partial class MeasureWeightMap : NopEntityTypeConfiguration<MeasureWeight>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<MeasureWeight> builder)
        {
            builder.ToTable("MeasureWeight");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.SystemKeyword).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Ratio).HasPrecision(18, 8);        }
    }
}
