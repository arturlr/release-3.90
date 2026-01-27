using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class StockQuantityHistoryMap : NopEntityTypeConfiguration<StockQuantityHistory>
    {
        public override void Configure(EntityTypeBuilder<StockQuantityHistory> builder)
        {
            builder.ToTable("StockQuantityHistory");
            builder.HasKey(sqh => sqh.Id);
            builder.HasOne(sqh => sqh.Product).WithMany().HasForeignKey(sqh => sqh.ProductId).IsRequired();
            base.Configure(builder);
        }
    }
}
