using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class BackInStockSubscriptionMap : NopEntityTypeConfiguration<BackInStockSubscription>
    {
        public override void Configure(EntityTypeBuilder<BackInStockSubscription> builder)
        {
            builder.ToTable("BackInStockSubscription");
            builder.HasKey(biss => biss.Id);
            builder.HasOne(biss => biss.Product).WithMany().HasForeignKey(biss => biss.ProductId).IsRequired();
            builder.HasOne(biss => biss.Customer).WithMany().HasForeignKey(biss => biss.CustomerId).IsRequired();
            base.Configure(builder);
        }
    }
}
