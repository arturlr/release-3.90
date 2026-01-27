using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Security;

namespace Nop.Data.Mapping.Security
{
    public partial class AclRecordMap : NopEntityTypeConfiguration<AclRecord>
    {
        public override void Configure(EntityTypeBuilder<AclRecord> builder)
        {
            builder.ToTable("AclRecord");
            builder.HasKey(ar => ar.Id);

            builder.Property(ar => ar.EntityName).IsRequired().HasMaxLength(400);

            builder.HasOne(ar => ar.CustomerRole)
                .WithMany()
                .HasForeignKey(ar => ar.CustomerRoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            base.Configure(builder);
        }
    }
}
