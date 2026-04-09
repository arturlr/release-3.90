# Observability

## Bounded Context
Observability — health checks, metrics, distributed tracing, and monitoring.

## Legacy Source
- No direct legacy equivalent — this is a new cross-cutting concern for the modern stack
- Legacy has: `ILogger` (DB logging), `KeepAliveTask`, basic performance counters

## Key Entities
- Health check endpoints
- Metrics (request duration, error rates, cache hit rates)
- Distributed traces

## External Dependencies
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `OpenTelemetry` for metrics and tracing
- `System.Diagnostics.ActivitySource` for distributed tracing

## Migration Notes
- **Decision**: New implementation (no legacy equivalent)
- Health checks: DB connectivity, Redis connectivity, SMTP connectivity, disk space
- Metrics: request count/duration, order count, cache hit/miss ratio, queue depth
- Tracing: distributed trace context propagation for payment/shipping API calls
- Structured logging integration (see logging spec)

## Acceptance Criteria
- [ ] `/health` endpoint returns health status of DB, Redis, and SMTP
- [ ] OpenTelemetry metrics exported for key business and infrastructure metrics
- [ ] Distributed tracing spans created for external API calls (payment, shipping)
- [ ] Readiness and liveness probes available for container orchestration
