using System;
using System.Collections.Generic;
using DiplomacyOverview.Behaviors;
using DiplomacyOverview.Core;
using DiplomacyOverview.Providers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// The Relations tab's whole view model: kingdom nodes on a circle plus their relation edges,
    /// projected into a fixed design-space canvas (the prefab hosts a matching fixed-size canvas
    /// widget, so all coordinates here are canvas design pixels).
    ///
    /// Rebuilds are lazy and pull-based: <see cref="RebuildIfNeeded"/> runs on tab selection only,
    /// and only when this instance never built or <see cref="RelationsDirtyBehavior"/> flagged a
    /// diplomacy change — never per-frame, never per-tick (issue #6 acceptance).
    /// </summary>
    internal sealed class RelationsVM : ViewModel
    {
        // ---- Design-space geometry -------------------------------------------------------------
        // The canvas is a fixed-size widget centered in the panel; these constants MUST stay in
        // sync with the <Constants> block of DiplomacyOverviewRelationsPanel.xml.
        public const float CanvasWidth = 1400f;
        public const float CanvasHeight = 800f;

        /// <summary>Node circle radius. 310 leaves label room: top box edge at y=47, bottom at 795.</summary>
        private const double CircleRadius = 310.0;

        /// <summary>Edge-trim radius around each medallion center (banner is ~75x86 design px).</summary>
        private const double NodeTrimRadius = 46.0;

        // Node box: 170x128 with the ~75x86 banner centered horizontally at the top and the name
        // label underneath — mirrored by the prefab's node ItemTemplate. The banner center inside
        // the box is the node's layout center.
        private const float NodeCenterOffsetX = 85f;
        private const float NodeCenterOffsetY = 43f;

        private const float EdgeThickness = 4f;

        private static readonly GraphCanvasSpec CanvasSpec =
            new GraphCanvasSpec(CanvasWidth, CanvasHeight, CircleRadius, NodeTrimRadius);

        // ---- State -------------------------------------------------------------------------------

        private readonly IRelationProvider[] _providers = { new WarProvider() };

        private MBBindingList<RelationNodeVM> _nodes = new MBBindingList<RelationNodeVM>();
        private MBBindingList<RelationEdgeVM> _edges = new MBBindingList<RelationEdgeVM>();
        private string _titleText;
        private bool _isSelected;
        private bool _built;

        public RelationsVM()
        {
            _titleText = new TextObject("{=DipOvTitle1}Relations").ToString();
        }

        [DataSourceProperty]
        public MBBindingList<RelationNodeVM> Nodes
        {
            get => _nodes;
            set
            {
                if (!ReferenceEquals(value, _nodes))
                {
                    _nodes = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<RelationEdgeVM> Edges
        {
            get => _edges;
            set
            {
                if (!ReferenceEquals(value, _edges))
                {
                    _edges = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        /// <summary>Drives the panel root's IsVisible — plays the role of vanilla tab VMs' Show.</summary>
        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (!string.Equals(value, _titleText, StringComparison.Ordinal))
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        /// <summary>
        /// Rebuilds the graph when this VM never built or campaign diplomacy changed since the
        /// last build. Any failure degrades to an empty web (AGENTS.md rule 6).
        /// </summary>
        public void RebuildIfNeeded()
        {
            if (_built && !RelationsDirtyBehavior.IsDirty)
            {
                return;
            }

            // Consume the flag before reading campaign state: events raised mid-rebuild re-dirty.
            RelationsDirtyBehavior.MarkClean();

            try
            {
                Rebuild();
            }
            catch
            {
                FinalizeNodeList(Nodes);
                Nodes = new MBBindingList<RelationNodeVM>();
                Edges = new MBBindingList<RelationEdgeVM>();
            }

            _built = true;
        }

        private void Rebuild()
        {
            var kingdoms = new List<Kingdom>();
            if (Campaign.Current is not null) // P-07: no campaign, empty web
            {
                foreach (var kingdom in Kingdom.All)
                {
                    if (kingdom is not null && !kingdom.IsEliminated)
                    {
                        kingdoms.Add(kingdom);
                    }
                }
            }

            var kingdomById = new Dictionary<string, Kingdom>(kingdoms.Count, StringComparer.Ordinal);
            var nodeIds = new List<string>(kingdoms.Count);
            foreach (var kingdom in kingdoms)
            {
                if (kingdomById.ContainsKey(kingdom.StringId))
                {
                    continue;
                }

                kingdomById.Add(kingdom.StringId, kingdom);
                nodeIds.Add(kingdom.StringId);
            }

            var edges = new List<RelationEdge>();
            foreach (var provider in _providers)
            {
                edges.AddRange(provider.GetEdges()); // never throws, per contract
            }

            // RelationGraph dedups edges and drops any edge whose endpoint is not a node.
            var graph = new RelationGraph(nodeIds, edges);
            var layout = GraphCanvas.Compute(graph, in CanvasSpec);

            var nodeVms = new MBBindingList<RelationNodeVM>();
            foreach (var node in layout.Nodes)
            {
                var kingdom = kingdomById[node.NodeId];
                nodeVms.Add(new RelationNodeVM(
                    node.NodeId,
                    kingdom.Name?.ToString() ?? node.NodeId,
                    CreateBannerVisual(kingdom),
                    (float)node.CenterX - NodeCenterOffsetX,
                    (float)node.CenterY - NodeCenterOffsetY));
            }

            var edgeVms = new MBBindingList<RelationEdgeVM>();
            foreach (var edge in layout.Edges)
            {
                edgeVms.Add(new RelationEdgeVM(
                    edge.Edge,
                    (float)edge.X1,
                    (float)edge.Y1,
                    (float)edge.X2,
                    (float)edge.Y2,
                    RelationPalette.ColorOf(edge.Edge.Kind),
                    EdgeThickness));
            }

            FinalizeNodeList(Nodes); // release the previous build's banner identifiers
            Nodes = nodeVms;
            Edges = edgeVms;
        }

        private static ImageIdentifierVM? CreateBannerVisual(Kingdom kingdom)
        {
            try
            {
                var banner = kingdom.Banner;
                return banner is null ? null : new BannerImageIdentifierVM(banner, nineGrid: false);
            }
            catch
            {
                return null; // medallion frame renders empty; name label still identifies the node
            }
        }

        private static void FinalizeNodeList(MBBindingList<RelationNodeVM>? nodes)
        {
            if (nodes is null)
            {
                return;
            }

            foreach (var node in nodes)
            {
                node.OnFinalize();
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            FinalizeNodeList(Nodes);
        }
    }
}
