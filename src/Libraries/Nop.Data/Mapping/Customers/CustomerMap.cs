using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class CustomerMap : NopEntityTypeConfiguration<Customer>
    {
        public override void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customer");
            builder.HasKey(c => c.Id);
            builder.Property(u => u.Username).HasMaxLength(1000);
            builder.Property(u => u.Email).HasMaxLength(1000);
            builder.Property(u => u.EmailToRevalidate).HasMaxLength(1000);
            builder.Property(u => u.SystemName).HasMaxLength(400);

            builder.HasMany(c => c.CustomerRoles)
                .WithMany()
                .UsingEntity(j => j.ToTable("Customer_CustomerRole_Mapping"));

            builder.HasMany(c => c.Addresses)
                .WithMany()
                .UsingEntity(j => j.ToTable("CustomerAddresses"));

            builder.HasOne(c => c.BillingAddress).WithMany().IsRequired(false);
            builder.HasOne(c => c.ShippingAddress).WithMany().IsRequired(false);
        }
    }
}
