# 07 — Architecture proposal

## Identity

- Module Id / folder / assembly / root namespace: **`DiplomacyOverview`**
- Display name: **Diplomacy Overview** — mod-page tagline: *"see every war, alliance and pact at a glance; reads your campaign, never touches it"*.

## Repo layout

```
bannerlord-diplomacy-overview/
├── AGENTS.md / CLAUDE.md / README.md
├── docs/
│   ├── research/            # these docs
│   └── images/              # design reference
├── src/DiplomacyOverview/
│   ├── DiplomacyOverview.csproj      # doc 03 blueprint
│   ├── SubModule.cs                  # MBSubModuleBase: UIExtender registration only
│   ├── _Module/                      # module-root template → copied to game Modules on build
│   │   ├── SubModule.xml
│   │   ├── GUI/Prefabs/DiplomacyOverview/{RelationsPanel.xml, RelationNode.xml, RelationLegend.xml}
│   │   └── ModuleData/Languages/…
│   ├── Data/                         # PURE layer — no TaleWorlds types beyond IDs where possible
│   │   ├── RelationKind.cs           # War, Alliance, TradeAgreement, CallToWar (+ legacy NAP) flags
│   │   ├── RelationEdge.cs           # (NodeId a, NodeId b, RelationKind, payload for tooltips)
│   │   ├── RelationGraph.cs          # nodes + edges + filter application
│   │   └── CircleLayout.cs           # pure math: N nodes → positions, label anchors, edge endpoints
│   ├── Providers/
│   │   ├── IRelationProvider.cs      # IEnumerable<RelationEdge> GetEdges(scope); RelationKind Provides
│   │   ├── WarProvider.cs            # IFaction.FactionsAtWarWith + GetStanceWith payload
│   │   ├── AllianceProvider.cs       # Kingdom.IsAllyWith / AlliedKingdoms / end dates
│   │   └── TradeAgreementProvider.cs # native 1.4.7 behavior (doc 11); self-disables when absent
│   ├── Behaviors/
│   │   └── RelationsDirtyBehavior.cs # CampaignBehaviorBase: event listeners → dirty flag; empty SyncData
│   └── UI/
│       ├── Mixins/KingdomManagementMixin.cs + PrefabExtensions.cs   # doc 04 §A
│       ├── ViewModels/{RelationsVM, RelationNodeVM, RelationEdgeVM, RelationLegendItemVM}
│       └── Widgets/RelationGraphCanvasWidget.cs                     # line rendering (doc 04 §C)
└── .github/workflows/build.yml
```

The `Data/` + `CircleLayout` layer stays free of game singletons → unit-testable with plain xunit
against net472/netstandard (no game install in CI). Providers/UI are thin adapters over it.

## Data flow

```
CampaignEvents (doc 05 table) ──> RelationsDirtyBehavior.dirty = true
Tab opened / SelectRelations ──> if dirty: rebuild
  scope = dropdown (Kingdoms | Clans)
  nodes = Kingdom.All / Clan.All (filtered, doc 05)
  edges = providers.Where(p => toggles[p.Provides]).SelectMany(GetEdges)
  layout = CircleLayout(nodes)            # deterministic order → stable positions
RelationsVM publishes MBBindingList<NodeVM>, MBBindingList<EdgeVM>, legend toggles, SelectorVM scope
```

- Rebuild is on-demand only (never per-tick, never per-frame). A full rebuild at RoT scale
  (~93 clans) is thousands of cheap property reads — microseconds-to-milliseconds; still, wars come
  from `FactionsAtWarWith` (per-node) not O(n²) pair scans; alliances from `AlliedKingdoms`; only
  trade agreements need pair iteration (no public enumerator — doc 11; kingdom count² ≈ 400 max — fine).
- Toggle/dropdown changes only re-filter/re-layout from the cached graph — no re-query.

## VM / widget split

