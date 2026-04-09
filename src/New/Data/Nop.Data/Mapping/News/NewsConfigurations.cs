using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.News;

namespace Nop.Data.Mapping.News;

public class NewsItemConfiguration : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> builder)
    {
        builder.ToTable("News");
        builder.HasKey(ni => ni.Id);
        builder.Property(ni => ni.Title).IsRequired();
        builder.Property(ni => ni.Short).IsRequired();
        builder.Property(ni => ni.Full).IsRequired();
    }
}

public class NewsCommentConfiguration : IEntityTypeConfiguration<NewsComment>
{
    public void Configure(EntityTypeBuilder<NewsComment> builder)
    {
        builder.ToTable("NewsComment");
        builder.HasKey(nc => nc.Id);
    }
}
