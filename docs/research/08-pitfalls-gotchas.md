# 08 — Pitfalls & gotchas

Numbered so reviews/commits can cite them ("guards against P-07").

**P-01 — Most tutorials pre-date v1.3 and are wrong in ways that compile-fail *or worse, silently mislead*.**
Removed/changed in v1.3 **[LOCAL decompiles]**: `BannerCode` class (use `Banner` instance;
`banner.BannerCode` string prop remains), `ImageIdentifierType` enum + non-abstract
`ImageIdentifierVM` (use `BannerImageIdentifierVM` etc.), `DrawObject2D` (+ its `CreateQuad`) →
`ImageDrawObject`/`Rectangle2D`, `FactionManager.IsAlliedWithFaction`/`DeclareAlliance` (gone;
alliances live in `AllianceCampaignBehavior` / `Kingdom.IsAllyWith`) — community docs at
bannerlordmodding.lt still show the old FactionManager surface. When any doc disagrees with a
decompile of the installed DLL, the decompile wins.

**P-02 — Version strings in SubModule.xml are unvalidated.** Shipping Diplomacy contains
`DependentVersion="vv1.3.4"` (double `v`) and it loads anyway — meaning typos won't be caught for
you either, and comparisons may quietly misbehave. Format: `v1.3.15` / metadata ranges `v1.3.15.*`.

**P-03 — Never ship game/framework DLLs in your module `bin`.** Direct game references need
`Private=False`; package refs need `IncludeAssets="compile"` / `PrivateAssets="All"` (doc 03).
A stray `0Harmony.dll` or `TaleWorlds.*.dll` in a mod folder causes cross-mod type chaos.

**P-04 — net472 on a modern SDK.** May need `Microsoft.NETFramework.ReferenceAssemblies` (P-04
check at first build); `LangVersion latest` is fine but runtime is .NET Framework — no `Span<T>`
fast paths, no `System.Text.Json` by default; stick to what the game ships (`Newtonsoft.Json` is
available game-side).

**P-05 — Don't reference `Bannerlord.ReferenceAssemblies.NavalDLC` casually.** Its published version
can lag the game (`1.3.14.107738` vs game 1.3.15 observed) and we don't use naval APIs. Also pin all
refasm versions exactly — `1.3.*` floats can grab a 1.4.x package (branch exists, doc 01).

**P-06 — Per-version community DLLs mean reflection targets move.** Diplomacy on this machine runs
its `1.3.13` assembly on a `1.3.15` game; assembly *file* names embed versions. Reflection must scan
loaded assemblies by prefix + probe the type (doc 05 recipe), never `Assembly.Load("Bannerlord.Diplomacy")`.

**P-07 — `Campaign.Current` is null outside campaigns** (main menu, custom battle). Guard every
campaign query; construct UI/data only from campaign-scoped entry points (tab open, behavior events).

**P-08 — Event listeners: non-serialized, exception-contained, campaign-scoped.** Use
`AddNonSerializedListener` (serialized listeners = save entanglement); wrap handler bodies in
try/catch (a throwing listener disrupts the event broadcast for everyone); don't hold
`Kingdom`/`Clan` refs across sessions (total conversions destroy/create factions — rebuild from
`Kingdom.All`/`Clan.All` each time).

**P-09 — Empty `SyncData` or it isn't removal-safe.** The save-safety guarantee (doc 06) holds only
while no behavior writes SyncData and no `SaveableTypeDefiner` exists. Enforce in code review.

**P-10 — GauntletLayer input plumbing.** A layer needs input restrictions/focus
(`InputRestrictions.SetInputRestrictions()`, `IsFocusLayer`, register to `ScreenManager` focus) or
clicks fall through to the map. Copy whichever pattern the kingdom screen / coda tutorial uses at M0.
**[WEB + INFERRED — verify at M0]**

**P-11 — Widget XML element names are a global namespace.** `<RelationGraphCanvas>` from two mods =
collision. Prefix: `<DiplomacyOverviewGraphCanvas>`. Same for brush names and prefab file names
(`GUI/Prefabs/DiplomacyOverview/…`, brushes `DiplomacyOverview.*`).

