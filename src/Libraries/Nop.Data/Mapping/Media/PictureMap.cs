using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Media;

namespace Nop.Data.Mapping.Media
{
    public partial class PictureMap : NopEntityTypeConfiguration<Picture>
    {
        public override void Configure(EntityTypeBuilder<Picture> builder)
        {
            builder.ToTable("Picture");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PictureBinary);
            builder.Property(p => p.MimeType).IsRequired().HasMaxLength(40);
            builder.Property(p => p.SeoFilename).HasMaxLength(300);
        }
    }
}
