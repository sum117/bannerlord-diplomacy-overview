# 01 — Ecosystem & versions (state: mid-2026)

## Game branches

- **Stable = v1.3.x ("War Sails" era).** This machine runs **v1.3.15** with the War Sails DLC
  (module `NavalDLC` v1.1.3, `ModuleType=OfficialOptional`, `RequiredBaseVersion v1.3.15`). **[LOCAL]**
- **A 1.4.x branch exists** by mid-2026: `Bannerlord.ReferenceAssemblies.Core 1.4.5.114927` is
  published on NuGet and a "Vanilla 1.4.5 Plus Modding Notes" page exists
  ([Nexus mods/11354](https://www.nexusmods.com/mountandblade2bannerlord/mods/11354)). **[WEB]**
  - **UPDATE 2026-07-22: 1.4.x is now the live branch — this Steam install runs v1.4.7.117484**, and
    all pins were bumped to match (doc 11). The rest of this doc describes the 1.3.15 state it was
    researched against; the pin-exactly rule stands, now at `1.4.7.117484`. **[LOCAL]**
- Runtime: only `bin\Win64_Shipping_Client` exists here → the classic **.NET Framework** client.
  Mods target **net472**. `net6` targets exist only for the Xbox/Microsoft Store build
  (`Gaming.Desktop.x64_Shipping_Client`), which the BUTR template supports via multi-targeting —
  irrelevant for us unless we later publish for Game Pass. **[LOCAL + template sources]**

## What v1.3 changed that matters to this mod

Verified against decompiled v1.3.15 assemblies **[LOCAL]**:

1. **Vanilla now has native alliances.** `TaleWorlds.CampaignSystem.CampaignBehaviors.AllianceCampaignBehavior`
   (+ public interface `IAllianceCampaignBehavior`), `Kingdom.IsAllyWith(Kingdom)`,
   `Kingdom.AlliedKingdoms`, `DefaultAllianceModel` (84-day duration, max 2 alliances), and
   campaign events `AllianceStartedEvent` / `AllianceEndedEvent` / `CallToWarAgreementStarted/EndedEvent`.
   → The mockup's green "Alliance" lines work on **pure vanilla**, no Diplomacy mod required.
2. **The Diplomacy mod now defers alliances to vanilla.** Its `AgreementType` enum contains only
   `NonAggressionPact`; its alliance checks call vanilla `Kingdom.IsAllyWith`. Diplomacy's own value
   for us = NAPs (and its UI features). See doc 05.
3. **API removals that invalidate most older tutorials** (details & replacements in doc 08):
   `BannerCode` class, `ImageIdentifierType` enum, non-abstract `ImageIdentifierVM`,
   `DrawObject2D`, `FactionManager.IsAlliedWithFaction/DeclareAlliance`.
4. War Sails traces in core types: `StanceLink` gained `ShipCasualties1/2`;
   `IFaction.HasNavalNavigationCapability`. Compiling against Core/SandBox reference assemblies does
   **not** require referencing NavalDLC assemblies.

## The BUTR stack (all pre-installed here)

| Module | Role for us |
|---|---|
| **Bannerlord.Harmony** (v2.4.2.225) | Ships 0Harmony + MonoMod as a module every mod depends on. We declare it a hard dependency but aim to write **zero patches of our own** (UIExtenderEx patches on our behalf). |
| **Bannerlord.UIExtenderEx** (v2.13.2) | The injection framework: `[PrefabExtension]` (XPath-targeted XML patches into vanilla prefabs) + `[ViewModelMixin]` (adds `[DataSourceProperty]`/`[DataSourceMethod]` members to vanilla VMs). This is how the Relations tab gets into the Kingdom screen. [Repo](https://github.com/BUTR/Bannerlord.UIExtenderEx) |
| **Bannerlord.ButterLib** (v2.10.4) | Utility layer: crash-report generation, logging (Serilog), hotkeys, sub-systems. Optional for v1 — take the dependency only when we use a feature. |
| **MCM / Bannerlord.MBOptionScreen** (v5.11.4) | Settings UI. Optional/soft dependency pattern is well established (see docs 03/06). Settings persist under `Documents\Mount and Blade II Bannerlord\Configs\ModSettings\`. **[LOCAL]** |
| **BLSE** (installed; [repo](https://github.com/BUTR/Bannerlord.BLSE), ~v1.6.7 May 2026) | Launcher/loader: assembly resolver, exception interceptor, loading interceptor (`BLSEInterceptorAttribute`), and enforcement of the `DependedModuleMetadatas` community metadata. Three entry points: `Bannerlord.BLSE.Launcher.exe` (vanilla-style), `LauncherEx.exe` (extended UI), `Standalone.exe` (CLI). **[LOCAL + WEB]** |

## The Diplomacy mod (soft-integration target)

- Installed v1.3.3; source: [DiplomacyTeam/Bannerlord.Diplomacy](https://github.com/DiplomacyTeam/Bannerlord.Diplomacy). **[LOCAL/WEB]**
- Ships **per-game-version implementation DLLs** (`Bannerlord.Diplomacy.1.3.4.dll` … `.1.3.13.dll`)
  selected at runtime by BUTR's `Bannerlord.ModuleLoader` (`LoaderFilter` tag in its SubModule.xml).
  Note: newest is **1.3.13 running on a 1.3.15 game** — works because the loader picks the best
  compatible build. Consequence for us: never assume the Diplomacy assembly's exact name/version
  when reflecting into it (doc 05 has the safe recipe). **[LOCAL]**
- Also installed: `Bannerlord.DiplomacyNavalDLCPatch` — the "compat patch as separate module"
  pattern, worth knowing as a convention (doc 06).

## Realm of Thrones (client's favorite total conversion)

- Current: **v8.0 "Warsails"** (updated 2026-04-05), on the **1.3.x + War Sails** branch — same
  baseline as us. Two captures disagree on the exact build (1.3.13 vs 1.3.15) — irrelevant at our
  level, same branch. A parallel v7.1 branch exists without the DLC requirement. **[WEB:
  [Nexus 2907](https://www.nexusmods.com/mountandblade2bannerlord/mods/2907),
  [ModDB](https://www.moddb.com/mods/realm-of-thrones)]**
- Ships as **4 modules**, requires the same BUTR stack (Harmony/ButterLib/UIExtenderEx/MCM) as
  separate installs — it does not bundle private copies. Uses standard `DependedModuleMetadatas`. **[WEB]**
- Scale: **~20 kingdoms, ~93+ clans** → our perf envelope (doc 07). **[WEB]**
- UI: "reimagined UI and lots of new sprites" (v6.0+) — a **sprite/brush reskin**, layered via
  UIExtenderEx, not a replacement of kingdom-screen logic → an injected tab should still appear.
  **[WEB, INFERRED — in-game verification is milestone M5, doc 07]**

## Reference documentation map

| Resource | Use for |
|---|---|
| [moddocs.bannerlord.com](https://moddocs.bannerlord.com) | Official modding docs (sprite sheets, asset pipeline) |
| [apidoc.bannerlord.com](https://apidoc.bannerlord.com) | **Per-version** generated API reference (has v1.3.4; good for diffing API breaks) |
| [docs.bannerlordmodding.com](https://docs.bannerlordmodding.com) | Community docs (SubModule schema, basic C# mod, save system) — some pages pre-date 1.3, cross-check |
| [docs.bannerlordmodding.lt](https://docs.bannerlordmodding.lt) | Community docs (UIExtenderEx, Harmony etiquette, kingdoms) — same caveat |
| [BUTR/Bannerlord.GABS gauntlet-ui.md](https://github.com/BUTR/Bannerlord.GABS/blob/master/docs/gauntlet-ui.md) | Best deep reference of the Gauntlet stack + widget-tree/VM spelunking |
| [Bannerlord Wiki Gauntlet tutorial (coda.io)](https://coda.io/@samuel/bannerlord-wiki/gauntlet-ui-tutorial-26) | End-to-end standalone screen + map-bar button walkthrough |
| Local install (`Modules\*\GUI\**`) | The ultimate prefab/brush reference — grep it before any web search |
