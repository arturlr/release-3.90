# Dependency Analysis

## External Dependencies Summary
Total NuGet Packages: ~30

### Critical Dependencies
- **Entity Framework 6.1.3**: ORM framework
- **Autofac 4.4.0**: IoC container
- **Newtonsoft.Json 9.0.1**: JSON serialization
- **AutoMapper 5.2.0**: Object mapping

### Supporting Dependencies
- **StackExchange.Redis 1.2.1**: Caching
- **Microsoft.AspNet.Mvc 5.2.3**: Web framework
- **FluentValidation**: Input validation
- **log4net** or similar: Logging

## Internal Dependencies
- Core → No dependencies
- Data → Depends on Core
- Services → Depends on Core, Data
- Web → Depends on Services, Core
- Plugins → Depends on Core, Services

## Dependency Graph
```
Core (Foundation)
  ↓
Data (Repository)
  ↓
Services (Business Logic)
  ↓
Web/Admin (Presentation)
  ↓
Plugins (Extensions)
```

## Circular Dependencies
**None detected** - Clean layered architecture

## Third-Party Risk
- Multiple outdated dependencies (see Technical Debt)
- High coupling to Entity Framework 6
- Moderate coupling to Autofac
- Low risk from other dependencies

**Version**: 1.0
