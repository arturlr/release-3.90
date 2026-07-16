using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class CustomerMap : NopEntityTypeConfiguration<Customer>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customer");
            builder.HasKey(c => c.Id);
            builder.Property(u => u.Username).HasMaxLength(1000);
            builder.Property(u => u.Email).HasMaxLength(1000);
            builder.Property(u => u.EmailToRevalidate).HasMaxLength(1000);
            builder.Property(u => u.SystemName).HasMaxLength(400);

            builder.HasMany(c => c.CustomerRoles)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "Customer_CustomerRole_Mapping",
                    right => right.HasOne<CustomerRole>().WithMany().HasForeignKey("CustomerRole_Id"),
                    left => left.HasOne<Customer>().WithMany().HasForeignKey("Customer_Id"),
                    j => j.ToTable("Customer_CustomerRole_Mapping"));

            builder.HasMany(c => c.Addresses)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "CustomerAddresses",
                    right => right.HasOne<Nop.Core.Domain.Common.Address>().WithMany().HasForeignKey("Address_Id"),
                    left => left.HasOne<Customer>().WithMany().HasForeignKey("Customer_Id"),
                    j => j.ToTable("CustomerAddresses"));

            builder.HasOne(c => c.BillingAddress)
                .WithMany()
                .HasForeignKey("BillingAddress_Id")
                .IsRequired(false);

            builder.HasOne(c => c.ShippingAddress)
                .WithMany()
                .HasForeignKey("ShippingAddress_Id")
                .IsRequired(false);
        }
    }
}
