using System.Collections.Generic;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DiplomacyOverview.UI.Mixins
{
    // Anchors verified against Modules\SandBox\GUI\Prefabs\Clan\ClanScreen.xml (v1.4.7). The clan
    // tab buttons carry NO Id (unlike the kingdom screen's DiplomacyTabButton), so we anchor on
    // distinctive attributes/elements, each confirmed unique in the prefab:
    //   - Tab: the Income button is the only one with Brush="Header.Tab.Right" (the right end-cap).
    //     Prepend before it → our Relations tab lands between Fiefs and Income as a mid-strip
    //     Header.Tab.Center button ("right after Fiefs"). Anchor-relative Prepend is immune to index
    //     drift from other mods (P-14).
    //   - Panel: the four vanilla panels are siblings in the "Lower Half" container; <ClanIncome> is
    //     the only such element. Prepend our panel there as a sibling. Only the selected panel is
    //     visible (each root binds IsVisible="@IsSelected"), so sibling order is cosmetic.

    /// <summary>Inserts the Relations tab button immediately before the Income (right end-cap) tab.</summary>
    [PrefabExtension("ClanScreen", "descendant::ButtonWidget[@Brush='Header.Tab.Right']")]
    internal sealed class ClanScreenTabButtonExtension : PrefabExtensionInsertPatch
    {
        private readonly List<XmlNode> _nodes;

        public ClanScreenTabButtonExtension()
        {
            var button = new XmlDocument();
            button.LoadXml(
                "<ButtonWidget DoNotPassEventsToChildren=\"true\" WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" " +
                "SuggestedWidth=\"!Header.Tab.Center.Width.Scaled\" SuggestedHeight=\"!Header.Tab.Center.Height.Scaled\" " +
                "PositionYOffset=\"6\" Brush=\"Header.Tab.Center\" IsSelected=\"@RelationsSelected\" " +
                "Command.Click=\"SelectRelations\" UpdateChildrenStates=\"true\">" +
                "<Children>" +
                "<TextWidget WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"StretchToParent\" " +
                "Brush=\"Clan.TabControl.Text\" Text=\"@RelationsText\" />" +
                "</Children>" +
                "</ButtonWidget>");
            _nodes = new List<XmlNode> { button };
        }

        public override InsertType Type => InsertType.Prepend;

        [PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> Nodes => _nodes;
    }

    /// <summary>
    /// Inserts the Relations content panel among the vanilla clan panels (the "Lower Half"
    /// container, which already carries the MarginTop/Bottom the vanilla panels rely on, so the
    /// panel needs none of its own). DataSource="{Relations}" re-scopes it onto the mixin's
    /// RelationsVM; its prefab root binds IsVisible="@IsSelected" against that VM, exactly like the
    /// vanilla clan panels bind against their sub-VMs.
    /// </summary>
    [PrefabExtension("ClanScreen", "descendant::ClanIncome")]
    internal sealed class ClanScreenRelationsPanelExtension : PrefabExtensionInsertPatch
    {
        private readonly List<XmlNode> _nodes;

        public ClanScreenRelationsPanelExtension()
        {
            var panel = new XmlDocument();
            panel.LoadXml(
                "<DiplomacyOverviewRelationsPanel Id=\"DiplomacyOverviewRelationsPanel\" DataSource=\"{Relations}\" />");
            _nodes = new List<XmlNode> { panel };
        }

        public override InsertType Type => InsertType.Prepend;

        [PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> Nodes => _nodes;
    }
}
