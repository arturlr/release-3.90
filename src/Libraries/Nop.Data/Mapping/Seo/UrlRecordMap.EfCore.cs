using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Seo;

namespace Nop.Data.Mapping.Seo
{
    public partial class UrlRecordMap : NopEntityTypeConfiguration<UrlRecord>
    {
        public override void Configure(EntityTypeBuilder<UrlRecord> builder)
        {
            builder.ToTable("UrlRecord");
            builder.HasKey(ur => ur.Id);
            builder.Property(ur => ur.EntityName).IsRequired().HasMaxLength(400);
            builder.Property(ur => ur.Slug).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
