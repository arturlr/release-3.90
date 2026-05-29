using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Stores;

namespace Nop.Data.Mapping.Stores
{
    public partial class StoreMappingMap : NopEntityTypeConfiguration<StoreMapping>
    {
        public override void Configure(EntityTypeBuilder<StoreMapping> builder)
        {
            builder.ToTable("StoreMapping");
            builder.HasKey(sm => sm.Id);

            builder.Property(sm => sm.EntityName).IsRequired().HasMaxLength(400);

            builder.HasOne(sm => sm.Store)
                .WithMany()
                .HasForeignKey(sm => sm.StoreId)
                .IsRequired();
        }
    }
}
