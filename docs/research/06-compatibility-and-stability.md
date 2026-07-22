# 06 — Compatibility & stability practices

Design goal: a mod that drops into a 60+ mod list (the dev machine's own list is the test bed
**[LOCAL]**) without patch conflicts, save corruption, or version panic.

## Dependency posture

| Dependency | Kind | Rationale |
|---|---|---|
| Bannerlord.Harmony | **hard** | UIExtenderEx requires it; ecosystem baseline |
| Bannerlord.UIExtenderEx | **hard** | our injection mechanism |
| Native/SandBoxCore/Sandbox/StoryMode | hard (game) | standard |
| MCM (Bannerlord.MBOptionScreen) | **optional** (`DependedModuleMetadata optional="true"`) | settings work without it (in-screen toggles) |
| Bannerlord.Diplomacy | **optional**, `LoadBeforeThis` | NAP lines only when present; reflection adapter degrades gracefully (doc 05) |
| Bannerlord.ButterLib | none for v1 | add only when we adopt a feature (logging/hotkeys) |
| NavalDLC | optional metadata only | no code dependency; metadata keeps sort order sane on DLC installs |

Declare hard deps in **both** `DependedModules` and `DependedModuleMetadatas`; optionals only in
metadata (BLSE/LauncherEx enforce + sort; vanilla launcher at least sees the hard set). **[LOCAL exemplars]**

## Harmony etiquette (aspiration: zero own patches)

- UIExtenderEx performs the vanilla-VM/prefab patching for us; we ship **no Harmony patches** in v1.
  Every patch we don't write is a conflict class we don't join.
- If one ever becomes necessary: postfix > prefix; never `return false` (skip-original) unless
  replacing behavior wholesale; unique harmony id (`"DiplomacyOverview"`); be aware the
  first-loaded Harmony assembly serves everyone. **[WEB: [bannerlordmodding.lt/modding/harmony](https://docs.bannerlordmodding.lt/modding/harmony/)]**

## Save safety (headline feature — state it on the mod page)

Bannerlord saves only contain mod data when a mod registers saveable types
(`SaveableTypeDefiner`) or writes in `CampaignBehaviorBase.SyncData`. We do neither: behaviors keep
`SyncData` empty and register only `AddNonSerializedListener`. ⇒ **Install & remove mid-campaign is
safe** — no "save corrupted by missing module" reports. Enforce in review: no
`SaveableTypeDefiner`, no `dataStore.SyncData` writes, no mutations of campaign state from UI code.
**[WEB: [save system docs](https://docs.bannerlordmodding.com/_csharp-api/savesystem/) + LOCAL design]**

## Version resilience

- Pin refassemblies exactly (currently `1.4.7.117484`; SubModule metadata `version="v1.4.7.*"`). On
  game patches: diff decompiles of the touched types (doc 02 §8) before bumping — the 1.3.15→1.4.7
  bump (2026-07-22, doc 11) compiled clean with all injection anchors intact.
- The community handles multi-version support with per-version DLLs + `Bannerlord.ModuleLoader`
  (Diplomacy ships 10 of them **[LOCAL]**) — documented in doc 03, deliberately deferred.
- Observed reality: Diplomacy's newest DLL targets 1.3.13 and runs fine on 1.3.15 — minor-version
  drift usually tolerable; **never** assume it across 1.3 → 1.4.
- 1.4.x landed: this install runs 1.4.7 since 2026-07 (doc 11); the cross-minor bump is done.

## Realm of Thrones coexistence (client requirement)

Facts (doc 01): v8.0, same 1.3.x/War Sails branch, 4 modules, standard BUTR stack + metadata,
UI = sprite/brush **reskin** (not screen-logic replacement), ~20 kingdoms / ~93 clans. **[WEB]**
(2026-07-22: the game moved to 1.4.7 — before running M5, check whether RoT shipped a 1.4.x
release; v8.0 targeted 1.3.x.)

Consequences & verification checklist (milestone M5):

1. Load order: RoT's modules `LoadBeforeThis` us? — RoT is a content overhaul; our metadata already
   sorts us after officials + optional deps. Add nothing RoT-specific unless testing shows a need;
   if it does, `<DependedModuleMetadata id="<RoT module id>" order="LoadBeforeThis" optional="true"/>`.
2. Verify in-game: Relations tab renders over RoT's reskinned kingdom screen; banner medallions
   render RoT's custom banners (`IFaction.Banner` — guard nulls defensively); tab strip still fits
   (RoT probably doesn't add tabs; BannerKings does — see below).
3. Perf: clan scope with ~93 clans ⇒ ~4.3k pairs; our lazy rebuild + `FactionsAtWarWith`
   enumeration (no full pair scan for wars) keeps this trivial (budget in doc 07).
4. Total conversions can eliminate/replace vanilla kingdoms mid-campaign — never cache `Kingdom`
   refs across sessions; rebuild node sets from `Kingdom.All` each refresh.

## BannerKings coexistence (same injection point!)

BannerKings adds four kingdom-screen tabs with the very pattern we use (doc 04) — the two mods are
*mechanically* compatible (independent mixins/insertions), but the tab strip gets crowded:
5 vanilla + 4 BK + ours = 10 buttons. BK already widens the strip; our button inherits that. Action:
keep our label short ("Relations"), test with BK installed, and keep the standalone-screen fallback
(doc 04 §B) as the pressure valve. RoT + BK together is the worst case — test it once. **[LOCAL BK sources + INFERRED]**

## Big-modlist etiquette

- **No work in module-load paths** (`OnSubModuleLoad` = register UIExtender + hotkeys only). Campaign
  queries only when `Campaign.Current != null`; UI construction only when the screen opens.
- **Exception-contain every event handler and every reflection call** — a throwing listener can take
  down unrelated mods' processing; our failure mode must be "lines missing", never a crash. On error:
  log once, disable the feature for the session.
- Read-only discipline: we never call `DeclareWar/SetNeutral/StartAlliance/...` — display only.
- Crash reports: BLSE/ButterLib attribute crashes to modules via stacktrace — keeping our frames out
  of patched vanilla paths (no Harmony) keeps us out of other people's crash reports.
- Don't override native prefabs by shipping same-named XML (that's total-conversion behavior);
  only additive `PrefabExtension` patches.
- Localize everything (`{=key}`) — RoT/Diplomacy users span many languages; hardcoded English breaks
  translation mods' expectations.

## The "compat patch module" convention

For deep integrations the ecosystem ships separate patch modules
(`Bannerlord.DiplomacyNavalDLCPatch` **[LOCAL]**). If Diplomacy-Overview-× interop ever needs
more than the reflection adapter (e.g., RoT-specific theming), ship `DiplomacyOverview.RoTPatch`
rather than if-else-ing the core module.
