using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class CustomerAttributeValueMap : NopEntityTypeConfiguration<CustomerAttributeValue>
    {
        public override void Configure(EntityTypeBuilder<CustomerAttributeValue> builder)
        {
            builder.ToTable("CustomerAttributeValue");
            builder.HasKey(cav => cav.Id);
            builder.Property(cav => cav.Name).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
