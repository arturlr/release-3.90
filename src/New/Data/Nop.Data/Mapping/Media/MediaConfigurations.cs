using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Media;

namespace Nop.Data.Mapping.Media;

public class DownloadConfiguration : IEntityTypeConfiguration<Download>
{
    public void Configure(EntityTypeBuilder<Download> builder)
    {
        builder.ToTable("Download");
        builder.HasKey(d => d.Id);
    }
}

public class PictureConfiguration : IEntityTypeConfiguration<Picture>
{
    public void Configure(EntityTypeBuilder<Picture> builder)
    {
        builder.ToTable("Picture");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.MimeType).IsRequired().HasMaxLength(40);
        builder.Property(p => p.SeoFilename).HasMaxLength(300);
    }
}
