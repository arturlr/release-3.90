# Testing Strategy

## Bounded Context
Test infrastructure — unit tests, integration tests, and end-to-end test scaffolding for the greenfield rewrite.

## Legacy Source
- `src/Tests/Nop.Core.Tests/` — core domain and infrastructure tests
- `src/Tests/Nop.Data.Tests/` — data layer tests
- `src/Tests/Nop.Services.Tests/` — service layer tests (largest: OrderProcessingServiceTests)
- `src/Tests/Nop.Tests/` — shared test utilities
- `src/Tests/Nop.Web.MVC.Tests/` — controller/routing tests
- `ATXDocumentation/migration/test-specifications.md` — migration test requirements

## Key Entities
- xUnit test projects (one per solution layer)
- Test fixtures and shared utilities
- In-memory database provider for integration tests
- Mock/stub infrastructure for service dependencies

## External Dependencies
- xUnit 2.x (test framework)
- Moq or NSubstitute (mocking)
- FluentAssertions (assertion library)
- Microsoft.EntityFrameworkCore.InMemory (integration tests)
- Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory for E2E)

## Migration Notes
- **Decision**: Rewrite test projects alongside implementation
- Legacy uses NUnit + Rhino Mocks → migrate to xUnit + Moq/NSubstitute
- Coverage target: 70%+ for service layer business logic
- Critical paths: pricing calculations, order processing, tax computation, discount application
- Performance baselines: homepage <2s, product page <1.5s, checkout flow <5s total

## Acceptance Criteria
- [ ] Unit test project exists per solution layer (Core, Data, Services, Web)
- [ ] Service layer tests cover pricing, order processing, tax, and discount logic
- [ ] Integration tests verify EF Core repository CRUD against in-memory or test database
- [ ] WebApplicationFactory-based tests verify critical controller endpoints return correct status codes
