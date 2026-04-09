## Project Context

Greenfield .NET 10 rewrite of legacy .NET Framework 4.5.1 nopCommerce e-commerce platform using Strangler Fig pattern.
Specs in `ralph/specs/` define what to build. Plan in `ralph/IMPLEMENTATION_PLAN.md` tracks progress.

**Legacy Stack:** .NET Framework 4.5.1, ASP.NET MVC 5.2.3, Entity Framework 6.1.3, Autofac 4.4.0, AutoMapper 5.2.0, Newtonsoft.Json 9.0.1, StackExchange.Redis 1.2.1, SQL Server + SQL CE
**Target Stack:** .NET 10, ASP.NET Core MVC, EF Core 9/10, Microsoft.Extensions.DependencyInjection, System.Text.Json, Microsoft.Extensions.Caching (Memory + Redis), SQL Server

## Key Directories

- `ATXDocumentation/` — Comprehensive codebase analysis (READ ONLY)
- `ATXASSESSMENT/` — Migration assessment per project (READ ONLY)
- `ralph/specs/` — Component specifications (one per bounded context)
- `ralph/IMPLEMENTATION_PLAN.md` — Dynamic task tracker (Ralph manages this)
- `ralph/DISCOVERIES.md` — Cross-iteration learnings
- `src/` — Legacy source code (31 projects, 1731 C# files, ~252K LOC)

## ATXDocumentation Key Files

| File | Use When |
|------|----------|
| `ATXDocumentation/architecture/system-overview.md` | Understanding layered architecture and component interaction |
| `ATXDocumentation/architecture/dependencies.md` | Checking project-to-project and NuGet dependencies |
| `ATXDocumentation/behavior/business-logic.md` | Implementing business rules (catalog, orders, pricing, etc.) |
| `ATXDocumentation/behavior/workflows.md` | Understanding process flows (checkout, order placement) |
| `ATXDocumentation/behavior/decision-logic.md` | Decision trees and conditional logic |
| `ATXDocumentation/behavior/error-handling.md` | Exception and error patterns |
| `ATXDocumentation/reference/interfaces.md` | Understanding API contracts (183 interfaces) |
| `ATXDocumentation/reference/data-models.md` | Implementing domain entities (100+ entities) |
| `ATXDocumentation/reference/program-structure.md` | Complete code organization |
| `ATXDocumentation/migration/validation-criteria.md` | Writing acceptance tests |
| `ATXDocumentation/migration/test-specifications.md` | Test requirements |
| `ATXDocumentation/specialized/plugin-system.md` | Plugin architecture details |
| `ATXDocumentation/specialized/entity-framework-data-access.md` | EF6 patterns |
| `ATXDocumentation/specialized/aspnet-mvc-implementation.md` | MVC patterns |
| `ATXDocumentation/technical-debt/summary.md` | 9 tech debt items (2 critical, 4 high, 3 medium) |
| `ATXDocumentation/technical-debt/security-vulnerabilities.md` | CVEs and security concerns |
| `ATXDocumentation/analysis/complexity-analysis.md` | High complexity areas |

## Process

### 1. Analysis & Dependency Mapping
Identify leaf nodes (utility classes, DTOs) versus highly coupled orchestrators.
Use SharpLens `dependency_graph` and `find_references` for accurate dependency mapping.

### 2. The First Atomic Commit
Generate the full implementation for the lowest-risk item:
* **Complete:** No placeholders or `// ...`.
* **Compilable:** Standalone logic that doesn't break existing callers.
* **One Iteration:** Single logical change per loop.

### 3. Verification
Run validation commands below. All must pass before committing.

---

## Technology Stack

| Layer | Legacy | Target |
|-------|--------|--------|
| Runtime | .NET Framework 4.5.1 | .NET 10 |
| Web | ASP.NET MVC 5.2.3 | ASP.NET Core MVC |
| ORM | Entity Framework 6.1.3 | EF Core 9/10 |
| DI | Autofac 4.4.0 | Microsoft.Extensions.DependencyInjection |
| Mapping | AutoMapper 5.2.0 | Mapster or AutoMapper 13.x |
| JSON | Newtonsoft.Json 9.0.1 | System.Text.Json |
| Caching | StackExchange.Redis 1.2.1 + custom ICacheManager | IMemoryCache + IDistributedCache (Redis) |
| Validation | FluentValidation 6.x | FluentValidation 11.x |
| Email | System.Net.Mail | MailKit |
| Logging | Custom ILogger (DB) | Microsoft.Extensions.Logging + DB sink |
| Database | SQL Server + SQL CE | SQL Server only |

---

## Validation Commands

```bash
# Build
dotnet build src/NopCommerce.New.sln --no-restore

# Test
dotnet test src/NopCommerce.New.sln --no-build

# Format check
dotnet format src/NopCommerce.New.sln --verify-no-changes
```

---

## Tools

| Tool | Purpose | Key Use Case |
| :--- | :--- | :--- |
| **SharpLens MCP** | Roslyn-powered semantic analysis | Dependencies, complexity, impact analysis, dead code, refactoring |
| **`sg` (ast-grep)** | AST-aware structural search | Inheritance, attributes, method signatures |
| **`rg` (ripgrep)** | Fast regex search | Inline API calls, string-based config keys, call-site counting |
| **`fd`** | Fast file finder | Legacy file types (`.asmx`, `.svc`, `.aspx`) |
| **`jq` / `xq`** | JSON/XML parsing | `project.assets.json`, `web.config`, `.csproj` |

### SharpLens Priority Rules

Prefer SharpLens over text-based tools for semantic queries:
- use `load_solution` passing the path to .sln, to initialize the MCP
- `search_symbols` instead of `rg` for finding symbols
- `find_references` instead of `rg` for semantic references
- `get_method_source` instead of file read for viewing methods
- `analyze_change_impact` before modifying public APIs
- `find_unused_code` for dead code detection
- Call `sync_documents` after any file edit

---

## Scope Tracking

- Total components identified: 67
- Total spec files: 67
- Total plan items: 173
- Data migration items: 5
- Integration items: 15
- Cutover items: 8
- Plugin/extension count: 20 (each with individual spec and plan item)
- Last updated: 2026-04-09 iteration 7 (added Azure Blob Storage + MaxMind GeoIP2 integration items)

---

## Codebase Patterns

### Legacy Project Structure
```
src/
├── Libraries/
│   ├── Nop.Core/          (domain + infrastructure, 0 internal deps)
│   ├── Nop.Data/          (EF6, depends on Core)
│   └── Nop.Services/      (business logic, depends on Core + Data)
├── Presentation/
│   ├── Nop.Web.Framework/ (shared web infra, depends on Core + Services)
│   ├── Nop.Web/           (public storefront, 27 controllers)
│   └── Nop.Web/Administration/ (admin area, 54 controllers)
├── Plugins/               (20 plugin projects)
└── Tests/                 (5 test projects)
```

### Key Complexity Hotspots
- `OrderProcessingService.cs` — 3167 LOC, most complex business logic
- `CodeFirstInstallationService.cs` — 12269 LOC, all seed data
- `ProductController.cs` (Admin) — 4857 LOC
- `OrderController.cs` (Admin) — 4379 LOC
- `ProductService.cs` — 2142 LOC
- `WorkflowMessageService.cs` — 1920 LOC
- `ShoppingCartController.cs` (Public) — 1849 LOC
- `CheckoutController.cs` (Public) — 1788 LOC
- `ForumService.cs` — 1537 LOC
- `ExportManager.cs` — 1530 LOC, `ImportManager.cs` — 1498 LOC

### Service Pattern
All services follow: interface + implementation with constructor-injected `IRepository<T>`, `ICacheManager`, `IEventPublisher`. Virtual methods for extensibility. Cache keys as string constants.

### Plugin Pattern
Each plugin: `IPlugin` implementation + optional controller + views + DependencyRegistrar + RouteProvider. Loaded from `~/Plugins/` directory via shadow copying.