**P-12 — Localization keys.** Every user string as `{=8charKey}Fallback` `TextObject`; module
strings XML under `ModuleData/Languages` (Diplomacy's layout, doc 03). Retrofitting is painful;
translation mods expect keys.

**P-13 — Hot reload only re-reads *edited existing* prefab XML on screen reopen; *new* files need a
restart** (doc 02 §7). Budget for that when adding prefabs.

**P-14 — Prefab-extension insertion `Index` is relative to already-patched XML** — other mods shift
it. Use XPath anchors that don't depend on absolute child positions for correctness; treat index as
cosmetic ordering only (doc 04 §A).

**P-15 — The launcher won't auto-enable your module.** After first deploy, tick it in BLSE
LauncherEx (or the `_MODULES_` arg path, doc 02 §6). Load order persists in
`Documents\...\Configs\LauncherData.xml` **[LOCAL]**.

**P-16 — MCM settings live under `Documents\Mount and Blade II Bannerlord\Configs\ModSettings\`**
(JSON, `Global/<Mod>/...` or `<Mod>/Options.json` patterns **[LOCAL]**) — don't invent a config file
location; if not using MCM, don't persist at all (v1 stance).

**P-17 — The Modding Kit (editor, SpriteSheetGenerator) is a separate Steam tools install** — absent
here. Any "just generate a sprite sheet" advice implies that install + `.tpac` packaging. Our design
avoids custom sprites entirely (doc 04 §F).

**P-18 — Steam Workshop publishing is not file-copying** — it goes through TaleWorlds' publish
tooling / BUTR's `release-steam` workflow with Steam credentials (doc 03 CI). Nexus is a plain zip of
the module folder. Defer both until release.

**P-19 — Don't commit decompiled sources** (TaleWorlds' or other mods') to the repo — legal gray zone
and huge diffs. Distilled API notes (docs 04/05) only. Keep decompiles in a local scratch dir.

**P-20 — `SetCircualMask` is really spelled that way.** v1.3 API typos exist; trust decompiles over
intuition when a name "looks wrong".

**P-21 — Two kingdom-screen extenders in one list (us + BannerKings) crowd the tab strip** — keep the
tab label short, test the combo, keep the standalone-screen fallback alive (docs 04 §B, 06).

**P-22 — War Sails swaps campaign ViewModels for `Naval*` subclasses; exact-type-keyed extensions
silently miss.** With NavalDLC enabled, the kingdom screen constructs
`NavalDLC.ViewModelCollection.Kingdom.NavalKingdomManagementVM : KingdomManagementVM` (verified via
a headless UIExtenderEx-registry dump, doc 10 run 3). UIExtenderEx mixins attach by
`instance.GetType()` **exact match**, so a mixin registered for the base VM never attaches on DLC
installs — with zero errors anywhere (`MessageUtils.Fail` paths are release-silent). Fix:
`[ViewModelMixin(null, true)]` (`handleDerived`) or a dedicated naval patch module — which is
precisely why `Bannerlord.DiplomacyNavalDLCPatch` exists (its whole payload is Diplomacy's mixins
re-targeted at `NavalKingdomManagementVM`). Prefab patches are unaffected (movie names don't
change). Assume the same pattern for any other campaign screen the DLC touches.

**P-23 — Two `Vector2` types exist at runtime; bind to the game's, or crash at JIT.** The game's
UI APIs (`Widget.Size`, `Rectangle2D` fields, draw contexts) use `System.Numerics.Vector2` from
**`System.Numerics.Vectors, Version=4.1.3.0`** (the DLL the game ships in `bin`). The .NET
Framework targeting pack's `System.Numerics, 4.0.0.0` facade defines a structurally identical but
**referentially different** `Vector2`, and the SDK references that facade implicitly — so
`using System.Numerics;` silently compiles your code against the wrong identity and every
cross-assembly signature containing it throws `MissingMethodException` at JIT (observed:
`Widget.get_Size`). The `System.Numerics.Vectors` NuGet is a minefield of forwarders: 4.5.0 = assembly 4.1.4.0
(wrong version); 4.4.0 = 4.1.3.0 (right version) **but both its `ref/net46` AND `lib/net46`
assemblies are type-forwarding facades on .NET Framework** (CS1069 back to the framework facade);
`Aliases` on `PackageReference` doesn't flow on net472; `<Reference Remove="System.Numerics"/>`
can't beat the SDK's implicit reference (CS0433 at best); and when a HintPath fails to resolve,
MSBuild **silently** falls back to the targeting pack's forwarding facade. Working recipe
(verified): a download-only `PackageReference` (`Version="4.4.0" ExcludeAssets="all"`) + an
explicit `<Reference Include="System.Numerics.Vectors">` with `<Aliases>game</Aliases>`,
`<Private>False</Private>`, and two conditional HintPaths — the **game's own DLL** when
`$(GameFolder)` exists (runtime truth), else the package's **`lib/netstandard2.0`** build (cannot
forward to framework DLLs ⇒ real types, same 4.1.3.0 identity) for game-less CI builds. Consuming
files: `extern alias game;` + `using Vector2 = game::System.Numerics.Vector2;`. Always verify by
reading your DLL's AssemblyRef table (must show `System.Numerics.Vectors 4.1.3.0` and no
`System.Numerics`).
Corollary of the general rule: for any type appearing in game API *signatures*, your compiled
identity must match the game's loaded identity exactly — decompiler output hides this (both sides
print "Vector2"). Related mitigation: keep risky tokens out of `OnRender` itself (JIT-time throws
in a virtual the engine calls are uncatchable by you); put them in a child method wrapped in a
self-disabling try/catch (AGENTS.md rule 6).

**P-24 — `Kingdom.All` is not "the active kingdoms"; on modded lists it contains zombie kingdoms.**
`Kingdom.All => Campaign.Current.Kingdoms => CampaignObjectManager.Kingdoms` **[LOCAL decompile]**
holds every Kingdom object ever added, and only a proper `DestroyKingdomAction` sets
`IsEliminated`. Mods add kingdoms freely: Separatism creates a first-class Kingdom per seceding
clan and (with `KeepEmptyKingdoms`) intentionally keeps the clanless husk alive forever — a long
campaign showed ~70 "kingdoms" where ~10 were real (doc 10 run 6). Filtering on `!IsEliminated`
alone is wrong on exactly the mod lists we target. Filter for *participation*: at least one
non-eliminated clan with a living leader (`KingdomFilter.IsParticipant`), and derive edge
endpoints through the same predicate so stale zombie stances drop with their nodes. Corollary:
never size UI to vanilla counts (8-ish kingdoms) — assume mods multiply campaign objects.
