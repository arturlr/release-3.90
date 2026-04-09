using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Localization;

namespace Nop.Data.Mapping.Localization;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Language");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.LanguageCulture).IsRequired().HasMaxLength(20);
        builder.Property(l => l.UniqueSeoCode).HasMaxLength(2);
        builder.Property(l => l.FlagImageFileName).HasMaxLength(50);
    }
}

public class LocaleStringResourceConfiguration : IEntityTypeConfiguration<LocaleStringResource>
{
    public void Configure(EntityTypeBuilder<LocaleStringResource> builder)
    {
        builder.ToTable("LocaleStringResource");
        builder.HasKey(lsr => lsr.Id);
        builder.Property(lsr => lsr.ResourceName).IsRequired().HasMaxLength(200);
        builder.Property(lsr => lsr.ResourceValue).IsRequired();
    }
}

public class LocalizedPropertyConfiguration : IEntityTypeConfiguration<LocalizedProperty>
{
    public void Configure(EntityTypeBuilder<LocalizedProperty> builder)
    {
        builder.ToTable("LocalizedProperty");
        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.LocaleKeyGroup).IsRequired().HasMaxLength(400);
        builder.Property(lp => lp.LocaleKey).IsRequired().HasMaxLength(400);
        builder.Property(lp => lp.LocaleValue).IsRequired();
    }
}
