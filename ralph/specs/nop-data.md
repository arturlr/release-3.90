# Nop.Data — Data Access Layer

## Bounded Context
Entity Framework data access — DbContext, repository implementation, entity mappings, data providers, and database initialization.

## Legacy Source
- `src/Libraries/Nop.Data/` — NopObjectContext, EfRepository<T>, data providers
- `src/Libraries/Nop.Data/Mapping/` — 16 subdirectories with Fluent API entity configurations
- Namespaces: `Nop.Data`, `Nop.Data.Mapping.*`

## Key Entities
- `NopObjectContext` (DbContext) → becomes EF Core `DbContext`
- `EfRepository<T>` — generic repository implementation
- `IDbContext` — context abstraction
- `IDataProvider` — SQL Server / SQL CE provider abstraction
- Entity type configurations (one per domain entity, ~100+ mapping classes)

## External Dependencies
- EntityFramework 6.1.3 → `Microsoft.EntityFrameworkCore` 9.x (or 10.x preview)
- EntityFramework.SqlServerCompact 6.1.3 → remove (SQL CE deprecated)
- Microsoft.SqlServer.Compact 4.0.8876.1 → remove

## Migration Notes
- **Decision**: Rewrite
- EF6 Fluent API → EF Core Fluent API (similar but different namespace/methods)
- SQL CE support dropped entirely; SQL Server only (with option for PostgreSQL later)
- `NopObjectContext` → `NopDbContext : DbContext` with `OnModelCreating` applying all configurations
- `EfRepository<T>` → implement `IRepository<T>` using EF Core `DbSet<T>`
- Mapping classes → `IEntityTypeConfiguration<T>` implementations
- Remove `ObjectContext` stored procedure helpers; use raw SQL or EF Core interceptors
- Add migration support via EF Core Migrations

## Acceptance Criteria
- [ ] `NopDbContext` inherits from EF Core `DbContext` and applies all entity configurations
- [ ] `EfRepository<T>` implements `IRepository<T>` with `GetById`, `Table`, `Insert`, `Update`, `Delete`
- [ ] All 100+ entity mappings have corresponding `IEntityTypeConfiguration<T>` implementations
- [ ] Database can be created from scratch via EF Core migrations
- [ ] SQL Server provider works; SQL CE is not supported
