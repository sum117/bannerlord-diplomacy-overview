# AGENTS.md — Diplomacy Overview (Mount & Blade II: Bannerlord mod)

C# mod adding a "Relations" tab to the Kingdom screen: kingdoms/clans as banner-medallion nodes on a
circle, colored lines for their relations (War / Alliance / Trade Agreement), clickable legend
toggles per relation type, and a kingdom⇄clan scope dropdown. **Read-only mod**: it reads campaign
state, never mutates it, and writes nothing into save files.

## Repo state

Implementation phase — `src/` holds the module (main + pure `Core` + xunit tests). Spikes S1–S3 are
resolved (doc 10); S4/S5 remain open. Current slice: issue #6 (kingdom war web). Before implementing
anything, read `docs/research/README.md` (index + provenance legend) and the game migration records
in `docs/research/11-game-1.4.7-migration.md` and `docs/research/12-game-1.4.8-update.md`. Follow the architecture in
`docs/research/07-architecture-proposal.md`. The trap list is `docs/research/08-pitfalls-gotchas.md`
— cite entries as `P-xx` in commits/reviews.

## Environment ground truth (dev machine)

- Game: Steam on **Linux (Proton)**,
  `~/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord`,
  **v1.4.8 (changeset 119303) + War Sails DLC** (NavalDLC v1.2.8). No BLSE.
  (Moved from the Windows machine + game updated 2026-10 — delta record in `docs/research/12`.)
- Builds deploy only when env var `BANNERLORD_GAME_DIR` = the path above; it is **not** set
  globally here — pass it per command (`BANNERLORD_GAME_DIR="..." dotnet build ...`).
- Compat baseline for framework mods: Harmony 2.4.2.248, ButterLib 2.11.1, UIExtenderEx 2.13.3,
  MCM 5.12.2. **None are installed in this game copy yet** — Harmony + UIExtenderEx must be added to
  `Modules/` before the mod can load.
- .NET SDK 10 comes from mise (`mise.toml` pins it; the system `dotnet` is runtime-only, so run
  `mise exec -- dotnet ...` outside an activated shell). The test project targets net10.0; the mod
  targets **net472 / x64** (see `docs/research/03` for the csproj).

## Build / run / iterate (once `src/` exists)

- Build: `dotnet build src/DiplomacyOverview -c Debug` — `Bannerlord.BuildResources` auto-deploys the
  packed module to `<game>\Modules\DiplomacyOverview`.
- Run: BLSE LauncherEx (enable the module once), or launch directly with a forced module list —
  exact command in `docs/research/02-environment-setup.md` §6.
- UI iteration: prefab XML hot-reloads on screen reopen, but the game reads the **deployed** copy
  under `<game>\Modules\...\GUI\Prefabs\` — for fast loops edit the deployed XML, then copy changes
  back into `src/DiplomacyOverview/_Module/GUI/Prefabs/` (the source of truth) before committing.
- Tests: keep `Data/` (graph, layout math) free of game types → plain xunit. There is no game
  integration harness; UI/behavior verification = launch the game against the milestone checklists
  in `docs/research/07`.
- API ground truth: decompile the installed DLLs with `ilspycmd` — web tutorials are largely
  pre-1.3 and wrong (P-01). `apidoc.bannerlord.com` has per-version references.

## Hard rules

1. Never mutate campaign state; every `CampaignBehaviorBase.SyncData` stays empty and no
   `SaveableTypeDefiner` exists (this is the mod's save-safety guarantee, P-09).
2. Never ship `TaleWorlds.*`, `0Harmony`, or other mods' DLLs in the module output — game refs are
   `Private=False` / `IncludeAssets="compile"` (P-03).
3. Never commit decompiled sources (P-19); commit distilled notes into `docs/` instead.
4. Pin game/package versions exactly (`1.4.8.119303`); no floating wildcards across minors (P-05).
5. Every user-facing string is a `TextObject` with a `{=key}` localization id (P-12).
6. Exception-contain all campaign event handlers and every optional-integration lookup (e.g. the
   trade-agreement behavior may be absent in old saves/TC mods) — our worst failure mode is "lines
   missing", never a crash (P-08).
7. Guard `Campaign.Current != null` on every campaign query (P-07).

## Releasing

Bump `<Version>` in the csproj **and** `SubModule.xml`, commit, then push a matching `vX.Y.Z` tag.
`.github/workflows/release.yml` verifies the three agree, publishes the GitHub release zip, and
uploads the same zip to Nexus Mods as a new version of the main file (needs the
`NEXUSMODS_API_KEY` secret and `NEXUSMODS_FILE_ID` variable; skipped with a warning without them).

## Conventions

- Root namespace `DiplomacyOverview`; XML-visible names (widgets, brushes, prefab files) prefixed
  `DiplomacyOverview` — those namespaces are global across all installed mods (P-11).
- Translations: `_Module/ModuleData/Languages/` holds an English template plus one folder per game
  language (BR, CNs, CNt, DE, FR, IT, JP, KO, PL, RU, SP, TR). Adding or changing a `{=key}` string
  means updating **every** `DiplomacyOverview_strings.xml`; `LocalizationTests` fails otherwise.
  Reuse the game's official term where one exists (look the English text's id up in the game's
  own `ModuleData/Languages`), and phrase around faction-name variables so they stay in base form.
- Commits: imperative subject ≤72 chars, body explains *why*; reference `P-xx`/`S-x`/milestone ids
  (`M0`–`M5`) where they apply.
- Compatibility posture: additive-only UI injection (UIExtenderEx), zero Harmony patches of our own,
  optional (soft) dependency for MCM — see `docs/research/06`. (The Diplomacy-mod NAP adapter was
  retired 2026-07-22: trade agreements are native — #9/#15.)
