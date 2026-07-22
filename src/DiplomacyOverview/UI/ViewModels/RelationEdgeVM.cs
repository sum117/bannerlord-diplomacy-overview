using System;
using System.Collections.Generic;
using System.Globalization;
using DiplomacyOverview.Core;
using DiplomacyOverview.Providers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// One relation line on the web: trimmed endpoints plus the routing apex in canvas design
    /// pixels plus styling, bound 1:1 onto a <see cref="Widgets.DiplomacyOverviewEdgeWidget"/>
    /// (apex == segment midpoint ⇒ straight; pulled toward the circle center ⇒ bowed curve for
    /// short-range edges — see GraphCanvasEdge).
    ///
    /// The drawn line lives on a stretched full-canvas widget and can't be hovered per-line, so this
    /// VM also exposes an invisible <see cref="Hint"/>-carrying hit-strip (Hit* properties): a thin
    /// rectangle placed along the segment (EdgeGeometry), rotated in radians, PivotX/Y = 0.5. On
    /// hover it shows the vanilla rich tooltip (<see cref="BasicTooltipViewModel"/> →
    /// <c>List&lt;TooltipProperty&gt;</c>, the same engine behind every in-game tooltip), built
    /// lazily and degrade-safe from the source <see cref="RelationEdge.Details"/> (issue #10).
    /// </summary>
    internal sealed class RelationEdgeVM : ViewModel
    {
        /// <summary>Hover hit-strip thickness, design px — a comfortable band around the thin line.</summary>
        private const float HitThickness = 22f;

        private readonly float _x1;
        private readonly float _y1;
        private readonly float _x2;
        private readonly float _y2;
        private readonly float _apexX;
        private readonly float _apexY;
        private readonly string _lineColor;
        private readonly float _lineThickness;

        private readonly string _nodeAName;
        private readonly string _nodeBName;

        private readonly float _hitX;
        private readonly float _hitY;
        private readonly float _hitWidth;
        private readonly float _hitRotation;

        public RelationEdgeVM(
            RelationEdge edge,
            string nodeAName,
            string nodeBName,
            float x1,
            float y1,
            float x2,
            float y2,
            float apexX,
            float apexY,
            string lineColor,
            float lineThickness)
        {
            Edge = edge;
            _nodeAName = nodeAName;
            _nodeBName = nodeBName;
            _x1 = x1;
            _y1 = y1;
            _x2 = x2;
            _y2 = y2;
            _apexX = apexX;
            _apexY = apexY;
            _lineColor = lineColor;
            _lineThickness = lineThickness;

            var strip = EdgeGeometry.Compute(x1, y1, x2, y2);
            _hitWidth = (float)strip.Length;
            _hitX = (float)strip.TopLeftX(HitThickness);
            _hitY = (float)strip.TopLeftY(HitThickness);
            _hitRotation = (float)strip.AngleRadians;

            Hint = new BasicTooltipViewModel(BuildTooltip);
        }

        /// <summary>The underlying graph edge (kind + tooltip Details) — not bound to XML.</summary>
        public RelationEdge Edge { get; }

        [DataSourceProperty]
        public float X1 => _x1;

        [DataSourceProperty]
        public float Y1 => _y1;

        [DataSourceProperty]
        public float X2 => _x2;

        [DataSourceProperty]
        public float Y2 => _y2;

        [DataSourceProperty]
        public float ApexX => _apexX;

        [DataSourceProperty]
        public float ApexY => _apexY;

        /// <summary>"#RRGGBBAA", parsed widget-side via Color.ConvertStringToColor.</summary>
        [DataSourceProperty]
        public string LineColor => _lineColor;

        /// <summary>Design pixels.</summary>
        [DataSourceProperty]
        public float LineThickness => _lineThickness;

        // ---- Hover hit-strip (design px; rotation radians) --------------------------------------

        [DataSourceProperty]
        public float HitX => _hitX;

        [DataSourceProperty]
        public float HitY => _hitY;

        [DataSourceProperty]
        public float HitWidth => _hitWidth;

        [DataSourceProperty]
        public float HitHeight => HitThickness;

        [DataSourceProperty]
        public float HitRotation => _hitRotation;

        /// <summary>Vanilla rich tooltip for this edge, built on hover.</summary>
        [DataSourceProperty]
        public BasicTooltipViewModel Hint { get; }

        /// <summary>
        /// Builds the tooltip property list (title + kind + kind-specific rows) from the edge's
        /// captured <see cref="RelationEdge.Details"/>. Exception-contained: a malformed detail
        /// costs a row, never the tooltip or the frame (AGENTS.md rule 6).
        /// </summary>
        private List<TooltipProperty> BuildTooltip()
        {
            var list = new List<TooltipProperty>();
            try
            {
                var title = new TextObject("{=DipOvEdgeTitle}{A}  —  {B}")
                    .SetTextVariable("A", _nodeAName)
                    .SetTextVariable("B", _nodeBName)
                    .ToString();
                list.Add(new TooltipProperty(title, string.Empty, 0, onlyShowWhenExtended: false,
                    TooltipProperty.TooltipPropertyFlags.Title));
                list.Add(new TooltipProperty(RelationKindText.Label(Edge.Kind), string.Empty, 0));

                switch (Edge.Kind)
                {
                    case RelationKind.War:
                        AppendWarRows(list);
                        break;
                    case RelationKind.Alliance:
                        AppendExpiryRow(list, AllianceProvider.AllianceEndDayKey);
                        break;
                    case RelationKind.TradeAgreement:
                        AppendTradeRows(list);
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Note("edge tooltip build failed: " + ex.Message);
            }

            return list;
        }

        private void AppendWarRows(List<TooltipProperty> list)
        {
            AppendCasualtyRow(list, Edge.NodeA, _nodeAName);
            AppendCasualtyRow(list, Edge.NodeB, _nodeBName);

            var details = Edge.Details;
            if (details != null
                && details.TryGetValue(WarProvider.TributePayerKey, out var payerId)
                && details.TryGetValue(WarProvider.TributeDailyAmountKey, out var amount))
            {
                var payerName = string.Equals(payerId, Edge.NodeA, StringComparison.Ordinal)
                    ? _nodeAName
                    : _nodeBName;
                var label = new TextObject("{=DipOvTipTribute}Tribute").ToString();
                var value = new TextObject("{=DipOvTipTributeVal}{P} pays {A} / day")
                    .SetTextVariable("P", payerName)
                    .SetTextVariable("A", amount)
                    .ToString();
                list.Add(new TooltipProperty(label, value, 0));
            }
        }

        private void AppendCasualtyRow(List<TooltipProperty> list, string nodeId, string name)
        {
            var details = Edge.Details;
            if (details != null && details.TryGetValue(WarProvider.CasualtiesKeyPrefix + nodeId, out var casualties))
            {
                var label = new TextObject("{=DipOvTipLosses}{F} losses").SetTextVariable("F", name).ToString();
                list.Add(new TooltipProperty(label, casualties, 0));
            }
        }

        private void AppendTradeRows(List<TooltipProperty> list)
        {
            AppendExpiryRow(list, TradeAgreementProvider.EndDayKey);
            AppendGoldRow(list, Edge.NodeA, _nodeAName);
            AppendGoldRow(list, Edge.NodeB, _nodeBName);
        }

        private void AppendGoldRow(List<TooltipProperty> list, string nodeId, string name)
        {
            var details = Edge.Details;
            if (details != null && details.TryGetValue(TradeAgreementProvider.GoldTotalKeyPrefix + nodeId, out var gold))
            {
                var label = new TextObject("{=DipOvTipEarned}{F} earned").SetTextVariable("F", name).ToString();
                list.Add(new TooltipProperty(label, gold, 0));
            }
        }

        private void AppendExpiryRow(List<TooltipProperty> list, string endDayKey)
        {
            var details = Edge.Details;
            if (details == null || !details.TryGetValue(endDayKey, out var endStr))
            {
                return;
            }

            if (!double.TryParse(endStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var endDay))
            {
                return;
            }

            var remaining = 0;
            try
            {
                remaining = Math.Max(0, (int)Math.Round(endDay - CampaignTime.Now.ToDays)); // P-07: hover is in-campaign
            }
            catch
            {
                // No campaign clock — fall back to just omitting the countdown value below is fine.
            }

            var label = new TextObject("{=DipOvTipEnds}Ends in").ToString();
            var value = new TextObject("{=DipOvTipDays}{N} days").SetTextVariable("N", remaining).ToString();
            list.Add(new TooltipProperty(label, value, 0));
        }
    }
}
