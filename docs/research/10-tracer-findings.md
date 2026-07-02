# 10 — Tracer findings (issue #5, sacrificial)

Status of each research spike after building the tracer. Everything marked **[LOCAL]** is grounded
in decompiles of the DLLs installed on this machine (game v1.3.15.110062, UIExtenderEx 2.13.2
module DLL) — decompiled with ilspycmd into a local scratch dir, never committed (P-19).
The tracer code itself lives in `src/DiplomacyOverview/UI/` + `SubModule.cs` +
`_Module/GUI/Prefabs/DiplomacyOverview/DiplomacyOverviewRelationsPanel.xml`; it is sacrificial
and may be reworked or deleted — these findings are the deliverable.

## S3 — UIExtenderEx registration API: **RESOLVED (docs 04 §A pattern confirmed verbatim)**

**[LOCAL: decompiled `Bannerlord.UIExtenderEx.UIExtender`, `UIExtenderRuntime`]**

- `UIExtender.Create(string moduleName)` (static factory; the public constructor is `[Obsolete]`),
  then `.Register(Assembly)`, then `.Enable()`. `Register(Assembly)` scans the assembly for types
  carrying attributes derived from `BaseUIExtenderAttribute` (`[ViewModelMixin]`,
  `[PrefabExtension]`) and hands them to `UIExtenderRuntime.Register(types)`.
- Lifecycle hook: `MBSubModuleBase.OnSubModuleLoad` — confirmed by decompiling the *shipping*
  Diplomacy mod's `SubModule.OnSubModuleLoad` (installed `Bannerlord.Diplomacy.1.3.10.dll`), which
  does exactly `UIExtender.Create(Name); val.Register(typeof(SubModule).Assembly); val.Enable();`.
  Harmony patching of the prefab/VM pipeline happens in `UIExtender`'s static constructor, and
  patches apply lazily at movie-load time, so any pre-first-screen hook works; OnSubModuleLoad is
  the ecosystem convention.
- `PrefabExtensionAttribute` current ctor is **2-arg** `(string movie, string? xpath = null)`.
  The 3-arg overload with `autoGenWidgetName` (used by BannerKings and sketched in docs 04) is
  `[Obsolete("AutoGens are globally disabled…")]` but still functional.
- `ViewModelMixinAttribute(string refreshMethodName)`; also `(refreshMethodName, handleDerived)`.
- `Prefabs2.PrefabExtensionInsertPatch` (namespace **Prefabs2**, not Prefabs — Prefabs is the
  obsolete v1 API): abstract `InsertType Type { get; }`, `virtual int Index { get; }`, and exactly
  **one** member (property or method) carrying a content attribute — we use
  `[PrefabExtensionXmlNodes]` on an `IEnumerable<XmlNode>` property (XmlDocuments in the sequence
  are unwrapped to their DocumentElement). Patch classes need a **parameterless constructor**
  (`UIExtenderRuntime.Register` instantiates via `GetConstructor(Type.EmptyTypes)`).
- `InsertType` values: `Prepend, ReplaceKeepChildren, Replace, Child, Append, Remove`.
- Mixins: `BaseViewModelMixin<TViewModel>` with a **ctor taking the VM instance**
  (`ViewModelComponent.InitializeMixinsForVMInstance` does `Activator.CreateInstance(mixinType,
  instance)`); mixins attach at the end of the target VM's constructor (constructor transpiler).
  The base class provides `ViewModel` (WeakReference-backed, nullable), `OnRefresh()`,
  `OnFinalize()`, and typed `OnPropertyChangedWithValue` helpers.
- Packaging note: the `Bannerlord.UIExtenderEx` 2.13.2 **NuGet package ships only a
  `netstandard2.0` lib** (no net472 folder). It compiles fine into our net472 module with
  `IncludeAssets="compile"`; at runtime the module's own net472 DLL is loaded. No action needed,
  just don't be surprised by the package layout.

### Insert `Index` semantics (corrects docs 04 §A)

