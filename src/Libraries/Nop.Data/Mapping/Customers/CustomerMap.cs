using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class CustomerMap : NopEntityTypeConfiguration<Customer>
    {
        public CustomerMap()
        {
            this.ToTable("Customer");
            this.HasKey(c => c.Id);
            this.Property(u => u.Username).HasMaxLength(1000);
            this.Property(u => u.Email).HasMaxLength(1000);
            this.Property(u => u.EmailToRevalidate).HasMaxLength(1000);
            this.Property(u => u.SystemName).HasMaxLength(400);

            this.HasMany(c => c.CustomerRoles)
                .WithMany()
                .Map(m => m.ToTable("Customer_CustomerRole_Mapping"));

            this.HasMany(c => c.Addresses)
                .WithMany()
                .Map(m => m.ToTable("CustomerAddresses"));

            Configurations.Add(b =>
            {
                b.HasOne(c => c.BillingAddress).WithMany().HasForeignKey("BillingAddress_Id").IsRequired(false);
                b.HasOne(c => c.ShippingAddress).WithMany().HasForeignKey("ShippingAddress_Id").IsRequired(false);
            });
        }
    }
}