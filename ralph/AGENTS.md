## Project Context

Greenfield .NET 10 rewrite of legacy .NET Framework application using Strangler Fig pattern.
Specs in `ralph/specs/` define what to build. Plan in `ralph/IMPLEMENTATION_PLAN.md` tracks progress.

**Legacy Stack:** {{LEGACY_STACK}}
**Target Stack:** {{TARGET_STACK}}

## Key Directories

- `ATXDocumentation/` — Comprehensive Codebase Analysis (READ ONLY)
- `ATXDocumentation/` — Comprehensive codebase analysis (READ ONLY)
- `ralph/specs/` — Component specifications (one per bounded context)
- `ralph/IMPLEMENTATION_PLAN.md` — Dynamic task tracker (Ralph manages this)

## ATXDocumentation Key Files

These are the most useful ATXDocumentation files for the build agent:

| File | Use When |
|------|----------|
| `ATXDocumentation/architecture/components.md` | Understanding component boundaries |
| `ATXDocumentation/architecture/dependencies.md` | Checking cross-project dependencies |
| `ATXDocumentation/behavior/business-logic.md` | Implementing business rules |
| `ATXDocumentation/migration/component-order.md` | Verifying migration sequence |
| `ATXDocumentation/reference/interfaces.md` | Understanding API contracts |
| `ATXDocumentation/reference/data-models.md` | Implementing domain entities |
| `ATXDocumentation/migration/validation-criteria.md` | Writing acceptance tests |

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

{{TECH_STACK}}

---

## Validation Commands

{{VALIDATION_COMMANDS}}

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
- use `load_solution` passing the path to .sln, to initializa the MCP
- `search_symbols` instead of `rg` for finding symbols
- `find_references` instead of `rg` for semantic references
- `get_method_source` instead of file read for viewing methods
- `analyze_change_impact` before modifying public APIs
- `find_unused_code` for dead code detection
- Call `sync_documents` after any file edit

---

## Scope Tracking

- Total components identified: {{COMPONENT_COUNT}}
- Total spec files: {{SPEC_COUNT}}
- Total plan items: {{PLAN_ITEM_COUNT}}
- Data migration items: {{DATA_MIGRATION_COUNT}}
- Integration items: {{INTEGRATION_COUNT}}
- Cutover items: {{CUTOVER_COUNT}}
- Plugin/extension count: {{PLUGIN_COUNT}}

---

## Codebase Patterns

(Ralph updates this section with operational learnings as the project evolves)

{{CODEBASE_PATTERNS}}
