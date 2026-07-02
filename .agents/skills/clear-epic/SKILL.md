---
name: clear-epic
description: Orchestrate end-to-end clearance of every sub-issue under a parent/epic GitHub issue — fan out parallel, same-tier, max-effort worker sub-agents in dependency order, each gated by a strict adversarial review before delivery. Use when the user wants to clear / ship / close out an epic or PRD, implement all children of a parent issue, fan out agents to work issues in parallel, or invokes /clear-epic with an issue URL or number.
---

# Clear Epic

Drive every issue under a parent/epic to a reviewed, green PR by fanning out one
worker sub-agent per issue, in dependency order, behind an adversarial review gate.

## Quick start

`/clear-epic <parent-issue-url-or-#number>` — e.g.
`/clear-epic https://github.com/sum117/masoria-universe/issues/214` or `/clear-epic 214`.

## Arguments

- `$ARGUMENTS` = the parent issue: a full URL (extract `owner/repo/number`) or a
  bare number / `#num` (use the current repo from `git remote`).
- Optional flags:
  - `--tier <tier>` — run the **worker** sub-agents at a specific tier/model
    (e.g. `opus`, `sonnet`, `haiku`) instead of the default (the orchestrator's
    own tier). Does NOT downgrade the adversarial reviewer (step 4).
  - `--serial` — no parallel fan-out; one worker at a time.
  - `--merge` — auto-merge each PR once clean; default: leave merges for a human.

## Workflow

### 1. Resolve the epic
- Read the parent: `gh issue view <num> --comments` (or the issue-tracker MCP).
- Enumerate **directly related** issues = native sub-issues of the parent **plus**
  any OPEN issue whose body declares it (`## Parent #<num>` / `PRD epic: #<num>`).
  Dedupe; skip closed ones.
- Per issue capture: title, ACs, labels (`ready-for-agent` vs `ready-for-human`),
  and **blocked-by** refs.

### 2. Plan the waves
- Build the blocked-by graph and topologically sort. A wave = issues whose
  blockers are all done.
- Flag `ready-for-human` / HITL issues — surface them; do **not** auto-run them.
- Present the plan (issues, waves, what runs in parallel) and get a go-ahead
  before fanning out.

### 3. Fan out (per wave)
- Spawn **one worker per ready issue, in parallel, at max effort** (one Agent
  call per issue, all in a single message), each at the **worker tier** — the
  orchestrator's own tier by default, or whatever `--tier` overrides to (pass it
  as the Agent call's `model`). Fill the **worker prompt** in
  [PROMPTS.md](PROMPTS.md) with that issue's placeholders.
- Every worker MUST build **test-first via the `/tdd` skill** (red → green →
  refactor) — baked into the worker prompt, non-negotiable.
- A worker is NOT done when its PR opens — only after the gate (step 4) is clean.

### 4. Adversarial review gate (before any issue is "delivered")
Enforced for every issue, no exceptions — deliver only when **all three** pass:
- **Automated DoD green**: per-app `tsc --noEmit`, the issue's named tests, and the
  project boundary test (e.g. `@masoria/wire`).
- **Independent adversarial review**: spawn a SEPARATE skeptical reviewer
  sub-agent (a worker may never review its own work) using the **reviewer prompt**
  in [PROMPTS.md](PROMPTS.md). It hunts AC gaps, `CONTEXT.md`/ADR violations,
  scope creep, and regressions, and tries to break the slice.
- **Bot review loop**: `@codex review` → fix → reply + resolve → re-review until
  clean (fall back to `/code-review` at max effort if the bot is unavailable).
- Loop fix → re-review; never wave a red gate through.

### 5. Orchestration loop
- Keep a live status checklist (issue → queued / running / in-review / clean /
  merged / blocked).
- As PRs go clean (and merge, if `--merge`), recompute the ready set and launch
  the next wave. Events don't cover everything — re-poll PR/CI state.
- Stop when every related issue is delivered; report any left blocked, HITL, or
  stuck after repeated re-kicks.

## Notes
- **Worker tier** defaults to the orchestrator's own model (workers are as
  capable as the driver); `--tier` overrides it. The adversarial reviewer is
  never downgraded by `--tier` — keep the gate sharp. "Max effort" = thorough, no
  shortcuts. One issue = one vertical slice = one PR that closes that issue.
- Project-specific commands are placeholders in [PROMPTS.md](PROMPTS.md) — adapt
  per repo; the defaults target `sum117/masoria-universe`.
