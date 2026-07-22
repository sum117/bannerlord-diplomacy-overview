# Research runbooks — Diplomacy Overview mod

Research pass for bootstrapping **Diplomacy Overview**, a Mount & Blade II: Bannerlord mod that adds a
"Relations" view: kingdoms/clans as banner nodes on a circle, colored lines for their relations
(war / alliance / pacts), per-relation toggles, and a kingdom⇄clan scope dropdown.
See [09-design-reference-mapping.md](09-design-reference-mapping.md) for the client mockup breakdown.

Compiled **2026-07-01** against v1.3.15. **Re-verified 2026-07-22 against v1.4.7.117484** after the
Steam install updated — the deltas, the executed pin bump, and the new native Trade Agreement API
live in [11-game-1.4.7-migration.md](11-game-1.4.7-migration.md). Version-tagged claims in docs
01–10 describe the 1.3.15 state they were researched against unless doc 11 says otherwise.

## Target baseline (verified on the dev machine)

| Component | Version | Notes |
|---|---|---|
| Game (Steam) | **v1.4.7** (changeset `117484`) | `bin\Win64_Shipping_Client` only → .NET Framework client |
| War Sails DLC | NavalDLC **v1.2.7** (`RequiredBaseVersion v1.4.7`) | now `DefaultModule=true` |
| Bannerlord.Harmony | v2.4.2.248 | installed |
| Bannerlord.ButterLib | v2.11.1 | installed |
| Bannerlord.UIExtenderEx | v2.13.3 | installed |
| MCM (Bannerlord.MBOptionScreen) | v5.12.2 | installed |
| Bannerlord.Diplomacy | **not installed** (was v1.3.3 pre-reformat) | NAP adapter (#9) has no local test target |
| BLSE | installed (`Bannerlord.BLSE.*.exe` in game bin) | active launcher |
| Realm of Thrones | **not installed**; v8.0 (Apr 2026) targeted the 1.3.x branch — 1.4.x support unverified | compat target |

## The documents

| Doc | What it answers |
|---|---|
| [01-ecosystem-and-versions.md](01-ecosystem-and-versions.md) | Game/DLC/branch state, BUTR stack roles, what v1.3 changed for modders |
| [02-environment-setup.md](02-environment-setup.md) | Runbook: SDK → template → build → deploy → run/debug → UI hot-reload → decompile toolkit |
| [03-module-anatomy-and-build.md](03-module-anatomy-and-build.md) | SubModule.xml anatomy, dependency metadata, csproj blueprint, CI/CD |
| [04-gauntlet-ui-playbook.md](04-gauntlet-ui-playbook.md) | Kingdom-screen tab injection, line drawing, banners, dropdowns, toggles, hot reload |
| [05-diplomacy-data-access.md](05-diplomacy-data-access.md) | Exact APIs for wars/alliances/NAPs, refresh events, native trade agreements (1.4.7 addendum) |
| [06-compatibility-and-stability.md](06-compatibility-and-stability.md) | Soft deps, load order, save safety, Realm of Thrones / BannerKings coexistence |
| [07-architecture-proposal.md](07-architecture-proposal.md) | Proposed module layout, provider pattern, VM/widget design, perf budget, milestones & spikes |
| [08-pitfalls-gotchas.md](08-pitfalls-gotchas.md) | Numbered trap list (stale tutorials, removed APIs, packaging mistakes…) |
| [09-design-reference-mapping.md](09-design-reference-mapping.md) | Mockup → widget mapping, client-request traceability, v1 scope recommendation |
| [10-tracer-findings.md](10-tracer-findings.md) | Issue #5 tracer verdicts: S1/S3 resolved (decompile evidence), S2 manual test script, corrections to docs 04/07 |
| [11-game-1.4.7-migration.md](11-game-1.4.7-migration.md) | 1.3.15 → 1.4.7 delta: executed pin bump, seam re-verification, native Trade Agreement API |

## Provenance & confidence legend

Facts are tagged throughout:

- **[LOCAL]** — read or decompiled from the installed game / installed mods on this machine, at the
  version stated in context (docs 01–10: v1.3.15; doc 11: v1.4.7). Highest confidence; version-exact.
  Decompiled sources themselves are *not* committed to this repo.
- **[WEB]** — primary web source (linked). Confidence stated inline when sources disagree.
- **[UNVERIFIED]** — plausible but not confirmed; each has a matching spike task in doc 07.

## How this research was produced

1. Inspected the local install: module manifests, launcher data, MCM configs, vanilla Gauntlet
   prefab/brush XMLs (`Modules\SandBox\GUI\...`).
2. Decompiled (ilspycmd) the exact installed assemblies: `TaleWorlds.CampaignSystem`,
   `TaleWorlds.GauntletUI`, `TaleWorlds.TwoDimension`, VM collections, and
   `Bannerlord.Diplomacy.1.3.13.dll` — all API claims come from these, not from memory or tutorials.
3. Studied exemplar open-source mods: [DiplomacyTeam/Bannerlord.Diplomacy](https://github.com/DiplomacyTeam/Bannerlord.Diplomacy),
   [R-Vaccari/bannerlord-banner-kings](https://github.com/R-Vaccari/bannerlord-banner-kings) (kingdom-tab injection),
   BUTR's [Module.Template](https://github.com/BUTR/Bannerlord.Module.Template),
   [BuildResources](https://github.com/BUTR/Bannerlord.BuildResources),
   [ReferenceAssemblies](https://github.com/BUTR/Bannerlord.ReferenceAssemblies) (repos fetched and read).
4. Web research on Realm of Thrones, BLSE, CI, sprites/hot-reload, AGENTS.md standard (sources cited inline).