**[LOCAL: decompiled `PrefabComponent.InsertAsChild`]** `Index` indexes the anchor node's **raw
`ChildNodes` — XML comment nodes count**. `index >= ChildNodes.Count` appends after the last
child; negative clamps to 0. The vanilla `KingdomManagement.xml` anchors enumerate as:

- `descendant::KingdomTabControlListPanel[1]/Children`: `[0]comment, [1]ClanTabButton,
  [2]comment, [3]FiefsTabButton, [4]comment, [5]PoliciesTabButton, [6]comment, [7]ArmiesTabButton,
  [8]comment, [9]DiplomacyTabButton` (10 nodes). Docs 04's sketch "`Index => 5` — after the 5
  vanilla buttons" is therefore wrong: node 5 is *PoliciesTabButton* — that index inserts
  mid-strip. "Append after all buttons" = any index ≥ 10 (or ≥ count after other mods' patches).
  The tracer uses `Index => 9` (before DiplomacyTabButton) so vanilla's right-end-cap brush stays
  the rightmost button.
- `descendant::Widget[1]/Children`: `[0]Standard.Background, [1]comment, [2]top-panel Widget,
  [3]ArmiesPanel, [4]ClansPanel, [5]FiefsPanel, [6]PoliciesPanel, [7]DiplomacyPanel, [8]comment,
  [9]Standard.DialogCloseButtons, [10]KingdomDecision, [11]comment, [12]KingdomGiftFiefPopup`.
  The tracer inserts the panel at `Index => 3` — *before* every vanilla panel in render order, so
  if selection state ever goes stale with two panels visible, the vanilla one draws on top
  (BannerKings' Court panel survives on the same masking). P-14 still applies: with other
  UI-patching mods installed these indices shift; they are cosmetic only.

## S1 — custom widget registration: **RESOLVED — AUTO-DISCOVERY, no registration call**

**[LOCAL: decompiled `TaleWorlds.GauntletUI.WidgetInfo`,
`TaleWorlds.GauntletUI.PrefabSystem.WidgetFactory`, `TaleWorlds.Engine.GauntletUI.UIResourceManager`,
`TaleWorlds.MountAndBlade.GauntletUI.GauntletUISubModule`, `TaleWorlds.MountAndBlade.Module`]**

The full chain, with method names:

1. `Module.LoadSubModules` loads **every** module's submodule assemblies
   (`AssemblyLoader.LoadFrom` + `AddSubModule`) for all modules **first**, and only then calls
   `InitializeSubModuleBases()` → each `OnSubModuleLoad()` in load order. So our DLL is in the
   AppDomain before any submodule's load hook runs.
2. Native's `GauntletUISubModule.OnSubModuleLoad` → `RefreshResources(initialLoad: true)` →
   `WidgetInfo.Refresh()` → `CollectWidgetTypes()`, which scans
   `AppDomain.CurrentDomain.GetAssemblies()` and takes **every `Widget` subclass** from every
   assembly that references `TaleWorlds.GauntletUI.dll` (`CheckAssemblyReferencesThis`).
3. Then `UIResourceManager.Refresh()` → `WidgetFactory.Initialize(assemblyOrder)` maps each
   collected type by **bare class name**: `_builtinTypes[type.Name] = type` (name collisions
   resolved by submodule DLL load order — P-11's global-namespace warning is literal).
4. XML instantiation: `WidgetFactory.CreateBuiltinWidget(context, typeName)` requires a **public
   instance constructor taking exactly `(UIContext)`**.

Contract for a module-shipped widget: public `Widget` subclass, public `(UIContext)` ctor,
assembly references TaleWorlds.GauntletUI (any Widget usage does that) — then
`<DiplomacyOverviewTracerLineWidget />` in any prefab resolves. Nothing to call, nothing to
register. Runtime confirmation is the point of the manual test below.

Two corollaries worth recording:

- **UIExtenderEx's explicit widget registration is dead on v1.3.15.**
  `Bannerlord.UIExtenderEx.ResourceManager.WidgetFactoryManager.Register(Type)` guards on a
  reflection delegate for `WidgetInfo.Reload` — a method that does not exist in v1.3.15 (it is
  `Refresh` now), so the guard nulls out and `Register` silently no-ops. If auto-discovery had
  failed, UIExtenderEx would not have been the fallback; manual reflection into
  `WidgetFactory._builtinTypes` would be.
- **Prefab (movie) names are discovered per-module too**: `UIResourceManager.RefreshResourceDepot`
  adds every module's `GUI/` folder to the ResourceDepot; `WidgetFactory.
  GetPrefabNamesAndPathsFromCurrentPath` maps *file name without extension* → custom type. That is
  why `<DiplomacyOverviewRelationsPanel />` resolves to our
  `GUI/Prefabs/DiplomacyOverview/DiplomacyOverviewRelationsPanel.xml` with no registration either
  (and why prefab file names are a global namespace, P-11 — a duplicate name triggers a
  FailedAssert and last-writer-wins).

## S2 — line rendering fidelity: **PENDING IN-GAME** (both experiments deployed side by side)

Static grounding is done **[LOCAL: decompiled `Widget.OnRender`/`OnUpdatePosition`,
`TwoDimensionDrawContext.DrawSprite`, `Rectangle2D`]**:

- `Widget.Rotation` is a plain public float property (degrees) → settable from XML like any
  attribute; it feeds `AreaRect.LocalRotation` in `OnUpdatePosition`, and the matrix math
  (`RectangleHelper.CreateMatrixFrame`) honors it with pivot `(PivotX, PivotY)` — default (0,0) =
  top-left. Hit-testing (`Rectangle2D.IsPointInside`) transforms into local space, so rotated
  widgets hit-test correctly *in theory*. Whether layout/clipping/scissoring stay sane is the
  in-game question.
- `TwoDimensionDrawContext.DrawSprite(Sprite, SimpleMaterial, in Rectangle2D, float scale)` +
  `CreateSimpleMaterial()` (pooled — do NOT cache materials across frames) is exactly as docs 04
  §C describes. A drawn rect needs `Rectangle2D.Create()` (sets `IsValid` and identity matrices),
  local values, then `CalculateMatrixFrame(in parentRect)` — pass the widget's own `AreaRect` to
  express coordinates widget-locally. Widget-local drawing must multiply design pixels by the
  protected `_scaleToUse` (vanilla `OnRender` does the same); `Widget.Size` is already scaled.
  Sprite lookup at render time: `Context.SpriteData.GetSprite("BlankWhiteSquare_9")` (the
  TutorialArrowWidget pattern).

What the tracer will show (see manual script): technique A = red strip `Rotation="30"` vs gold
unrotated control strip sharing the same origin; technique B = custom widget drawing a green line
between two gold endpoint markers inside a gray border. The endpoint markers make rotation-sign or
pivot errors diagnosable from a single screenshot (line not connecting the markers ⇒ sign/pivot
convention differs from `atan2` screen-space assumption; the fix is negating the angle — record it
here after the run).

## P-10 — layer input focus: **RESOLVED for the tab shell** (evidence, not inference)

**[LOCAL: decompiled `SandBox.GauntletUI.GauntletKingdomScreen.OnActivate`]** The kingdom screen's
own layer does `InputRestrictions.SetInputRestrictions(true, InputUsageMask(7))`,
`IsFocusLayer = true`, `ScreenManager.TrySetFocus(...)`. An injected tab lives inside that layer
and inherits all of it — no input plumbing on our side. P-10 remains relevant **only** for the
standalone-screen fallback (docs 04 §B); this decompile is the pattern to copy if that fallback is
ever needed.

## Corrections to docs 04 / 07 (beyond the Index note above)

1. **docs 04 §A `OnRefresh` recipe has a latent gap.** The sketch (and the BannerKings exemplar)
   hooks `[ViewModelMixin("RefreshValues")]` and deselects the custom tab in `OnRefresh` when a
   vanilla `Show` flag is set. But **[LOCAL: decompiled `KingdomManagementVM`]** vanilla tab
   clicks run `ExecuteShowX → SetSelectedCategory`, which only flips the per-tab `Show` bools and
   **never calls `RefreshValues`** — so a RefreshValues-hooked OnRefresh does not fire on the
   exact interaction it exists for. (BannerKings gets away with it because its panels are inserted
   *before* the vanilla panels: a vanilla panel drawn on top masks the stale one; the stale
   *button highlight* bug is visible in BK if you look.) The tracer instead hooks
   `[ViewModelMixin("OnFrameTick")]` — `GauntletKingdomScreen.OnFrameTick` calls
   `DataSource.OnFrameTick()` every frame while the screen is open **[LOCAL]** — so the deselect
   check runs per frame (five bool reads; UIExtenderEx's hook shim does reflective attribute
   lookups per call, so the *final* design should either keep this and measure, or bind the
   panel/button state differently). Vanilla tab *buttons* need no such help:
   `KingdomTabControlListPanel.OnLateUpdate` mirrors panel visibility into vanilla buttons'
   `IsSelected` every frame; it never touches foreign buttons **[LOCAL]**.
2. **docs 04 §A registration snippet is now verified** (was `[WEB … verify at spike time]`):
   exact surface confirmed against the installed 2.13.2 DLL, see S3 above.
3. **docs 04 §C custom-widget registration question is now closed** (was `[UNVERIFIED spike S1]`):
   auto-discovery, see S1 above. The GABS-doc caveat about explicit registration is obsolete for
   v1.3.15.
4. **docs 07 spike table**: S1 ✔ resolved (auto-discovery), S3 ✔ resolved (Create/Register/Enable
   in OnSubModuleLoad), S2 ⏳ pending the game run below. S4/S5 untouched.
5. **docs 03 csproj blueprint (P-04 adjacent)**: net472 + Widget APIs needs the
   `System.Numerics.Vectors` **4.5.0** package (`IncludeAssets="compile"`, `PrivateAssets="all"`)
   because `Widget.Size`/`Rectangle2D` expose `System.Numerics.Vector2`. The game ships
   `System.Numerics.Vectors.dll` in `bin\Win64_Shipping_Client`, so compile-only is correct and
   P-03 stays satisfied (verified: deployed `bin` contains only `DiplomacyOverview.dll/.pdb`).
6. **Tab hotkey cycling excludes injected tabs**: `_categoryCount = 5` is private and
   `SelectNextCategory/SelectPreviousCategory` cycle only vanilla categories **[LOCAL]**. Same
   limitation in BannerKings. Gamepad/hotkey reachability of the Relations tab is a design TODO
   for the real implementation (not a tracer goal).

## Go / no-go: Kingdom-tab shell vs standalone screen — **PENDING IN-GAME**

Decision inputs after the run: tab renders and selects/deselects cleanly → **GO** for the tab
shell (docs 04 §A). Tab strip breaks, injection asserts, or clicks misbehave in ways the fallback
matrix below doesn't cover → standalone screen (docs 04 §B), for which the P-10 input recipe is
now documented above.

---

## Manual test script (single game run, no dev tools needed)

Preconditions: module built & deployed (`dotnet build DiplomacyOverview.sln -c Release` with
`BANNERLORD_GAME_DIR` set — already done if you're reading this after a green build), game
v1.3.15, BLSE installed.

1. **Launch** Bannerlord via **BLSE LauncherEx**. In the Singleplayer mod list, make sure
   **Harmony, ButterLib, UIExtenderEx, Diplomacy Overview** are all ticked (tick Diplomacy
   Overview once — first deploy is never auto-enabled, P-15) and Diplomacy Overview sits **below**
   Harmony/UIExtenderEx in load order (LauncherEx sorts this automatically). Click PLAY.
   - *Game fails to reach main menu / error popup mentioning DiplomacyOverview or UIExtenderEx*:
     screenshot the popup. This means registration or module metadata is broken — stop here.
2. **Load any campaign save** where your character belongs to a kingdom (any vassal or ruler
   save; a mercenary save also opens the screen but tabs behave differently — prefer a vassal).
3. Press **K** (or bottom-bar kingdom icon) to open the **Kingdom screen**.
4. **Look at the tab strip** (top center): expected order
   `Clans | Fiefs | Policies | Armies | Relations | Diplomacy` — our **Relations** tab sits
   between Armies and Diplomacy, styled like the middle tabs.
   - 📸 **Screenshot 1: the whole screen before clicking anything.**
   - *No Relations tab at all*: UIExtenderEx patch didn't apply (check
     `Modules\DiplomacyOverview\` exists and the module was ticked). If an in-game red error
     banner appeared mentioning "Failed to apply extension to KingdomManagement", the XPath
     anchor failed — screenshot it.
5. **Click the Relations tab.** Expected: the five vanilla panels disappear and a dark panel
   appears with the title "Diplomacy Overview - tracer (issue 5)" and TWO experiment areas:
   - **Left (A)**: a horizontal **gold** strip and, starting at the same left end, a **red**
     strip angled ~30° downward. Two separate bars = XML `Rotation` works. Red exactly on top of
     gold (both horizontal) = rotation silently ignored. Red bar missing = rotation broke
     rendering. Angled *upward* instead of downward = rotation sign is counter-clockwise —
     fine, just note it.
   - **Right (B)**: a thin **gray border** rectangle, two small **gold squares** inside it, and a
     **green** line. The green line should run corner-ish from upper-left to lower-right,
     **connecting the centers of the two gold squares**.
     - Green line connects the squares → technique B fully proven (position, angle, scale, tint).
     - Green line present but *not* touching the squares (mirrored/offset) → rotation sign or
       pivot mismatch; note which way it's off. One-line fix, technique still viable.
     - Gray border yes, green/gold missing → `DrawSprite` path broken or sprite not loaded.
     - Nothing at all in the right half, not even the border → the custom widget didn't
       instantiate (S1 auto-discovery failed in practice) — check `rgl_log` / crash uploader for
       "builtin widget type not found in CreateBuiltinWidget(DiplomacyOverviewTracerLineWidget)".
   - 📸 **Screenshot 2: the Relations panel** (this is the issue's acceptance screenshot).
   - 📸 **Screenshot 3: zoom/crop of area B** if anything about the green line looks off.
6. **Tab interplay** — click, in order: **Clans → Relations → Armies → Relations → Diplomacy**.
   Expected every time: exactly one panel visible; selecting a vanilla tab hides our panel *and*
   un-highlights our tab button (this specific direction is the OnFrameTick-hook experiment);
   selecting Relations hides the vanilla panel and highlights our button.
   - *Our tab stays highlighted while a vanilla panel shows, or two panels stack*: the
     deselect hook failed — 📸 screenshot the stale state.
7. **Click-through probe**: with Relations open, click somewhere in the middle of the dark panel
   (not on a label), then click the **Done** button bottom-center. Panel clicks should do nothing
   (no map click-through underneath), Done should close the screen normally.
8. **Reopen** the kingdom screen (K). Expected: opens on the vanilla default tab (Fiefs), our tab
   deselected. Open **Esc menu → resume** once with the screen open to shake out anything odd.
9. Afterwards, drop the screenshots into the issue #5 thread. If the game crashed at any point,
   grab the crash-uploader text or `ProgramData\Mount and Blade II Bannerlord\logs\rgl_log_*.txt`.

### What each failure decides (docs 04 §C fallback matrix)

| Observation | Verdict recorded here |
|---|---|
| A rotated correctly | `Widget.Rotation` viable → simplest per-edge line = rotated child widgets, VM-bound (docs 04 §C technique 1 simple variant) |
| A ignored/broken, B works | Lines are drawn in `OnRender` by one canvas widget (technique 1 recommended variant) — the architecture already assumes this |
| B works but mirrored/offset | Same as above + note the sign/pivot correction constant |
| B missing entirely but A works | Ship lines as rotated plain widgets; custom widget path needs the S1 runtime discrepancy diagnosed first |
| A *and* B both broken | Fall back to stamped-dots lines (TutorialArrowWidget pattern, docs 04 §C technique 2) — dashed styles become the default look |
| Tab never appears / screen breaks | Kingdom-tab shell **no-go** → standalone screen (docs 04 §B) using the P-10 recipe above |

---

## Addendum — tracer run 1 results (2026-07-02) and run-2 changes

Run 1 (user's modded campaign, load order ends `…*Bannerlord.Diplomacy*…*DiplomacyOverview*`):
tab appeared as an **empty, textless button** right of the Diplomacy tab; user read it as "no
Relations tab". Diagnosis chain, all **[LOCAL]**:

1. Module enabled + loaded (LauncherData, rgl_log `_MODULES_` args); UIExtenderEx logged our
   runtime `Register → Register Types → Enabled` cleanly (`Configs\ModLogs\`).
2. **Prefab patches APPLIED** — proven by flipping UIExtenderEx's `DumpXML` SubModule tag
   (`Modules\Bannerlord.UIExtenderEx\SubModule.xml`, dumps to that module's `Dumps\` per runtime):
   `KingdomManagement_DiplomacyOverview.xml` contains both our button (with its TextWidget child)
   and our panel node. Constants resolve. The Diplomacy mod's own patches shift our button after
   its Factions insertion (P-14, cosmetic, as designed).
3. Therefore the failing layer is the **mixin binding** (`@RelationsText` empty ⇒ no label). No
   errors anywhere — several UIExtenderEx failure paths are `MessageUtils.Fail` = silent in
   release.
4. Reference point: the **Diplomacy mod's Factions tab works in the same session and is a genuine
   UIExtenderEx mixin** (`[ViewModelMixin]` no-arg; `FactionsLabel { get; set; }` assigned in
   ctor; `[DataSourceMethod] ExecuteShowFactions`). So the mixin mechanism itself works on
   v1.3.15 + UIExtenderEx 2.13.2. Decompile of `ViewModelExtensions.AddProperty` confirms the
   per-instance copy-on-write against `ViewModel._propertiesAndMethods`/`_cachedViewModelProperties`
   is 1.3.15-compatible.
5. `DataSource="{..}"` on tab-button texts turns out to be a **vanilla idiom** (Fiefs/Policies use
   it) that Diplomacy mirrors; at root context it appears to clamp to the root VM.

**Run-2 tracer changes** (eliminate every delta vs the proven-working mixin at once, and
instrument the seams):

- Mixin now mirrors Diplomacy's exact shape: `[ViewModelMixin]` **without** refresh argument
  (supersedes run 1's `"OnFrameTick"` hook — which is thereby *unproven, not validated*; the
  deselect-on-vanilla-click concern moves to the production design, temporarily masked by panel
  z-order like Diplomacy/BannerKings), `RelationsText` as a plain settable property assigned in
  the ctor (was getter-only expression).
- Button text mirrors the working XML idiom: `DataSource="{..}"`; plus a literal `*` marker
  TextWidget (no binding) so the button is visually locatable regardless.
- File instrumentation (`Configs\ModLogs\DiplomacyOverview-tracer.log`): SubModule enable, mixin
  attach (+ VM runtime type — would expose a VM-subclass conflict), first binder read of
  `RelationsText`, `SelectRelations` invocation.

Run-2 decision matrix (replaces "no tab" row above): label shows → binding fixed, proceed to S2
observations. Marker `*` shows but no label + log has "attached" but no "read" → binder never
queries mixin props on this setup → escalate (root-cause in UIExtenderEx property injection).
Log lacks "attached" → mixin never instantiated (check VM runtime type line / UIExtenderEx issue).
Click logged but panel doesn't show → panel-side binding (`IsVisible="@RelationsSelected"`) next.
