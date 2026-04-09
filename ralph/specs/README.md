# Specs Directory

One markdown file per component or bounded context. These are the **source of truth**
for what Ralph builds. The `IMPLEMENTATION_PLAN.md` tracks what's left to do — these
specs define what "done" looks like.

## Spec Format

```markdown
# [Component Name]

## Bounded Context
What domain this covers.

## Legacy Source
Projects/namespaces in the legacy codebase.

## Key Entities
Domain objects, aggregates, value objects.

## External Dependencies
Third-party packages, services, APIs.

## Migration Notes
From assessment: decision (Replace/Rewrite/Wrap), complexity, risks.

## Acceptance Criteria
- [ ] Observable outcome that proves this component works
- [ ] Another verifiable outcome
```

## How Specs Are Created

1. **Bootstrap from assessment:** Run the planner in `plan` mode. It reads
   `migration/*` and creates specs for components it finds.
2. **Manual creation:** Write specs for components you know about.
3. **Ralph-discovered:** During planning iterations, Ralph may create specs
   for components it identifies as missing.

## Rules

- One spec per bounded context / component
- Keep specs focused — if you need "and" to describe it, split it
- Acceptance criteria must be observable and verifiable
- Specs are the static source of truth; the plan is the dynamic tracker
