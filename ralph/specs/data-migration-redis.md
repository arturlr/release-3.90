# Data Migration — Redis Cache

## Bounded Context
Redis cache migration — cache key format changes and connection configuration.

## Legacy Source
- `src/Libraries/Nop.Core/Caching/RedisCacheManager.cs`
- `src/Libraries/Nop.Core/Caching/RedisConnectionWrapper.cs`
- Cache key patterns throughout services

## Key Entities
- Cache keys (string patterns per service)
- Redis connection configuration

## External Dependencies
- StackExchange.Redis 1.2.1 → 2.x or `Microsoft.Extensions.Caching.StackExchangeRedis`

## Migration Notes
- **Decision**: Flush and rebuild
- Cache is ephemeral — no data migration needed, just flush on cutover
- New cache key format may differ from legacy
- Connection string format may change slightly between Redis client versions
- Distributed locking library change (RedLock.net → newer version)

## Acceptance Criteria
- [ ] Redis connection established using new client library
- [ ] Cache flush performed during cutover
- [ ] New cache key patterns documented and consistent
- [ ] Distributed locking works for cache stampede prevention
