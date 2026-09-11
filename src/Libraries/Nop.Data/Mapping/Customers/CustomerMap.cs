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
            
            //EF6: HasMany(...).WithMany().Map(m => m.ToTable(...)) - unidirectional
            //many-to-many (CustomerRole/Address have no inverse navigation back to Customer).
            //EF Core models this as a skip navigation over an implicit join entity named
            //through UsingEntity.
            //Runtime deferral 4.12, closed by task 7.7 - see the long note in
            //Mapping/Catalog/ProductMap.cs. EF Core's convention named these columns
            //(CustomerId, CustomerRolesId) and (CustomerId, AddressesId); 3.90's EF6 schema uses
            //(Customer_Id, CustomerRole_Id) and (Customer_Id, Address_Id), which
            //App_Data/Install/SqlServer.StoredProcedures.sql (CustomerLoadAllPaged's Guests
            //role test) and App_Data/Install/Fast/create_*.sql both join by name.
            builder.HasMany(c => c.CustomerRoles)
                .WithMany()
                .UsingEntity(j =>
                {
                    j.ToTable("Customer_CustomerRole_Mapping");
                    j.Property<int>("CustomerId").HasColumnName("Customer_Id");
                    j.Property<int>("CustomerRolesId").HasColumnName("CustomerRole_Id");
                });

            builder.HasMany(c => c.Addresses)
                .WithMany()
                .UsingEntity(j =>
                {
                    j.ToTable("CustomerAddresses");
                    j.Property<int>("CustomerId").HasColumnName("Customer_Id");
                    j.Property<int>("AddressesId").HasColumnName("Address_Id");
                });

            //EF6: HasOptional(c => c.BillingAddress) with no With* call - EF6 inferred an
            //optional unidirectional reference with a generated FK column. EF Core requires
            //the other end to be stated; the FK is a shadow property whose name is pinned to
            //the legacy 3.90 column so the schema is unchanged.
            builder.HasOne(c => c.BillingAddress)
                .WithMany()
                .HasForeignKey("BillingAddress_Id")
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.ShippingAddress)
                .WithMany()
                .HasForeignKey("ShippingAddress_Id")
                .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}