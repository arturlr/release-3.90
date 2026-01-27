using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class CustomerPasswordMap : NopEntityTypeConfiguration<CustomerPassword>
    {
        public override void Configure(EntityTypeBuilder<CustomerPassword> builder)
        {
            builder.ToTable("CustomerPassword");
            builder.HasKey(cp => cp.Id);
            builder.HasOne(cp => cp.Customer).WithMany().HasForeignKey(cp => cp.CustomerId).IsRequired();
            base.Configure(builder);
        }
    }
}
