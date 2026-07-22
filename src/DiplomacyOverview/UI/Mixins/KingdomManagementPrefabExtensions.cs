using System.Collections.Generic;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DiplomacyOverview.UI.Mixins
{
    // XPath anchors verified against the installed game's prefab
    // (Modules\SandBox\GUI\Prefabs\KingdomManagement\KingdomManagement.xml, v1.3.15):
    //   - descendant::KingdomTabControlListPanel[1]/Children = the tab strip (5 ButtonWidgets
    //     interleaved with 5 comment nodes -> 10 child nodes).
    //   - descendant::Widget[1]/Children = the screen root's children (Standard.Background,
    //     top panel, the five *Panel siblings, close buttons, decision/gift popups).
    //
    // Insert semantics (decompiled Bannerlord.UIExtenderEx 2.13.2 PrefabComponent.InsertAsChild):
    // Index counts RAW XmlNodes of the anchor — comments included — and clamps: index >= count
    // appends after the last child. Indices therefore shift when other mods patch first (P-14);
    // they are cosmetic ordering only, nothing semantic hangs on them.

    /// <summary>
    /// Inserts the Relations tab button as the sibling immediately BEFORE the vanilla
    /// DiplomacyTabButton (anchor-relative Prepend — immune to other mods shifting child
    /// indices, P-14; run 5 showed a fixed index drifting past the right end-cap once the
    /// Diplomacy mod's Factions insertion shifted the strip). Diplomacy keeps its
    /// Header.Tab.Right end-cap as the rightmost button; ours renders mid-strip.
    /// !Header.Tab.Center.* constants are declared at the top of the same prefab document,
    /// so the spliced node can reference them.
    ///
    /// Known, accepted cosmetic limitation: a 6th tab widens the centered vanilla strip until its
    /// right edge slides under the top-right leader portrait. We deliberately do NOT reflow the
    /// shared strip to fix it — that is a global mutation of a widget every mod's tabs occupy,
    /// which breaks our additive-only posture worse than adding one node does (P-25; short label
    /// per P-21 is the only in-remit mitigation).
    /// </summary>
    [PrefabExtension("KingdomManagement", "descendant::ButtonWidget[@Id='DiplomacyTabButton']")]
    internal sealed class KingdomManagementTabButtonExtension : PrefabExtensionInsertPatch
    {
        private readonly List<XmlNode> _nodes;

        public KingdomManagementTabButtonExtension()
        {
            var button = new XmlDocument();
            button.LoadXml(
                "<ButtonWidget Id=\"DiplomacyOverviewRelationsTabButton\" DoNotPassEventsToChildren=\"true\" " +
                "WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" " +
                "SuggestedWidth=\"!Header.Tab.Center.Width.Scaled\" SuggestedHeight=\"!Header.Tab.Center.Height.Scaled\" " +
                "VerticalAlignment=\"Center\" PositionYOffset=\"2\" Brush=\"Header.Tab.Center\" " +
                "IsSelected=\"@RelationsSelected\" Command.Click=\"SelectRelations\" UpdateChildrenStates=\"true\">" +
                "<Children>" +
                // DataSource="{..}" mirrors the exact idiom of the two tab texts PROVEN to bind in
                // this modlist (vanilla Fiefs + Diplomacy's Factions) — tracer run 2 evidence.
                "<TextWidget DataSource=\"{..}\" WidthSizePolicy=\"CoverChildren\" HeightSizePolicy=\"CoverChildren\" " +
                "HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" Brush=\"Clan.TabControl.Text\" Text=\"@RelationsText\" />" +
                "</Children>" +
                "</ButtonWidget>");
            _nodes = new List<XmlNode> { button };
        }

        public override InsertType Type => InsertType.Prepend;

        [PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> Nodes => _nodes;
    }

    /// <summary>
    /// Inserts the Relations content panel among the five vanilla tab panels. Index 3 puts it
    /// BEFORE ArmiesPanel (node 3), i.e. earlier in render order than every vanilla panel:
    /// if selection state ever goes stale with both a vanilla panel and ours visible, the
    /// vanilla panel draws on top and wins (same self-masking BannerKings relies on).
    /// The element name resolves to our prefab file
    /// _Module/GUI/Prefabs/DiplomacyOverview/DiplomacyOverviewRelationsPanel.xml via
    /// WidgetFactory custom-type (prefab filename) discovery; attributes here are applied to
    /// that prefab's root widget. The node mirrors the vanilla panel idiom exactly
    /// (&lt;ArmiesPanel DataSource="{Army}" MarginTop="188" MarginBottom="75" /&gt;):
    /// DataSource="{Relations}" re-scopes the panel onto the mixin's RelationsVM, and the
    /// prefab root binds IsVisible="@IsSelected" against that VM — just like the vanilla
    /// panels' IsVisible="@Show".
    /// </summary>
    [PrefabExtension("KingdomManagement", "descendant::Widget[1]/Children")]
    internal sealed class KingdomManagementRelationsPanelExtension : PrefabExtensionInsertPatch
    {
        private readonly List<XmlNode> _nodes;

        public KingdomManagementRelationsPanelExtension()
        {
            var panel = new XmlDocument();
            panel.LoadXml(
                "<DiplomacyOverviewRelationsPanel Id=\"DiplomacyOverviewRelationsPanel\" " +
                "DataSource=\"{Relations}\" MarginTop=\"188\" MarginBottom=\"75\" />");
            _nodes = new List<XmlNode> { panel };
        }

        public override InsertType Type => InsertType.Child;

        public override int Index => 3;

        [PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> Nodes => _nodes;
    }
}
