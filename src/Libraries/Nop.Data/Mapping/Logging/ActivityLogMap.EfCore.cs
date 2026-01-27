using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Logging;

namespace Nop.Data.Mapping.Logging
{
    public partial class ActivityLogMap : NopEntityTypeConfiguration<ActivityLog>
    {
        public override void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder.ToTable("ActivityLog");
            builder.HasKey(al => al.Id);
            
            builder.Property(al => al.Comment).IsRequired();
            builder.Property(al => al.IpAddress).HasMaxLength(200);

            base.Configure(builder);
        }
    }
}
