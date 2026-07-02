# AGENTS.md — Diplomacy Overview (Mount & Blade II: Bannerlord mod)

C# mod adding a "Relations" tab to the Kingdom screen: kingdoms/clans as banner-medallion nodes on a
circle, colored lines for their relations (War / Alliance / Non-Aggression Pact), clickable legend
toggles per relation type, and a kingdom⇄clan scope dropdown. **Read-only mod**: it reads campaign
state, never mutates it, and writes nothing into save files.

## Repo state

Bootstrap phase — research docs only, **no code yet**. Before implementing anything, read
`docs/research/README.md` (index + provenance legend). Follow the architecture in
`docs/research/07-architecture-proposal.md`; burn down its spike list (S1–S5) before building on
unverified assumptions. The trap list is `docs/research/08-pitfalls-gotchas.md` — cite entries as
`P-xx` in commits/reviews.

## Environment ground truth (dev machine)

- Game: Steam, `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`,
  **v1.3.15 + War Sails DLC** (NavalDLC v1.1.3). BLSE launcher installed.
- Builds expect env var `BANNERLORD_GAME_DIR` = the path above.
- Installed framework mods (our compat baseline): Harmony 2.4.2.225, ButterLib 2.10.4,
  UIExtenderEx 2.13.2, MCM 5.11.4, Diplomacy mod 1.3.3.
- .NET SDK 10 is installed; the mod targets **net472 / x64** (see `docs/research/03` for the csproj).

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
4. Pin game/package versions exactly (`1.3.15.110062`); no floating wildcards across minors (P-05).
5. Every user-facing string is a `TextObject` with a `{=key}` localization id (P-12).
6. Exception-contain all campaign event handlers and all reflection into the Diplomacy mod — our
   worst failure mode is "lines missing", never a crash (P-08, doc 05 adapter).
7. Guard `Campaign.Current != null` on every campaign query (P-07).

## Conventions

- Root namespace `DiplomacyOverview`; XML-visible names (widgets, brushes, prefab files) prefixed
  `DiplomacyOverview` — those namespaces are global across all installed mods (P-11).
- Commits: imperative subject ≤72 chars, body explains *why*; reference `P-xx`/`S-x`/milestone ids
  (`M0`–`M5`) where they apply.
- Compatibility posture: additive-only UI injection (UIExtenderEx), zero Harmony patches of our own,
  optional (soft) dependencies for MCM and the Diplomacy mod — see `docs/research/06`.
