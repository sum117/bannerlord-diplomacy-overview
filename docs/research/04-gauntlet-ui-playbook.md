# 04 — Gauntlet UI playbook

Everything here is grounded in the installed game's prefab XML (`Modules\SandBox\GUI\...`), the
decompiled v1.3.15 UI assemblies, and two shipping kingdom-screen-extending mods (BannerKings,
Diplomacy). **[LOCAL]** unless noted.

## Stack in one paragraph

`ScreenManager` pushes `ScreenBase`s; a screen adds one or more `GauntletLayer`s; a layer loads a
**movie** = prefab XML (from `GUI\Prefabs\**`) bound to a **ViewModel** (`TaleWorlds.Library.ViewModel`).
XML element names map to C# `Widget` subclasses; `Brush`es (in `GUI\Brushes\*.xml`) bundle sprites,
styles, states, fonts. Binding syntax in prefab XML: `Text="@Prop"` (VM property, needs
`[DataSourceProperty]`), `Command.Click="ExecuteFoo"` (VM method / `[DataSourceMethod]` on mixins),
`DataSource="{ChildVm}"` (re-scope), `*Param` (prefab parameters), `!Constant` (brush/layout constants).

## A. Recommended shell: inject a "Relations" tab into the Kingdom screen

Vanilla structure **[LOCAL: `SandBox\GUI\Prefabs\KingdomManagement\KingdomManagement.xml`]**:

- The tab strip is a custom `KingdomTabControlListPanel` widget hardwired to five
  button/panel pairs (`ClansButton="ClanTabButton" ClansPanel="..\..\ClansPanel"` …).
- Tab buttons are plain `ButtonWidget`s, brushes `Header.Tab.Left|Center|Right`, text brush
  `Clan.TabControl.Text`, click commands `ExecuteShowClan` etc.
- Per-tab VMs on `KingdomManagementVM`: `Clan`, `Settlement` (= Fiefs), `Policy`, `Army`,
  `Diplomacy` — each with a `Show` bool. **[LOCAL: decompiled `KingdomManagementVM`]**

**The proven injection recipe** (BannerKings does exactly this for four extra tabs; Diplomacy has the
same mixin/extension pair — two mods demonstrably coexisting this way): **[LOCAL: decompiled
`BannerKings.UI.Extensions.KingdomManagementMixin/Extension`; WEB: both repos]**

1. **ViewModel mixin** — add state + commands to the vanilla VM:

```csharp
[ViewModelMixin("RefreshValues")]                       // hook vanilla RefreshValues → OnRefresh
internal sealed class KingdomManagementMixin : BaseViewModelMixin<KingdomManagementVM>
{
    [DataSourceProperty] public bool RelationsSelected { get; /* set → OnPropertyChangedWithValue */ }
    [DataSourceProperty] public string RelationsText => new TextObject("{=DipOv001}Relations").ToString();
    [DataSourceProperty] public RelationsVM Relations { get; }   // our whole tab's VM

    [DataSourceMethod]
    public void SelectRelations()
    {
        var vm = ViewModel!;                    // hide all vanilla panels:
        vm.Clan.Show = vm.Settlement.Show = vm.Policy.Show = vm.Army.Show = vm.Diplomacy.Show = false;
        RelationsSelected = true; Relations.IsSelected = true;
        vm.RefreshValues();
    }

    public override void OnRefresh()            // vanilla tab clicked → deselect ours
    {
        var vm = ViewModel!;
        if (vm.Clan.Show || vm.Settlement.Show || vm.Policy.Show || vm.Army.Show || vm.Diplomacy.Show)
        { RelationsSelected = false; Relations.IsSelected = false; }
    }
}
```

2. **Prefab extensions** — insert the button into the tab strip and the panel into the screen:

