using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Security;

namespace Nop.Data.Mapping.Security;

public class AclRecordConfiguration : IEntityTypeConfiguration<AclRecord>
{
    public void Configure(EntityTypeBuilder<AclRecord> builder)
    {
        builder.ToTable("AclRecord");
        builder.HasKey(ar => ar.Id);
        builder.Property(ar => ar.EntityName).IsRequired().HasMaxLength(400);
    }
}

public class PermissionRecordConfiguration : IEntityTypeConfiguration<PermissionRecord>
{
    public void Configure(EntityTypeBuilder<PermissionRecord> builder)
    {
        builder.ToTable("PermissionRecord");
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Name).IsRequired();
        builder.Property(pr => pr.SystemName).IsRequired().HasMaxLength(255);
        builder.Property(pr => pr.Category).IsRequired().HasMaxLength(255);
    }
}
