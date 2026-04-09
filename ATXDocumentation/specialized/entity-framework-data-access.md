# Entity Framework Data Access

## Overview
nopCommerce uses Entity Framework 6.1.3 (Code First) as its ORM layer, providing data access abstraction through the Repository pattern.

## DbContext Configuration

### NopObjectContext
**Location**: `Nop.Data.NopObjectContext`  
**Inherits**: `DbContext`

**Key Features**:
- Lazy loading disabled by default
- Proxy creation disabled
- Auto-detect changes disabled for performance
- Configured via Fluent API (no attributes)

**Connection Management**:
- Connection string from configuration
- Support for SQL Server and SQL CE
- Database initialization strategies configurable

## Entity Mappings

### Fluent API Configuration
**Pattern Used**: EntityTypeConfiguration<T>

**Example**:
```csharp
public class ProductMap : EntityTypeConfiguration<Product>
{
    public ProductMap()
    {
        this.ToTable("Product");
        this.HasKey(p => p.Id);
        this.Property(p => p.Name).IsRequired().HasMaxLength(400);
        this.Property(p => p.Price).HasPrecision(18, 4);
        
        // Relationships
        this.HasMany(p => p.ProductCategories)
            .WithRequired(pc => pc.Product)
            .HasForeignKey(pc => pc.ProductId);
    }
}
```

### Mapping Conventions
- All entities inherit from `BaseEntity`
- Primary keys named `Id` (int, identity)
- Foreign keys follow naming: `{EntityName}Id`
- Many-to-many through junction tables
- DateTime stored as UTC

## Repository Pattern

### IRepository<T> Interface
**Generic repository for all entities**

**Key Methods**:
- `GetById(id)` - Single entity retrieval
- `Table` - IQueryable for LINQ queries
- `TableNoTracking` - Detached queries for read-only
- `Insert(entity)` - Add single entity
- `Insert(entities)` - Bulk insert
- `Update(entity)` - Modify entity
- `Delete(entity)` - Remove entity

### EfRepository<T> Implementation
**Features**:
- Automatic context injection
- Change tracking for updates
- SaveChanges called per operation
- Exception handling and logging

## Query Patterns

### Common Patterns
```csharp
// Simple query
var product = _productRepository.GetById(productId);

// LINQ query with filtering
var products = _productRepository.Table
    .Where(p => p.Published && !p.Deleted)
    .OrderBy(p => p.DisplayOrder)
    .ToList();

// No-tracking for read-only
var productNames = _productRepository.TableNoTracking
    .Where(p => p.Published)
    .Select(p => p.Name)
    .ToList();

// Eager loading
var products = _productRepository.Table
    .Include(p => p.ProductPictures)
    .Include(p => p.ProductCategories.Select(pc => pc.Category))
    .ToList();
```

## Performance Considerations

### Optimization Strategies
1. **No-Tracking Queries**: Use `TableNoTracking` for read-only operations
2. **Explicit Loading**: Load related entities only when needed
3. **Projection**: Select only required columns
4. **Compiled Queries**: Not extensively used (EF 6 limitation)
5. **Caching**: Service layer caches frequent queries

### N+1 Query Prevention
- Use `.Include()` for eager loading
- Avoid lazy loading (disabled by default)
- Batch queries where possible

## Database Migrations

### Code First Migrations
**Approach**: Manual migrations

**Migration Commands**:
```
Enable-Migrations
Add-Migration MigrationName
Update-Database
```

**Migration Strategy**:
- Migrations stored in `Nop.Data/Migrations`
- Applied during application startup
- Supports upgrade scripts for existing installations

## Connection Resilience

### Retry Logic
- No built-in retry (EF 6 limitation)
- Application-level retry for transient failures
- Connection timeout configurable

### Transaction Management
- Implicit transactions per SaveChanges
- Explicit transactions for complex operations
- Transaction scope for distributed transactions

## Database Support

### SQL Server
- Primary database platform
- Full feature support
- Optimized queries
- Stored procedures for complex operations

### SQL Server Compact Edition
- Lightweight option (deprecated)
- Limited to 4GB
- No stored procedures
- Single-user limitations

## Related Documentation
- [Data Models](../reference/data-models.md)
- [Program Structure](../reference/program-structure.md)

**Version**: 1.0
