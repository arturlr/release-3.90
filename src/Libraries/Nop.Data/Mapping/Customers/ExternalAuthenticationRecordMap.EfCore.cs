using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class ExternalAuthenticationRecordMap : NopEntityTypeConfiguration<ExternalAuthenticationRecord>
    {
        public override void Configure(EntityTypeBuilder<ExternalAuthenticationRecord> builder)
        {
            builder.ToTable("ExternalAuthenticationRecord");
            builder.HasKey(ear => ear.Id);
            builder.HasOne(ear => ear.Customer).WithMany(c => c.ExternalAuthenticationRecords).HasForeignKey(ear => ear.CustomerId).IsRequired();
            base.Configure(builder);
        }
    }
}
