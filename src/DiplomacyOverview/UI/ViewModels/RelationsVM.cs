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
        // The canvas is a fixed-size widget centered in the panel; its dimensions MUST stay in
        // sync with the <Constants> block of DiplomacyOverviewRelationsPanel.xml. Everything else
        // (circle radius, node/banner sizes, label visibility, edge trim/thickness) is computed
        // per rebuild by WebDensity from the live node count — real modded campaigns hold 80+
        // living kingdoms (docs/research/10 run 7, P-24) and fixed vanilla-count sizing fuses the
        // web into a solid ring.
        public const float CanvasWidth = 1400f;
        public const float CanvasHeight = 800f;

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
            catch (Exception ex)
            {
                Diagnostics.Note("web rebuild failed, showing empty web: " + ex);
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
                    // Not merely !IsEliminated: Kingdom.All carries mod-created zombie kingdoms
                    // (Separatism's per-clan personal kingdoms flooded the first #6 in-game pass
                    // with ~70 nodes — docs/research/10 run 6). See KingdomFilter.
                    if (KingdomFilter.IsParticipant(kingdom))
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

            // Size the web to the actual world, then project (docs/research/10 run 7).
            var density = WebDensity.Compute(nodeIds.Count, CanvasWidth, CanvasHeight);
            var spec = new GraphCanvasSpec(CanvasWidth, CanvasHeight, density.CircleRadius, density.TrimRadius);

            // RelationGraph dedups edges and drops any edge whose endpoint is not a node.
            var graph = new RelationGraph(nodeIds, edges);
            var layout = GraphCanvas.Compute(graph, in spec);

            var nodeVms = new MBBindingList<RelationNodeVM>();
            foreach (var node in layout.Nodes)
            {
                var kingdom = kingdomById[node.NodeId];
                nodeVms.Add(new RelationNodeVM(
                    node.NodeId,
                    kingdom.Name?.ToString() ?? node.NodeId,
                    CreateBannerVisual(kingdom),
                    (float)(node.CenterX - density.NodeCenterOffsetX),
                    (float)(node.CenterY - density.NodeCenterOffsetY),
                    (float)density.NodeBoxWidth,
                    (float)density.NodeBoxHeight,
                    (float)density.BannerWidth,
                    (float)density.BannerHeight,
                    density.ShowLabels));
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
                    (float)density.EdgeThickness));
            }

            FinalizeNodeList(Nodes); // release the previous build's banner identifiers
            Nodes = nodeVms;
            Edges = edgeVms;

            // One line per rebuild into rgl_log: the difference between "provider found no wars",
            // "graph dropped the edges", and "VM never rebuilt" is invisible on screen.
            Diagnostics.Note(
                "web rebuilt: " + nodeVms.Count + " kingdoms, " + edgeVms.Count + " war lines ("
                + edges.Count + " raw provider edges); node scale "
                + density.NodeScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                + ", labels " + (density.ShowLabels ? "on" : "off") + ".");
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
