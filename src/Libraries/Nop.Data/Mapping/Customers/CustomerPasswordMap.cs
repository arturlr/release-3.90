using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class CustomerPasswordMap : NopEntityTypeConfiguration<CustomerPassword>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<CustomerPassword> builder)
        {
            builder.ToTable("CustomerPassword");
            builder.HasKey(password => password.Id);

            builder.HasOne(password => password.Customer)
                .WithMany()
                .HasForeignKey(password => password.CustomerId)
                .IsRequired();

            builder.Ignore(password => password.PasswordFormat);
        }
    }
}
