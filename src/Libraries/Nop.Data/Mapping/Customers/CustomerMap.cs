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
                .Map(m => { m.ToTable("Customer_CustomerRole_Mapping"); m.MapLeftKey("Customer_Id"); m.MapRightKey("CustomerRole_Id"); });

            this.HasMany(c => c.Addresses)
                .WithMany()
                .Map(m => { m.ToTable("CustomerAddresses"); m.MapLeftKey("Customer_Id"); m.MapRightKey("Address_Id"); });
            this.Ignore(c => c.BillingAddress);
            this.Ignore(c => c.ShippingAddress);
        }
    }
}