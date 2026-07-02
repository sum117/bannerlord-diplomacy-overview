using DiplomacyOverview.Core;
using TaleWorlds.Library;

namespace DiplomacyOverview.UI.ViewModels
{
    /// <summary>
    /// One relation line on the web: trimmed endpoints in canvas design pixels plus styling,
    /// bound 1:1 onto a <see cref="Widgets.DiplomacyOverviewEdgeWidget"/>. Keeps the source
    /// <see cref="RelationEdge"/> (with its Details payload) for the future tooltip pass.
    /// </summary>
    internal sealed class RelationEdgeVM : ViewModel
    {
        private readonly float _x1;
        private readonly float _y1;
        private readonly float _x2;
        private readonly float _y2;
        private readonly string _lineColor;
        private readonly float _lineThickness;

        public RelationEdgeVM(RelationEdge edge, float x1, float y1, float x2, float y2, string lineColor, float lineThickness)
        {
            Edge = edge;
            _x1 = x1;
            _y1 = y1;
            _x2 = x2;
            _y2 = y2;
            _lineColor = lineColor;
            _lineThickness = lineThickness;
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

        /// <summary>"#RRGGBBAA", parsed widget-side via Color.ConvertStringToColor.</summary>
        [DataSourceProperty]
        public string LineColor => _lineColor;

        /// <summary>Design pixels.</summary>
        [DataSourceProperty]
        public float LineThickness => _lineThickness;
    }
}
