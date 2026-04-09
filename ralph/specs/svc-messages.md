# Message Services

## Bounded Context
Messaging and notifications — workflow messages (transactional emails), message templates, email accounts, queued emails, newsletter subscriptions, and campaigns.

## Legacy Source
- `src/Libraries/Nop.Services/Messages/` — all files
- Key interfaces: `IWorkflowMessageService` (1920 LOC), `IMessageTemplateService`, `IQueuedEmailService`, `IEmailAccountService`, `INewsLetterSubscriptionService`, `IMessageTokenProvider`, `ICampaignService`
- Domain: `Nop.Core.Domain.Messages`

## Key Entities
- EmailAccount, MessageTemplate, QueuedEmail
- NewsLetterSubscription, Campaign
- Token system for template variable replacement

## External Dependencies
- SMTP server (System.Net.Mail → MailKit in .NET 10)

## Migration Notes
- **Decision**: Rewrite
- `IWorkflowMessageService` has 30+ methods for different notification types — keep same granularity
- Message templates use token replacement (`%Store.Name%`, `%Order.OrderNumber%`, etc.)
- Email queue: messages queued then sent by background task
- Newsletter: subscription/unsubscription with activation workflow
- Campaigns: bulk email to customer role segments
- Replace `System.Net.Mail` with `MailKit` for SMTP

## Acceptance Criteria
- [ ] All 30+ workflow message types send correct emails with proper token replacement
- [ ] Email queue processes messages via background task with retry logic
- [ ] Newsletter subscription/unsubscription with activation email works correctly
- [ ] Campaign emails target correct customer role segments
