using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Tasks;

namespace Nop.Data.Mapping.Tasks
{
    public partial class ScheduleTaskMap : NopEntityTypeConfiguration<ScheduleTask>
    {
        public override void Configure(EntityTypeBuilder<ScheduleTask> builder)
        {
            builder.ToTable("ScheduleTask");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired();
            builder.Property(t => t.Type).IsRequired();

            base.Configure(builder);
        }
    }
}