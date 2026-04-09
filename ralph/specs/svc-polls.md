# Poll Services

## Bounded Context
Polling — polls, poll answers, and voting records.

## Legacy Source
- `src/Libraries/Nop.Services/Polls/` — all files
- Key interfaces: `IPollService`
- Domain: `Nop.Core.Domain.Polls`

## Key Entities
- Poll, PollAnswer, PollVotingRecord

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Simple CRUD with language filtering, date range, store mapping
- Guest voting configurable per poll
- Denormalized vote counts on PollAnswer

## Acceptance Criteria
- [ ] Poll CRUD with language, date range, and store mapping filtering
- [ ] Voting records prevent duplicate votes per customer
- [ ] Guest voting respects `AllowGuestsToVote` setting
