# Prompts

Two reusable templates. Fill every `{{PLACEHOLDER}}` before spawning a sub-agent.
Defaults target `sum117/masoria-universe`; change them per project.

## Placeholders
- `{{ISSUE}}` — issue number (e.g. `220`)
- `{{REPO}}` — `owner/repo` (e.g. `sum117/masoria-universe`)
- `{{PARENT}}` — parent/epic issue number (e.g. `214`)
- `{{BRANCH}}` — branch name (e.g. `claude/issue-{{ISSUE}}-<slug>`)
- `{{TYPECHECK}}` — typecheck command (default: per-app `tsc --noEmit`)
- `{{TESTS}}` — the issue's named tests (default: `bun --filter @masoria/<app> test`)
- `{{BOUNDARY_TEST}}` — cross-boundary test (default: the `@masoria/wire` boundary test)
- `{{REVIEW_BOT}}` — review trigger (default: `@codex review`; fallback `/code-review` at max effort)

---

## Worker prompt — one per issue

> Implement GitHub issue #{{ISSUE}} ({{REPO}}) end-to-end.
>
> - **Read it first**: `gh issue view {{ISSUE}} --comments`, plus parent
>   #{{PARENT}} and the seam / PRD it slices. Read the linked acceptance criteria.
> - **Respect the domain**: honor `CONTEXT.md` and any `docs/adr/` touching the
>   area; use the glossary's exact terms; do not re-open one-way-door decisions.
> - **Build it test-first (required)** — invoke the `/tdd` skill and work
>   red → green → refactor: write a failing test for each acceptance criterion
>   first, then the code that makes it pass. Not optional; the review gate checks
>   for it.
> - **Deliver the full vertical slice** — satisfy EVERY acceptance-criteria
>   checkbox; tick each in the issue as you complete it.
> - **Definition of done = the issue's ACs**, plus: `{{TYPECHECK}}` green,
>   `{{TESTS}}` green, and `{{BOUNDARY_TEST}}` passing. Test at the highest seam;
>   prefer pure functions with injected deps.
> - **Branch → commit → PR**: branch `{{BRANCH}}`; small, focused commits; open a
>   PR whose body closes #{{ISSUE}}.
> - **Drive the review loop**: run `{{REVIEW_BOT}}` → fix → reply + resolve
>   threads → re-review until clean. Do not declare done until the PR is green AND
>   review is clean.
> - **Report back**: PR URL, AC checklist state, typecheck/test results, and any
>   deviation (with the reason).

---

## Adversarial reviewer prompt — independent gate (never the agent that built it)

> You are a STRICT, SKEPTICAL, ADVERSARIAL reviewer. Assume the PR for issue
> #{{ISSUE}} ({{REPO}}) is wrong until proven otherwise. Your job is to BLOCK
> delivery unless it genuinely satisfies the issue.
>
> - Pull the diff and the issue: `gh pr view <pr> --comments`,
>   `gh issue view {{ISSUE}} --comments`.
> - **AC audit**: for each acceptance-criteria checkbox, find the concrete code +
>   test that satisfies it. A checked box with no evidence is a defect.
> - **Spec / domain**: flag any violation of `CONTEXT.md` terms or `docs/adr/`
>   decisions; flag scope creep beyond this slice.
> - **Try to break it**: edge cases; error / empty / loading states; the
>   for-granted + mobile gate (if user-facing); the boundary contract;
>   retroactive / back-compat behavior.
> - **Tests**: built test-first (every AC has a test)? right seam? behavior not
>   implementation? would they catch a regression? Run them.
> - **Verdict**: output `BLOCK` with a numbered, actionable defect list, or `PASS`
>   only if you genuinely could not find a defect. Default to BLOCK.
