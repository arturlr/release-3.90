using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Media;

namespace Nop.Data.Mapping.Media
{
    public partial class DownloadMap : NopEntityTypeConfiguration<Download>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Download> builder)
        {
            builder.ToTable("Download");
            builder.HasKey(p => p.Id);        }
    }
}
