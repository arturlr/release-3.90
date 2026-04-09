using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customer");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Username).HasMaxLength(1000);
        builder.Property(c => c.Email).HasMaxLength(1000);
        builder.Property(c => c.EmailToRevalidate).HasMaxLength(1000);
        builder.Property(c => c.SystemName).HasMaxLength(400);
    }
}

public class CustomerAttributeConfiguration : IEntityTypeConfiguration<CustomerAttribute>
{
    public void Configure(EntityTypeBuilder<CustomerAttribute> builder)
    {
        builder.ToTable("CustomerAttribute");
        builder.HasKey(ca => ca.Id);
        builder.Property(ca => ca.Name).IsRequired().HasMaxLength(400);
        builder.Ignore(ca => ca.AttributeControlType);
    }
}

public class CustomerAttributeValueConfiguration : IEntityTypeConfiguration<CustomerAttributeValue>
{
    public void Configure(EntityTypeBuilder<CustomerAttributeValue> builder)
    {
        builder.ToTable("CustomerAttributeValue");
        builder.HasKey(cav => cav.Id);
        builder.Property(cav => cav.Name).IsRequired().HasMaxLength(400);
    }
}

public class CustomerPasswordConfiguration : IEntityTypeConfiguration<CustomerPassword>
{
    public void Configure(EntityTypeBuilder<CustomerPassword> builder)
    {
        builder.ToTable("CustomerPassword");
        builder.HasKey(cp => cp.Id);
        builder.Ignore(cp => cp.PasswordFormat);
    }
}

public class CustomerRoleConfiguration : IEntityTypeConfiguration<CustomerRole>
{
    public void Configure(EntityTypeBuilder<CustomerRole> builder)
    {
        builder.ToTable("CustomerRole");
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Name).IsRequired().HasMaxLength(255);
        builder.Property(cr => cr.SystemName).HasMaxLength(255);
    }
}

public class ExternalAuthenticationRecordConfiguration : IEntityTypeConfiguration<ExternalAuthenticationRecord>
{
    public void Configure(EntityTypeBuilder<ExternalAuthenticationRecord> builder)
    {
        builder.ToTable("ExternalAuthenticationRecord");
        builder.HasKey(ear => ear.Id);
    }
}

public class RewardPointsHistoryConfiguration : IEntityTypeConfiguration<RewardPointsHistory>
{
    public void Configure(EntityTypeBuilder<RewardPointsHistory> builder)
    {
        builder.ToTable("RewardPointsHistory");
        builder.HasKey(rph => rph.Id);
        builder.Property(rph => rph.UsedAmount).HasPrecision(18, 4);
    }
}
