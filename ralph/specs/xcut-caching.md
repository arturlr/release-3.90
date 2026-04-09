# Caching

## Bounded Context
Caching infrastructure — in-memory cache, distributed Redis cache, per-request cache, and cache invalidation patterns.

## Legacy Source
- `src/Libraries/Nop.Core/Caching/` — ICacheManager, MemoryCacheManager, RedisCacheManager, PerRequestCacheManager, IRedisConnectionWrapper, RedisConnectionWrapper
- Cache key patterns defined throughout services (e.g., `PRODUCTS_BY_ID_KEY`)

## Key Entities
- `ICacheManager` interface (Get, Set, IsSet, Remove, RemoveByPattern, Clear)
- Three implementations: Memory, Redis, PerRequest

## External Dependencies
- StackExchange.Redis.StrongName 1.2.1 → `Microsoft.Extensions.Caching.StackExchangeRedis`
- RedLock.net.StrongName 1.7.4 → `Medallion.Threading.Redis` or `RedLock.net` 3.x
- System.Runtime.Caching → `Microsoft.Extensions.Caching.Memory`

## Migration Notes
- **Decision**: Rewrite
- Replace custom `ICacheManager` with `IMemoryCache` + `IDistributedCache` from Microsoft.Extensions.Caching
- Pattern-based cache invalidation (`RemoveByPattern`) needs custom implementation on top of `IDistributedCache`
- Per-request cache → scoped service with `Dictionary<string, object>`
- Cache key management: define constants per service, use consistent naming
- Distributed locking for cache stampede prevention

## Acceptance Criteria
- [ ] In-memory caching via `IMemoryCache` with configurable expiration
- [ ] Distributed caching via Redis using `IDistributedCache`
- [ ] Pattern-based cache invalidation removes all keys matching a prefix/pattern
- [ ] Per-request caching prevents duplicate DB calls within a single HTTP request
