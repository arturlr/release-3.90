---
name: dotnet-ralph-planner
description: >
  Ralph loop planning skill for .NET Framework → .NET 10 greenfield rewrite.
  Reads ATXDocumentation for architecture/business/dependency insights, uses
  CLI tools (sg, rg, jq) for static analysis, and produces ralph artifacts
  (AGENTS.md, specs, IMPLEMENTATION_PLAN.md) with strict scope integrity
  enforcement. Runs iteratively with fresh context per iteration.
---

# .NET Ralph Loop Planner Skill

Plans a greenfield .NET 10 rewrite using the Ralph Wiggum loop technique.
Each iteration gets fresh context, reads shared state from disk, performs
planning work, verifies scope integrity, and exits.

---

## How This Works (Three-Phase Model)

This skill runs inside a `while` loop. Each iteration detects its phase:

**Scaffold Phase 1** (no `ralph/AGENTS.md` exists):
1. Deep-reads ATXDocumentation (priority 1-4 only: README, component-order, components, dependencies)
2. Runs CLI validation to confirm project structure and dependencies
3. Generates `ralph/AGENTS.md` with discovered tech stack, project list, and scope tracking
4. Generates skeleton `ralph/IMPLEMENTATION_PLAN.md` with phase headers and counts only (no specs yet)
5. Commits with inventory counts

**Scaffold Phase 2** (`AGENTS.md` exists but specs are incomplete):
1. Reads `ralph/AGENTS.md` to get the component inventory
2. Reads remaining ATXDocumentation (priority 5-8: business-logic, remediation, complexity, patterns)
3. Picks ONE layer/category that has no specs yet (e.g., "cross-cutting concerns", "controllers A-M")
4. Generates specs for that layer only (≤15 specs per iteration)
5. Updates `IMPLEMENTATION_PLAN.md` with plan items for the new specs
6. Runs NuGet compatibility check if not yet done
7. Commits with updated scope counts

Repeat Scaffold Phase 2 until all components have specs.

**Refine Phase** (all components have specs):
1. Reads current specs, plan, and AGENTS.md
2. Gap analysis: missing specs, orphan plan items, weak acceptance criteria
3. Targeted CLI queries on unclear areas
4. Updates artifacts — may ADD/SPLIT/REORDER, may NOT remove or merge
5. Runs scope verification checkpoint, commits

**You do NOT implement anything. You only plan.**

---

## ATXDocumentation Reading Order

ATXDocumentation contains comprehensive static analysis of the legacy codebase.
Read these files in this order during the scaffold phase:

| Priority | File | What to Extract |
|----------|------|-----------------|
| 1 | `ATXDocumentation/README.md` | Catalog, tech stack, critical findings |
| 2 | `ATXDocumentation/migration/component-order.md` | Migration sequence, dependencies |
| 3 | `ATXDocumentation/architecture/components.md` | Every component in the system |
| 4 | `ATXDocumentation/architecture/dependencies.md` | Project-to-project and external deps |
| 5 | `ATXDocumentation/behavior/business-logic.md` | Business rules per component |
| 6 | `ATXDocumentation/technical-debt/remediation-plan.md` | Priorities and risks |
| 7 | `ATXDocumentation/analysis/complexity-analysis.md` | Complexity per component |
| 8 | `ATXDocumentation/architecture/patterns.md` | Design patterns in use |

These files are **READ ONLY** — never modify anything under `ATXDocumentation/`.

---

## CLI Tools for Static Analysis

Use these CLI tools for codebase analysis (SharpLens MCP is not compatible with .NET Framework):

| Task | Command |
|------|---------|
| Project structure | `fd -e csproj` |
| Project dependencies | `rg '<ProjectReference' -g '*.csproj'` |
| Target frameworks | `rg '<TargetFramework' -g '*.csproj'` |
| NuGet packages | `rg '<PackageReference Include="([^"]+)"' -o -r '$1' -g '*.csproj'` |
| Symbol search | `rg 'class\|interface\|enum' -g '*.cs'` |
| Reference finding | `rg 'using Namespace' -g '*.cs'` |
| Legacy file types | `fd -e asmx -e svc -e aspx` |
| Namespace counting | `rg -c 'using System\.(Web\|Configuration\|ServiceModel)' -g '*.cs'` |
| God class detection | `find . -name '*.cs' -exec wc -l {} + \| sort -rn \| head -20` |
| AST patterns | `sg` for class inheritance, attributes, structural patterns |

---

## Step 0: Orient

Read these files every iteration:

```
ralph/AGENTS.md                  ← operational constraints, scope tracking
ralph/IMPLEMENTATION_PLAN.md     ← current plan state (may not exist on first run)
ralph/DISCOVERIES.md             ← cross-iteration discovery log (may not exist yet)
ATXDocumentation/README.md       ← documentation catalog (READ ONLY)
```

### Knowledge Base (auto-synced)

