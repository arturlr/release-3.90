#!/bin/bash
set -euo pipefail

# =============================================================================
# Ralph Loop for .NET Greenfield Rewrite
# =============================================================================
#
# Usage:
#   ./ralph_loop.sh              # Build mode, unlimited iterations
#   ./ralph_loop.sh plan         # Planning mode, unlimited
#   ./ralph_loop.sh plan 5       # Planning mode, max 5 iterations
#   ./ralph_loop.sh 20           # Build mode, max 20 iterations
#   ./ralph_loop.sh --dry-run    # Build mode, 1 iteration, no commit/push
#   ./ralph_loop.sh plan --dry-run  # Plan mode, 1 iteration, no commit/push
#
# Environment:
#   MAX_FAILURES=N  — consecutive failures before abort (default: 5)
#
# Modes:
#   plan  — Reads ATXDocumentation + CLI tools, generates/updates specs and
#           IMPLEMENTATION_PLAN.md. No code changes. Run this first.
#   build — Reads plan, picks one item, implements it, validates, commits.
#           This is the main work loop.
#
# =============================================================================

CURRENT_DIR="$(pwd)"
RALPH_DIR="$CURRENT_DIR/ralph"

if [ ! -d ".git" ]; then
  git init && git add . && git commit -m "Initial commit"
fi

# ATX Documentation
if [[ ! -d "${CURRENT_DIR}/ATXDocumentation" ]]; then
    echo "Generating aplication assessment..."
    unset AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN
    export AWS_PROFILE=atx # AWS profile to be used
    export AWS_REGION=us-east-1 # Region for ATX
    export ATX_SHELL_TIMEOUT=43200 # 12 hours in seconds
    atx custom def exec -n "AWS/early-access-comprehensive-codebase-analysis" -p . -x -t
    echo "Assessment completed"
fi

# Detect solution file for SharpLens MCP
SLN_FILE=$(find "$CURRENT_DIR" -maxdepth 2 -name '*.sln' -o -name '*.slnx' 2>/dev/null | head -1)
if [ -n "$SLN_FILE" ]; then
    export DOTNET_SOLUTION_PATH="$SLN_FILE"
    echo "SharpLens solution: $SLN_FILE"
else
    echo "⚠ No .sln/.slnx found — SharpLens will require manual load_solution call."
fi

# --- Parse arguments ---
DRY_RUN=false
for arg in "$@"; do
    [ "$arg" = "--dry-run" ] && DRY_RUN=true
done

if [ "${1:-}" = "plan" ]; then
    MODE="plan"
    PROMPT_FILE="$RALPH_DIR/PROMPT_plan.md"
    AGENT="dotnet-ralph-planner"
    if [ "$DRY_RUN" = true ]; then MAX_ITERATIONS=1; else MAX_ITERATIONS=${2:-0}; fi
elif [[ "${1:-}" =~ ^[0-9]+$ ]]; then
    MODE="build"
    PROMPT_FILE="$RALPH_DIR/PROMPT_build.md"
    AGENT="default"
    if [ "$DRY_RUN" = true ]; then MAX_ITERATIONS=1; else MAX_ITERATIONS=$1; fi
else
    MODE="build"
    PROMPT_FILE="$RALPH_DIR/PROMPT_build.md"
    AGENT="default"
    if [ "$DRY_RUN" = true ]; then MAX_ITERATIONS=1; else MAX_ITERATIONS=0; fi
fi

ITERATION=0
CONSECUTIVE_FAILURES=0
MAX_CONSECUTIVE_FAILURES=${MAX_FAILURES:-5}
BRANCH=$(git branch --show-current)

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Ralph Loop — .NET Greenfield Rewrite"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Mode:   $MODE"
echo "Agent:  $AGENT"
echo "Prompt: $PROMPT_FILE"
echo "Branch: $BRANCH"
[ $MAX_ITERATIONS -gt 0 ] && echo "Max:    $MAX_ITERATIONS iterations"
[ "$DRY_RUN" = true ] && echo "Dry run: yes (no commit/push)"
[ -n "${DOTNET_SOLUTION_PATH:-}" ] && echo "Solution: $DOTNET_SOLUTION_PATH"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# --- Preflight checks ---
if [ ! -f "$PROMPT_FILE" ]; then
    echo "Error: $PROMPT_FILE not found"
    exit 1
fi

if [ ! -d ".git" ]; then
    echo "Error: Not a git repository. Run 'git init' first."
    exit 1
fi

# Seed discovery log if missing
if [ ! -f "$RALPH_DIR/DISCOVERIES.md" ]; then
    echo "# Ralph Discovery Log" > "$RALPH_DIR/DISCOVERIES.md"
    echo "" >> "$RALPH_DIR/DISCOVERIES.md"
    echo "Append-only log of cross-iteration findings. Never edit or remove previous entries." >> "$RALPH_DIR/DISCOVERIES.md"
    echo "" >> "$RALPH_DIR/DISCOVERIES.md"
fi

# --- Main loop ---
while true; do
    if [ $MAX_ITERATIONS -gt 0 ] && [ $ITERATION -ge $MAX_ITERATIONS ]; then
        echo ""
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "  Reached max iterations: $MAX_ITERATIONS"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        break
    fi

    ITERATION=$((ITERATION + 1))
    echo ""
    echo "======================== ITERATION $ITERATION ========================"
    echo ""

    # Run one Ralph iteration with fresh context
    if cat "$PROMPT_FILE" | kiro-cli chat \
        --agent "$AGENT" \
        --no-interactive \
        --trust-all-tools; then
        CONSECUTIVE_FAILURES=0
    else
        CONSECUTIVE_FAILURES=$((CONSECUTIVE_FAILURES + 1))
        echo "⚠ Iteration $ITERATION failed ($CONSECUTIVE_FAILURES/$MAX_CONSECUTIVE_FAILURES). Resetting to last good commit."
        git reset --hard HEAD
        if [ $CONSECUTIVE_FAILURES -ge $MAX_CONSECUTIVE_FAILURES ]; then
            echo "✖ $MAX_CONSECUTIVE_FAILURES consecutive failures — aborting loop."
            exit 1
        fi
        continue
    fi

    # Progress summary
    PLAN_FILE="ralph/IMPLEMENTATION_PLAN.md"
    if [ -f "$PLAN_FILE" ]; then
        DONE=$(grep -c '\[x\]' "$PLAN_FILE" 2>/dev/null || echo 0)
        PENDING=$(grep -c '\[ \]' "$PLAN_FILE" 2>/dev/null || echo 0)
        echo "📊 Plan: $DONE done, $PENDING pending"
    fi

    echo ""
    echo "======================== ITERATION $ITERATION COMPLETE ========================"
done
