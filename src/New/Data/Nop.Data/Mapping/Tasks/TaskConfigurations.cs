using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Tasks;

namespace Nop.Data.Mapping.Tasks;

public class ScheduleTaskConfiguration : IEntityTypeConfiguration<ScheduleTask>
{
    public void Configure(EntityTypeBuilder<ScheduleTask> builder)
    {
        builder.ToTable("ScheduleTask");
        builder.HasKey(st => st.Id);
        builder.Property(st => st.Name).IsRequired();
        builder.Property(st => st.Type).IsRequired();
    }
}
