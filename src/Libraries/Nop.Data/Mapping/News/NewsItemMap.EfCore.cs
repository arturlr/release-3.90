using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.News;

namespace Nop.Data.Mapping.News
{
    public partial class NewsItemMap : NopEntityTypeConfiguration<NewsItem>
    {
        public override void Configure(EntityTypeBuilder<NewsItem> builder)
        {
            builder.ToTable("News");
            builder.HasKey(n => n.Id);
            
            builder.Property(n => n.Title).IsRequired();
            builder.Property(n => n.MetaKeywords).HasMaxLength(400);
            builder.Property(n => n.MetaTitle).HasMaxLength(400);

            base.Configure(builder);
        }
    }
}
