0a. Study `ralph/AGENTS.md` for project constraints and validation commands.

0b. Study `ralph/IMPLEMENTATION_PLAN.md` to understand what needs to be done.

0c. Read `ralph/DISCOVERIES.md` for cross-iteration learnings (if it exists).

0d. Study `ralph/specs/*` as needed for the current task.

0e. Knowledge bases (specs, plan, discoveries) are auto-indexed by the agent config.
    Use `knowledge search` with natural language to find relevant context
    (e.g., "which spec covers authentication?", "what failed approaches were tried?").

1. Choose the most important incomplete item from `ralph/IMPLEMENTATION_PLAN.md`.
   Before making changes, search the codebase — don't assume something is missing without checking the documentation and assessement.

2. Implement that ONE item. Follow patterns established in existing code and
   `ralph/AGENTS.md`. Implement completely — no placeholders, no stubs.

3. After implementing, run validation commands from `ralph/AGENTS.md`:
   - Build must succeed
   - Tests must pass
   - Fix any failures before proceeding

4. Update `ralph/IMPLEMENTATION_PLAN.md`:
   - Mark the completed item as `[x]`
   - Add any newly discovered issues or bugs
   - Note learnings that affect future items

5. Update `ralph/AGENTS.md` if you learned something operational (keep it brief).

6. Append to `ralph/DISCOVERIES.md` (create if missing) any findings that future
   iterations should know: hidden dependencies, failed approaches, pattern decisions,
   cross-cutting findings. Use format: `## Iteration — [component] / Finding / Impact / Action`.

7. `git add -A && git commit -m "[ralph] <description of what was implemented>"`

## CRITICAL RULES (never violate)
- Implement completely. Placeholders and stubs waste time redoing work.
- ONE item per loop. Do not try to do multiple items.
- The codebase must compile after every commit.
- When IMPLEMENTATION_PLAN.md gets large, clean out completed items.
- If you find inconsistencies in specs/*, fix them and note in the plan.
- For bugs you notice, resolve them or document them in IMPLEMENTATION_PLAN.md.
- Keep AGENTS.md operational only — progress notes belong in IMPLEMENTATION_PLAN.md.
