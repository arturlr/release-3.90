# Ralph Loop — Getting Started

## Prerequisites

- `kiro-cli` installed and configured
- `git` initialized in the target legacy .NET repo
- AWS CLI configured with an `atx` profile (used by ATX documentation generation)
- CLI tools: `sg`, `rg`, `fd`, `jq` (see main README for install commands)

> **Note:** The assessment and ATX documentation steps are now automated by the loop script. You no longer need to run them manually.

---

## Step 1: Bootstrap specs

Create component specs in `ralph/specs/`. Start with one file per bounded context or domain component (e.g., `foundation.md`, `strangler-facade.md`, `customer-management.md`). See `ralph/specs/README.md` for the spec format.

Specs are the static source of truth for what Ralph builds. You can write them manually or let the planning mode generate them from the assessment output.

---

## Step 2: Planning mode

Generates or updates `ralph/IMPLEMENTATION_PLAN.md` by reading specs + assessment artifacts. No code changes happen in this mode.

```bash
./ralph-loop.sh plan 5       # 5 planning iterations
./ralph-loop.sh plan          # unlimited planning iterations (Ctrl+C to stop)
```

Each iteration spawns a fresh agent session that:
1. Reads `ralph/AGENTS.md` + `ralph/specs/*` + `ATXDocumentation/*`
2. Creates/updates `ralph/IMPLEMENTATION_PLAN.md`
3. Creates missing spec files for components found in the assessment
4. Commits and pushes changes

---

## Step 3: Review the plan

```bash
cat ralph/IMPLEMENTATION_PLAN.md
```

Adjust specs if the plan doesn't look right. The plan is disposable — delete `ralph/IMPLEMENTATION_PLAN.md` and re-run planning mode anytime.

---

## Step 4: Building mode

This is where Ralph writes code — one item per iteration:

```bash
./ralph-loop.sh 30            # max 30 iterations
./ralph-loop.sh               # unlimited iterations (Ctrl+C to stop)
```

Each iteration:
1. Fresh agent session (no memory of prior loops)
2. Reads `ralph/AGENTS.md` + `ralph/IMPLEMENTATION_PLAN.md`
3. Picks the most important pending item
4. Implements it completely (no placeholders or stubs)
5. Runs validation (`dotnet build`, `dotnet test`)
6. Marks item as done, commits, and pushes
7. Exits → loop restarts

---

## Step 5: Monitor & course-correct

- Watch progress: `git log --oneline`
- Stop anytime: `Ctrl+C`
- Resume: `./ralph-loop.sh 30` (picks up where it left off)
- If Ralph drifts: delete `ralph/IMPLEMENTATION_PLAN.md`, re-run `./ralph-loop.sh plan 5`

---

## What the script automates

The `ralph-loop.sh` script handles one pre-flight step before entering the main loop:

1. **ATX Documentation** — If `ATXDocumentation/` doesn't exist, runs the ATX custom definition to generate comprehensive codebase analysis.

This step is skipped on subsequent runs if the output directory already exists.

---

## File structure

```
ralph/
├── AGENTS.md                   # Operational guide (Ralph updates this)
├── IMPLEMENTATION_PLAN.md      # Dynamic task tracker (Ralph manages this)
├── DISCOVERIES.md              # Append-only cross-iteration learning log
├── PROMPT_plan.md              # Planning mode instructions
├── PROMPT_build.md             # Building mode instructions
├── specs/                      # One spec per component (source of truth)
│   └── README.md               # Spec format reference
└── support/                    # Automated setup resources
    └── run_atxdocumentation.sh # ATX codebase analysis script
```
