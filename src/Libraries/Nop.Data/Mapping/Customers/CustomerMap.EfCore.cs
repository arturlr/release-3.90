using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    /// <summary>
    /// EF Core mapping configuration for Customer entity
    /// </summary>
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

            base.Configure(builder);
        }
    }
}