The agent config auto-indexes these as persistent knowledge bases on load:
- `ralph-specs` — all spec files (semantic, auto-updated)
- `ralph-plan` — implementation plan (semantic, auto-updated)
- `ralph-discoveries` — cross-iteration findings (semantic, auto-updated)
- `atx-docs` — ATXDocumentation (semantic, indexed once)

Use `knowledge search` with natural language queries instead of reading entire files
when looking for specific information (e.g., "which component uses Redis?",
"what did we learn about OrderService?").

Then determine phase based on whether specs and plan exist.

---

## Step 1: Gap Analysis (Refine Phase)

Compare `ralph/specs/*` against ATXDocumentation components:

- Does every component in `ATXDocumentation/architecture/components.md` have a spec?
- Does every spec have at least one plan item in `IMPLEMENTATION_PLAN.md`?
- Does every spec have at least 3 acceptance criteria?
- Are there plan items that don't trace back to a spec?
- Are sizing constraints met (no component >50 input files or >15 output files per task)?
- Are data migration items present for every data store?
- Are integration items present for every external service?
- Are all 8 cutover items present?

---

## Step 2: Update IMPLEMENTATION_PLAN.md

Create or update `ralph/IMPLEMENTATION_PLAN.md` as a prioritized bullet list.

Each item:
```
- [ ] [Phase.N] Brief description
  - Spec: specs/[filename].md
  - Scope: [projects/files affected]
  - Depends on: [other items if any]
```

Mark completed items:
```
- [x] [Done] Description of completed item
```

### Sizing Rules

Size plan items for what one Ralph iteration can reliably complete and verify:

- **Legacy input ceiling:** ≤50 source files to read/understand per plan item
  - >50 files → split by sub-domain, feature module, or namespace
- **Output ceiling:** ≤15 new files produced per plan item
  - If a component needs more, split into interface/contracts + implementation items
- **Complexity multiplier:** Components rated HIGH in ATXDocumentation complexity
  analysis get halved limits (≤25 input files, ≤8 output files)
- Each plugin/extension → 1 dedicated item (enumerate individually)
- Each data store → 1 data migration item
- Each external integration → 1 integration item
- Each cross-cutting concern → 1 dedicated item (do not combine)
- **God classes (>500 LOC) → dedicated decomposition item** before the component migration item

### Sequencing (leaf-to-root, lowest risk first)

1. Foundation (solution skeleton, CI/CD, build infrastructure)
2. DI infrastructure (service registration modules, lifetime management, factory patterns)
3. Cross-cutting concerns (each separately: auth, logging, caching, etc.)
4. Strangler facade setup
5. Data layer decomposition (see below)
6. Read-only / reporting components
7. Simple CRUD bounded contexts
8. Core business logic
9. Complex domain components
10. Plugins/extensions (each individually)
11. External integrations (each individually)
12. Data migrations (one per data store)
13. Cutover items (all 8 required)

### Data Layer Decomposition

For large monoliths, the data layer requires multiple dedicated plan items:

- **DbContext decomposition** — one bounded DbContext per domain area (not one monolithic context)
- **Entity configuration migration** — Fluent API mappings, conventions, value converters
- **Stored procedure replacement** — one item per SP cluster or domain area
- **Seed data / reference data** — lookup tables, enums-as-tables, initial state
- **Raw SQL / Dapper queries** — inventory and migrate outside-of-ORM data access

Each gets its own plan item. Do not combine into a single "migrate data layer" item.

### Fixed Cutover Items (always present)

These must always appear in the plan:
- Facade setup
- Facade flip
- Smoke tests
- Go/no-go criteria
- Rollback plan
- Legacy decommission
- DNS/routing cutover
- Monitoring setup

---

## Step 3: Create Missing Specs

If ATXDocumentation reveals components that have no spec file in `ralph/specs/`,
create the spec. Use this format:

```markdown
# [Component Name]

## Bounded Context
[What domain this covers]

## Legacy Source
[Specific project paths and namespaces in the legacy codebase]

## Key Entities
[Domain objects, aggregates, value objects]

## View Layer
[Only for components with Razor pages/views]
- Pages/Views: [list of .cshtml files and routes]
- Layout chain: [_Layout.cshtml → _ViewStart → partials]
- Tag helpers: [custom tag helpers used]
- JS/CSS bundles: [bundled assets this component depends on]
- Strategy: [Keep as Razor Pages | Rewrite as API + SPA | Convert to Razor Components]

## External Dependencies
[Third-party packages, services, APIs]

## Migration Notes
[From ATXDocumentation: complexity, risks, recommended approach]

## Acceptance Criteria
- [ ] [Observable outcome 1]
- [ ] [Observable outcome 2]
- [ ] [Observable outcome 3]
```

Populate fields from ATXDocumentation:
- Legacy Source → from `architecture/components.md` and `architecture/dependencies.md`
- Key Entities → from `reference/data-models.md` and `behavior/business-logic.md`
- External Dependencies → from `architecture/dependencies.md`
- Migration Notes → from `technical-debt/remediation-plan.md` and `migration/component-order.md`
- Acceptance Criteria → from `migration/validation-criteria.md` and `migration/test-specifications.md`

