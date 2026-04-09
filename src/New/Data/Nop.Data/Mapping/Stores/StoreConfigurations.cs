using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Stores;

namespace Nop.Data.Mapping.Stores;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Store");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(400);
        builder.Property(s => s.Url).IsRequired().HasMaxLength(400);
        builder.Property(s => s.SecureUrl).HasMaxLength(400);
        builder.Property(s => s.Hosts).HasMaxLength(1000);
        builder.Property(s => s.CompanyName).HasMaxLength(1000);
        builder.Property(s => s.CompanyAddress).HasMaxLength(1000);
        builder.Property(s => s.CompanyPhoneNumber).HasMaxLength(1000);
        builder.Property(s => s.CompanyVat).HasMaxLength(1000);
    }
}

public class StoreMappingConfiguration : IEntityTypeConfiguration<StoreMapping>
{
    public void Configure(EntityTypeBuilder<StoreMapping> builder)
    {
        builder.ToTable("StoreMapping");
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.EntityName).IsRequired().HasMaxLength(400);
    }
}
