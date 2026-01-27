using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Localization;

namespace Nop.Data.Mapping.Localization
{
    public partial class LocalizedPropertyMap : NopEntityTypeConfiguration<LocalizedProperty>
    {
        public override void Configure(EntityTypeBuilder<LocalizedProperty> builder)
        {
            builder.ToTable("LocalizedProperty");
            builder.HasKey(lp => lp.Id);
            builder.Property(lp => lp.LocaleKeyGroup).IsRequired().HasMaxLength(400);
            builder.Property(lp => lp.LocaleKey).IsRequired().HasMaxLength(400);
            builder.Property(lp => lp.LocaleValue).IsRequired();
            builder.HasOne(lp => lp.Language).WithMany().HasForeignKey(lp => lp.LanguageId).IsRequired();
            base.Configure(builder);
        }
    }
}