---

## Step 4: Check NuGet Package Compatibility

Extract every third-party NuGet package in the solution and verify .NET 8+ compatibility.

1. Get package names via CLI:

```bash
rg '<PackageReference Include="([^"]+)"' -o -r '$1' --no-filename -g '*.csproj' | sort -u
```

2. Run the compatibility checker with the extracted names:

```bash
python3 .kiro/skills/dotnet-ralph-planner/references/check_nuget_compat.py Package1 Package2 ... > ./ralph/packages-report.md
```

3. Use the output to populate `External Dependencies` and `Migration Notes` in each spec — flag packages marked `❌ NO` as requiring replacement or rewrite.

---

## Step 5: Record Discoveries

Before committing, append any findings to `ralph/DISCOVERIES.md` (create if missing).
This is an **append-only** log — never edit or remove previous entries.

Format:
```markdown
## Iteration N — [phase] — YYYY-MM-DD

### [Component or topic]
- **Finding:** [what you discovered]
- **Impact:** [which specs/plan items are affected]
- **Action:** [what you did about it, or what a future iteration should do]
```

Record anything a future iteration would benefit from knowing:
- Hidden dependencies not visible in ATXDocumentation
- Failed approaches and why they failed
- Pattern decisions and rationale
- Cross-cutting findings that affect multiple components
- Corrections to ATXDocumentation assumptions

---

## Step 6: Commit

```bash
git add -A && git commit -m "[ralph-plan] <phase>: <what was updated>"
```

---

## Scope Integrity (Contract)

These rules are enforced at every iteration. Violation is a bug.

1. **1:1 Component-to-Spec:** Every component in ATXDocumentation gets its own spec. No merging.
2. **Exhaustive Plan Coverage:** Every spec must have at least one plan item.
3. **Explicit Data Migration:** One plan item per data store/database/cache/external state.
4. **Explicit Integrations:** One plan item per external service/API/message queue.
5. **Plugin Enumeration:** Each plugin gets its own spec and plan item. "Migrate all plugins" is forbidden.
6. **Cross-Cutting Separation:** Security, authentication, authorization, logging, caching, localization, error handling, observability — each gets its own spec and plan item.
7. **Cutover Completeness:** All 8 cutover items must be present.
8. **Acceptance Criteria Minimum:** Every spec must have ≥3 acceptance criteria.
9. **No Complexity Filtering:** LOW complexity components get full specs and plan items.
10. **Monotonic Scope:** Scope counts never decrease between iterations. Add, split, reorder only — never remove or merge.

---

## Verification Queries

Run these to verify scope integrity before committing:

```bash
# Count spec files (excluding README)
ls ralph/specs/*.md | grep -v README | wc -l

# Count plan items
grep -c '\[ \]' ralph/IMPLEMENTATION_PLAN.md

# Find orphan specs (spec exists but no plan item references it)
for f in ralph/specs/*.md; do
  [ "$(basename "$f")" = "README.md" ] && continue
  name=$(basename "$f" .md)
  grep -q "$name" ralph/IMPLEMENTATION_PLAN.md || echo "ORPHAN SPEC: $f"
done

# Find plan items missing spec reference (excluding cutover)
grep '\[ \]' ralph/IMPLEMENTATION_PLAN.md | grep -v 'Cutover\|cutover' | grep -v 'Spec:' && echo "PLAN ITEMS MISSING SPEC REFERENCE"

# Count cutover items
grep -c -i 'cutover\|facade.*flip\|smoke.*test\|go.no.go\|rollback\|decommission\|dns.*routing\|monitoring.*setup' ralph/IMPLEMENTATION_PLAN.md

# Specs with fewer than 3 acceptance criteria
for f in ralph/specs/*.md; do
  [ "$(basename "$f")" = "README.md" ] && continue
  count=$(grep -c '\[ \]' "$f" 2>/dev/null || echo 0)
  [ "$count" -lt 3 ] && echo "WEAK SPEC ($count criteria): $f"
done
```

---

## Constraints

- **Phase-based work:** Scaffold Phase 1 does inventory + skeleton. Scaffold Phase 2 generates specs one layer at a time (≤15 per iteration). Refine phase does gap analysis and improvement.
- **Build verification checkpoints:** After completing all plan items in a sequencing phase (e.g., all cross-cutting concerns done), the next build iteration must run `dotnet build` and `dotnet test` on the greenfield solution before starting the next phase. Failed builds block forward progress.
- Do NOT create implementation code, source files, or project files.
- Do NOT assume something is missing — use CLI tools to verify.
- ATXDocumentation is READ ONLY.
- Keep `ralph/AGENTS.md` operational — progress notes belong in `IMPLEMENTATION_PLAN.md`.
- If you learn something operational, update `ralph/AGENTS.md` Codebase Patterns section.
