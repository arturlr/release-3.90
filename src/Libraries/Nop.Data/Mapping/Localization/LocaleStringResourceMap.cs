using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Localization;

namespace Nop.Data.Mapping.Localization
{
    public partial class LocaleStringResourceMap : NopEntityTypeConfiguration<LocaleStringResource>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<LocaleStringResource> builder)
        {
            builder.ToTable("LocaleStringResource");
            builder.HasKey(lsr => lsr.Id);
            builder.Property(lsr => lsr.ResourceName).IsRequired().HasMaxLength(200);
            builder.Property(lsr => lsr.ResourceValue).IsRequired();

            builder.HasOne(lsr => lsr.Language)
                .WithMany(l => l.LocaleStringResources)
                .HasForeignKey(lsr => lsr.LanguageId)
                .IsRequired();        }
    }
}
