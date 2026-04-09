# Topic Services

## Bounded Context
Content pages (topics) — static pages like "About Us", "Terms of Service", etc.

## Legacy Source
- `src/Libraries/Nop.Services/Topics/` — all files
- Key interfaces: `ITopicService`, `ITopicTemplateService`
- Domain: `Nop.Core.Domain.Topics`

## Key Entities
- Topic, TopicTemplate

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Topics are CMS-style content pages with SEO support
- System topics identified by `SystemName` (e.g., "AboutUs", "ConditionsOfUse")
- Password-protected topics supported
- Navigation placement: sitemap, top menu, footer columns 1-3
- ACL and store mapping supported

## Acceptance Criteria
- [ ] Topic CRUD with ACL, store mapping, and SEO slug support
- [ ] System topics retrievable by `SystemName`
- [ ] Password-protected topics require correct password to view
