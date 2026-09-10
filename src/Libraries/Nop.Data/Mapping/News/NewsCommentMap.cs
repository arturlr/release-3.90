using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.News;

namespace Nop.Data.Mapping.News
{
    public partial class NewsCommentMap : NopEntityTypeConfiguration<NewsComment>
    {
        public override void Configure(EntityTypeBuilder<NewsComment> builder)
        {
            builder.ToTable("NewsComment");
            builder.HasKey(comment => comment.Id);

            builder.HasOne(comment => comment.NewsItem)
                .WithMany(news => news.NewsComments)
                .HasForeignKey(comment => comment.NewsItemId);

            builder.HasOne(comment => comment.Customer)
                .WithMany()
                .HasForeignKey(comment => comment.CustomerId);

            builder.HasOne(comment => comment.Store)
                .WithMany()
                .HasForeignKey(comment => comment.StoreId);

            base.Configure(builder);
        }
    }
}