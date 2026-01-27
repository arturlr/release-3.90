using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Media;

namespace Nop.Data.Mapping.Media
{
    public partial class DownloadMap : NopEntityTypeConfiguration<Download>
    {
        public override void Configure(EntityTypeBuilder<Download> builder)
        {
            builder.ToTable("Download");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.ContentType).HasMaxLength(20);
            builder.Property(d => d.Extension).HasMaxLength(20);
            base.Configure(builder);
        }
    }
}