```csharp
[PrefabExtension("KingdomManagement", "descendant::KingdomTabControlListPanel[1]/Children", "KingdomManagement")]
internal sealed class TabButtonExtension : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Child;
    public override int Index => 5;             // after the 5 vanilla buttons
    [PrefabExtensionXmlNodes] public IEnumerable<XmlNode> Nodes { get; } = /* ButtonWidget XML:
      Id="RelationsTabButton" Brush="Header.Tab.Center" IsSelected="@RelationsSelected"
      Command.Click="SelectRelations" UpdateChildrenStates="true"
      + child TextWidget Brush="Clan.TabControl.Text" Text="@RelationsText" */;
}

[PrefabExtension("KingdomManagement", "descendant::Widget[1]/Children", "KingdomManagement")]
internal sealed class TabPanelExtension : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Child;
    public override int Index => 4;             // among the sibling tab panels
    [PrefabExtensionXmlNodes] public IEnumerable<XmlNode> Nodes { get; } =
      /* <DiplomacyOverviewRelationsPanel DataSource="{Relations}" MarginTop="188" MarginBottom="75" /> */;
}
```

   BannerKings additionally widens the strip via `PrefabExtensionSetAttributePatch`
   (`KingdomTabControlListPanel` → `WidthSizePolicy=StretchToParent` + margins, and each vanilla
   `ButtonWidget[@Id='…TabButton']` → `StretchToParent`) so 5+N tabs fit. If BannerKings is also
   installed, both mods apply compatible attribute patches (same values) — still verify visually
   (doc 06). The custom panel prefab itself is a normal XML file in our `GUI\Prefabs\`.

3. **Registration** in `SubModule` (`MBSubModuleBase`):
   `_extender = UIExtender.Create("DiplomacyOverview"); _extender.Register(typeof(SubModule).Assembly); _extender.Enable();`
   **[WEB: UIExtenderEx README pattern — verify exact overloads against v2.13.2 at spike time]**

Key insight: the vanilla `KingdomTabControlListPanel` never needs patching — it only manages its own
five pairs; our button drives selection purely through VM state, and `OnRefresh` handles the
"vanilla tab clicked" direction. Selection visuals come from `IsSelected` + `UpdateChildrenStates`.

Note on `Index`: insertion indices are evaluated against the *current* (possibly already-patched)
XML, so ordering vs other mods is load-order-dependent. Cosmetic-only issue for a tab strip; don't
rely on absolute positions for anything semantic.

## B. Fallback shell: standalone screen

If tab-strip crowding (RoT theming + BannerKings tabs) ever bites: `ScreenManager.PushScreen` a
`ScreenBase` + `GauntletLayer` (own `GameState` like vanilla's `KingdomState` — plain class,
`IsMenuState=true`, created via `GameStateManager.Current.CreateState<T>()`), opened from a hotkey
(`HotKeyManager.RegisterContext(GameKeyContext, …)`) and/or a map-bar button injected via
`MapNavigationVM` mixin. Full walkthrough exists. **[LOCAL: `C_ScreenManager/C_KingdomState/C_HotKeyManager`;
WEB: [coda.io Gauntlet tutorial](https://coda.io/@samuel/bannerlord-wiki/gauntlet-ui-tutorial-26)]**
Decide at milestone M0; the graph panel itself is shell-agnostic.

## C. Drawing the relation lines (the novel 5%)

Ground truth from decompiled v1.3.15 **[LOCAL]**:

- `Widget` render hook: `protected virtual void OnRender(TwoDimensionContext, TwoDimensionDrawContext)`.
- `TwoDimensionDrawContext` (TaleWorlds.TwoDimension.dll): `CreateSimpleMaterial()`,
  `DrawSprite(Sprite, SimpleMaterial, in Rectangle2D, float scale)`, scissoring, and
  `SetCircualMask(Vector2, float, float)` (sic — typo is in the API).
- Geometry primitive `Rectangle2D`: `LocalPosition`, `LocalScale`, `LocalPivot`, **`LocalRotation`
  (float, degrees)**. No arbitrary quads/meshes — `DrawObject2D` was removed in v1.3. **Rotation of a
  rectangle is the only angled primitive, and it's first-class** (`Widget.Rotation` feeds
  `AreaRect.LocalRotation` in `OnUpdatePosition`).
- Vanilla never draws point-to-point angled connectors. Its two techniques:
  (a) troop-tree connectors = stretched child `BrushWidget`s with canned diagonal art states
  (`SetState("Left"/"Right"/"Straight")`); (b) `TutorialArrowWidget` = N small sprites
  (`BlankWhiteCircle`) lerp-stamped along the segment (dotted line).

**Technique ranking for us:**

1. **One rotated stretched sprite per edge (recommended).** A custom `Widget` per line — or one
   canvas widget that draws all edges in `OnRender` via `DrawSprite` with
   `Rectangle2D{ LocalScale=(length,thickness), LocalRotation=angleDeg, LocalPivot=(0,0.5) }` and a
   tinted `SimpleMaterial`. Sprite: the native plain-white `BlankWhiteSquare_9` (seen tinted inline in
   vanilla prefabs), so **no custom sprite assets at all**. The simpler variant — child widgets with
   `Widget.Rotation` set from the VM — needs a spike to confirm rotation applies cleanly at layout
   level (it demonstrably exists in the render path). **[LOCAL + UNVERIFIED spike S2]**
2. **Stamped dots** (TutorialArrowWidget pattern) — trivial, and dashes/dots are a feature:
   distinct line *styles* (e.g. dashed = non-aggression pact) help colorblind users.
3. Canned-art diagonals — unsuitable for arbitrary angles; ignore.

**Custom widget registration:** Gauntlet resolves XML element names to `Widget` subclasses via its
`WidgetFactory`; UIExtenderEx/GABS document custom-widget registration. Exact mechanism for
module-shipped widgets (auto-discovery of loaded SubModule assemblies vs explicit
`WidgetFactory` registration) = **spike S1**. **[WEB: [GABS gauntlet-ui.md](https://github.com/BUTR/Bannerlord.GABS/blob/master/docs/gauntlet-ui.md); UNVERIFIED]**
Prefix widget class names uniquely (`DiplomacyOverview…Widget`) — element names are global.

## D. Banner medallion nodes

- v1.3 way **[LOCAL]**: `new BannerImageIdentifierVM(faction.Banner, nineGrid: false)`
  (`ImageIdentifierVM` is now abstract; `BannerCode` class is gone — `Banner` itself has a
  `BannerCode` string property if serialization is ever needed).
- Prefab side: `MaskedTextureWidget DataSource="{Banner}" ImageId="@Id" AdditionalArgs="@AdditionalArgs"
  TextureProviderName="@TextureProviderName"` + a frame brush. Vanilla brushes:
  `Flat.Tuple.Banner.Small(.Hero)` (`StdAssets\banner_tuple`), `Kingdom.TornBanner.Big`
  (~105×126). **[LOCAL: KingdomManagement.xml usage]**
- The mockup's circular medallions: try a circular frame brush first; if none fits, custom-draw with
  `SetCircualMask` in our canvas widget. Node = `ButtonWidget` (hover/click → encyclopedia link or
  highlight) wrapping the banner + `TextWidget` label.

## E. Dropdown (Kingdoms ⇄ Clans) and toggles (legend)

- **Dropdown** **[LOCAL: `SandBox\GUI\Prefabs\Clan\ClanPartyRoleDropdown.xml`]**: `DropdownWidget`
  (attrs `Button="DropdownButton"`, `ListPanel="SelectionList"`, `CurrentSelectedIndex="@SelectedIndex"`,
  `TextWidget="DropdownButton\SelectedTextWidget"`) + `ListPanel DataSource="{ItemList}"` with
  `ButtonWidget ButtonType="Radio"` items (`Text="@StringItem"`). VM: `SelectorVM<SelectorItemVM>`
  — ctor `(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<T>> onChange)`;
  `ItemList`, `SelectedIndex`, `SelectedItem`, `SetOnChangeAction`. **[LOCAL decompile]**
- **Toggles** **[LOCAL: `Encyclopedia\EncyclopediaList\EncyclopediaFilterListItem.xml`]**:
  `ButtonWidget ButtonType="Toggle" IsSelected="@IsSelected" Command.Click="ExecuteToggle…"` with a
  checkbox sprite pair (`Encyclopedia\list_filters_checkbox[_full]`). Legend row = toggle + a small
  color swatch (tinted `BlankWhiteSquare_9` widget) + label — matches the mockup's legend exactly.

## F. Styling to match vanilla

- Colors: widgets accept inline `Color="#RRGGBBAA"` and `Brush.FontColor="#…"`; brush styles support
  `ColorFactor/AlphaFactor/HueFactor`. Line palette proposal (from mockup): war `#C0392BFF`,
  trade `#D4A017FF`, alliance `#4E9B47FF`, NAP (dashed) `#D4A017FF` or a distinct teal. **[LOCAL]**