- `RelationNodeVM`: faction name, `BannerImageIdentifierVM`, X/Y (from layout), selection/hover
  state, `ExecuteOpenEncyclopedia` (link via `IFaction.EncyclopediaLink`).
- `RelationEdgeVM`: endpoints (resolved to canvas coords), kind, color, thickness, dashed flag,
  tooltip payload (war stats / expiry dates — doc 05).
- `RelationGraphCanvasWidget`: consumes edge list; draws all edges in `OnRender` (rotated
  `DrawSprite` rects, doc 04 §C technique 1). Nodes are ordinary child widgets positioned by binding
  — they hot-reload; only lines are code-drawn.
- Legend: `MBBindingList<RelationLegendItemVM>` (kind, color swatch, `IsEnabled` toggle) —
  visibility of a kind = provider present AND toggle on; "Trade Agreement" appears only when the
  native behavior exists (presence-gated, doc 11).

## Settings

- v1: in-screen toggles + scope dropdown; remember per session (static). **No save data.**
- v2: MCM soft dependency for defaults (default-enabled kinds, colors, minor-clan filter). MCM's
  `Settings` classes via the template's soft-dep pattern (doc 03); settings persist to
  `Documents\...\Configs\ModSettings\` on their own.

## Milestones

- **M0 — walking skeleton**: empty "Relations" tab appears in vanilla kingdom screen; hot-reload loop
  proven. *(Exit: screenshot.)*
- **M1 — nodes**: kingdoms on a circle with banner medallions + labels.
- **M2 — edges**: war/alliance lines + legend toggles (the mockup, minus dropdown).
- **M3 — scope**: kingdom⇄clan dropdown; clan clustering by kingdom; minor-clan filter.
- **M4 — integrations**: TradeAgreementProvider (orange lines, native 1.4.7 — #15), tooltips (war
  stats, expiry), optional call-to-war kind. (Diplomacy NAP adapter retired — #9.)
- **M5 — compat pass**: RoT installed (+ BannerKings): tab visible, banners render, strip fits,
  perf sane. MCM defaults. Release packaging + CI publish wiring.

## Spikes first (each kills an [UNVERIFIED] from these docs)

| # | Question | Method | Status |
|---|---|---|---|
| S1 | How do module-shipped custom `Widget` classes register with `WidgetFactory`? | Minimal widget in M0 skeleton; if not auto-discovered, GABS/UIExtenderEx registration path | ✔ **Resolved (tracer #5)**: auto-discovery via `WidgetInfo.CollectWidgetTypes` AppDomain scan; public `(UIContext)` ctor required; doc 10 |
| S2 | Does `Widget.Rotation` render child widgets sanely (pivot, hit-testing)? | Rotate a tinted `BlankWhiteSquare_9` child 30°; else fall back to `OnRender` `DrawSprite` (proven path) | ✔ **Resolved (tracer #5 run 5)**: BOTH techniques render correctly in-game (user screenshot); production uses OnRender canvas (doc 10). Also see P-22/P-23 for the two runtime traps found en route |
| S3 | `UIExtender.Create/Register/Enable` exact v2.13.2 API | compile against package; mirror Diplomacy's SubModule.cs | ✔ **Resolved (tracer #5)**: pattern confirmed verbatim against installed DLL; corrections (2-arg attribute, Index counts comments, RefreshValues hook gap) in doc 10 |
| S4 | BLSE `Standalone.exe` forwards `_MODULES_` args? | try once; else use vanilla exe for the debug profile | open |
| S5 | UIExtenderEx `DumpXML` tag semantics | grep UIExtenderEx source | open (note: decompile shows `UIExtenderExSettings.Instance.DumpXML` gates a per-movie dump into the module's `Dumps\` folder) |

## Explicit non-goals (v1)

Writing any diplomacy state; map-overlay rendering; multiplayer; Game
Pass (`net6`/`Gaming.Desktop`) build; per-version DLL loader; custom sprite assets.
