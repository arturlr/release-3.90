using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Messages;

namespace Nop.Data.Mapping.Messages;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaign");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired();
        builder.Property(c => c.Subject).IsRequired();
        builder.Property(c => c.Body).IsRequired();
    }
}

public class EmailAccountConfiguration : IEntityTypeConfiguration<EmailAccount>
{
    public void Configure(EntityTypeBuilder<EmailAccount> builder)
    {
        builder.ToTable("EmailAccount");
        builder.HasKey(ea => ea.Id);
        builder.Property(ea => ea.Email).IsRequired().HasMaxLength(255);
        builder.Property(ea => ea.DisplayName).HasMaxLength(255);
        builder.Property(ea => ea.Host).IsRequired().HasMaxLength(255);
        builder.Property(ea => ea.Username).IsRequired().HasMaxLength(255);
        builder.Property(ea => ea.Password).IsRequired().HasMaxLength(255);
        builder.Ignore(ea => ea.FriendlyName);
    }
}

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.ToTable("MessageTemplate");
        builder.HasKey(mt => mt.Id);
        builder.Property(mt => mt.Name).IsRequired().HasMaxLength(200);
    }
}

public class NewsLetterSubscriptionConfiguration : IEntityTypeConfiguration<NewsLetterSubscription>
{
    public void Configure(EntityTypeBuilder<NewsLetterSubscription> builder)
    {
        builder.ToTable("NewsLetterSubscription");
        builder.HasKey(nls => nls.Id);
        builder.Property(nls => nls.Email).IsRequired().HasMaxLength(255);
    }
}

public class QueuedEmailConfiguration : IEntityTypeConfiguration<QueuedEmail>
{
    public void Configure(EntityTypeBuilder<QueuedEmail> builder)
    {
        builder.ToTable("QueuedEmail");
        builder.HasKey(qe => qe.Id);
        builder.Property(qe => qe.From).IsRequired().HasMaxLength(500);
        builder.Property(qe => qe.FromName).HasMaxLength(500);
        builder.Property(qe => qe.To).IsRequired().HasMaxLength(500);
        builder.Property(qe => qe.ToName).HasMaxLength(500);
        builder.Property(qe => qe.ReplyTo).HasMaxLength(500);
        builder.Property(qe => qe.ReplyToName).HasMaxLength(500);
        builder.Property(qe => qe.CC).HasMaxLength(500);
        builder.Property(qe => qe.Bcc).HasMaxLength(500);
        builder.Property(qe => qe.Subject).HasMaxLength(1000);
        builder.Ignore(qe => qe.Priority);
    }
}
