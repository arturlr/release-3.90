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
            builder.HasKey(nc => nc.Id);
            builder.HasOne(nc => nc.NewsItem).WithMany(n => n.NewsComments).HasForeignKey(nc => nc.NewsItemId).IsRequired();
            base.Configure(builder);
        }
    }
}
