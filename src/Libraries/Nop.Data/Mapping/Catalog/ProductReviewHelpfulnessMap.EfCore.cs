using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductReviewHelpfulnessMap : NopEntityTypeConfiguration<ProductReviewHelpfulness>
    {
        public override void Configure(EntityTypeBuilder<ProductReviewHelpfulness> builder)
        {
            builder.ToTable("ProductReviewHelpfulness");
            builder.HasKey(prh => prh.Id);
            builder.HasOne(prh => prh.ProductReview).WithMany().HasForeignKey(prh => prh.ProductReviewId).IsRequired();
            base.Configure(builder);
        }
    }
}
