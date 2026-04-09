# Helpers Services

## Bounded Context
Utility helpers — date/time helpers and other shared utility services.

## Legacy Source
- `src/Libraries/Nop.Services/Helpers/` — all files
- Key interfaces: `IDateTimeHelper`

## Key Entities
- No domain entities — utility functions

## External Dependencies
- None beyond Nop.Core

## Migration Notes
- **Decision**: Rewrite
- `IDateTimeHelper`: UTC ↔ user timezone conversion, timezone listing
- May add additional helpers as needed during migration

## Acceptance Criteria
- [ ] UTC to user timezone conversion works correctly for all standard timezones
- [ ] Timezone listing returns all available system timezones
- [ ] Default timezone configurable via settings
