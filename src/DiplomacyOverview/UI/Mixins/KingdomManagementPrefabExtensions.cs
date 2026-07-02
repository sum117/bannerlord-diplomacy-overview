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
    /// Inserts the Relations tab button into the tab strip. Index 9 lands before the
    /// DiplomacyTabButton (node 9) in an unpatched document, keeping vanilla's right-end cap
    /// brush (Header.Tab.Right) as the rightmost button. Attribute set mirrors the vanilla
    /// center tabs; !Header.Tab.Center.* constants are declared at the top of the same prefab
    /// document, so the spliced node can reference them.
    /// </summary>
    [PrefabExtension("KingdomManagement", "descendant::KingdomTabControlListPanel[1]/Children")]
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
                // Literal marker (no binding involved): locates the button visually even if the
                // mixin-bound label fails again. Remove after the tracer.
                "<TextWidget WidthSizePolicy=\"CoverChildren\" HeightSizePolicy=\"CoverChildren\" " +
                "HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" MarginRight=\"4\" Brush=\"Clan.TabControl.Text\" Text=\"*\" />" +
                "</Children>" +
                "</ButtonWidget>");
            _nodes = new List<XmlNode> { button };
        }

        public override InsertType Type => InsertType.Child;

        public override int Index => 9;

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
    /// that prefab's root widget. No DataSource: the panel binds against KingdomManagementVM
    /// itself, whose property set includes our mixin's properties.
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
                "IsVisible=\"@RelationsSelected\" MarginTop=\"188\" MarginBottom=\"75\" />");
            _nodes = new List<XmlNode> { panel };
        }

        public override InsertType Type => InsertType.Child;

        public override int Index => 3;

        [PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> Nodes => _nodes;
    }
}