- Reusable brush inventory: tab brushes (`Header.Tab.*`, `Clan.TabControl.Text`), screen chrome
  (`Standard.DialogCloseButtons` prefab + brushes, top header sprite `SPKingdom\kingdom_top_header`),
  dropdown brushes (`SPClan.RoleSelection.Dropdown`, `SPOptions.Dropdown.*`), banner frames (§D).
  Brush files: `SandBox\GUI\Brushes\{Kingdom,Clan,Encyclopedia}.xml`, `Native\GUI\Brushes\Standard.xml`.
- **Custom sprites: avoid entirely.** New sprites require the separate Modding Kit's
  `SpriteSheetGenerator` → `.tpac` AssetPackages + generated SpriteData — heavyweight and the kit
  isn't installed here. Everything the mockup needs exists as native sprites + tinting.
  **[WEB: [official sprite-sheet docs](https://moddocs.bannerlord.com/asset-management/generating_and_loading_ui_sprite_sheets/); LOCAL: no wEditor bin present]**

## G. Iteration loop

Prefab XML in our module's `GUI\Prefabs\` hot-reloads on screen reopen (doc 02 §7) — build the
layout XML-first, keep C# widgets thin. Useful runtime spelunking patterns (find widget positions,
inspect live VMs): GABS doc's `FindChild`/`GetAllChildrenRecursive` recipes. **[WEB]**
