# Forum Services

## Bounded Context
Forum/community management — forum groups, forums, topics, posts, votes, subscriptions, and private messages.

## Legacy Source
- `src/Libraries/Nop.Services/Forums/` — ForumService.cs (1537 LOC)
- Key interfaces: `IForumService`
- Domain: `Nop.Core.Domain.Forums`

## Key Entities
- ForumGroup, Forum, ForumTopic, ForumPost
- ForumPostVote, ForumSubscription, PrivateMessage

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: MEDIUM-HIGH — single 1537 LOC service handles all forum operations
- Topic types: Normal, Sticky, Announcement
- Post voting (up/down)
- Forum/topic subscriptions with email notifications
- Private messaging between customers
- Denormalized counts (NumTopics, NumPosts, Views) need careful update logic

## Acceptance Criteria
- [ ] Forum group → forum → topic → post hierarchy CRUD works correctly
- [ ] Topic types (Normal, Sticky, Announcement) sort and display correctly
- [ ] Forum/topic subscriptions trigger email notifications on new posts
- [ ] Private messaging between customers with read/delete tracking works
