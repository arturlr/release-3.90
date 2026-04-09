# Data Migration — SQL Server

## Bounded Context
Data migration from legacy SQL Server database to new .NET 10 EF Core schema.

## Legacy Source
- SQL Server database with EF6-generated schema
- All entity tables, junction tables, indexes, and stored procedures

## Key Entities
- All domain entity tables (~100+ tables)
- Junction tables (Product_ProductTag_Mapping, etc.)
- Indexes and constraints

## External Dependencies
- SQL Server (source and target)
- EF Core Migrations

## Migration Notes
- **Decision**: Schema migration via EF Core Migrations + data migration scripts
- Schema differences: EF6 conventions vs EF Core conventions (table names, FK naming)
- `AttributesXml` columns → convert XML to JSON during migration
- Password hashes: keep existing hashes, add format identifier for legacy verification
- Plugin-specific tables: each plugin may have custom tables
- Consider: blue-green database approach (new schema alongside old)

## Acceptance Criteria
- [ ] EF Core migrations create complete schema matching all domain entities
- [ ] Data migration scripts transfer all records with zero data loss
- [ ] `AttributesXml` data converted to JSON format
- [ ] Legacy password hashes preserved with format identifier for backward-compatible verification
- [ ] Foreign key relationships and indexes match or improve upon legacy schema
