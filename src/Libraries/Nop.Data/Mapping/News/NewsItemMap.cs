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
            builder.HasKey(ni => ni.Id);
            builder.Property(ni => ni.Title).IsRequired();
            builder.Property(ni => ni.Short).IsRequired();
            builder.Property(ni => ni.Full).IsRequired();
            builder.Property(ni => ni.MetaKeywords).HasMaxLength(400);
            builder.Property(ni => ni.MetaTitle).HasMaxLength(400);

            builder.HasOne(ni => ni.Language)
                .WithMany()
                .HasForeignKey(ni => ni.LanguageId).OnDelete(DeleteBehavior.Cascade);

            base.Configure(builder);
        }
    }
}