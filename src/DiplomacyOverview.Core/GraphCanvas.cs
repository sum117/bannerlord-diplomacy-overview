using System;
using System.Collections.Generic;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// The fixed design-space canvas a relation graph is projected onto: canvas dimensions in
    /// design pixels, the radius of the circle the nodes sit on (centered on the canvas), and the
    /// node (medallion) radius used to trim edge segments back from node centers.
    /// </summary>
    public readonly struct GraphCanvasSpec
    {
        public double CanvasWidth { get; }
        public double CanvasHeight { get; }
        public double CircleRadius { get; }
        public double NodeRadius { get; }

        public GraphCanvasSpec(double canvasWidth, double canvasHeight, double circleRadius, double nodeRadius)
        {
            if (canvasWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(canvasWidth), canvasWidth, "Canvas width must be positive.");
            }

            if (canvasHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(canvasHeight), canvasHeight, "Canvas height must be positive.");
            }

            if (circleRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(circleRadius), circleRadius, "Circle radius must not be negative.");
            }

            if (nodeRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeRadius), nodeRadius, "Node radius must not be negative.");
            }

            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
            CircleRadius = circleRadius;
            NodeRadius = nodeRadius;
        }
    }

    /// <summary>A node's medallion-center position on the canvas, in design pixels.</summary>
    public readonly struct GraphCanvasNode
    {
        public string NodeId { get; }
        public double CenterX { get; }
        public double CenterY { get; }

        public GraphCanvasNode(string nodeId, double centerX, double centerY)
        {
            NodeId = nodeId;
            CenterX = centerX;
            CenterY = centerY;
        }
    }

    /// <summary>
    /// An edge segment on the canvas, trimmed back from both node centers so it starts and ends at
    /// medallion boundaries. Carries the source <see cref="RelationEdge"/> so callers can derive
    /// styling (kind, color) and tooltip payloads without re-joining.
    /// </summary>
    public readonly struct GraphCanvasEdge
    {
        public RelationEdge Edge { get; }
        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }

        public GraphCanvasEdge(RelationEdge edge, double x1, double y1, double x2, double y2)
        {
            Edge = edge;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }

    /// <summary>The full canvas projection of a graph: node centers plus trimmed edge segments.</summary>
    public sealed class GraphCanvasLayout
    {
        public IReadOnlyList<GraphCanvasNode> Nodes { get; }
        public IReadOnlyList<GraphCanvasEdge> Edges { get; }

        public GraphCanvasLayout(IReadOnlyList<GraphCanvasNode> nodes, IReadOnlyList<GraphCanvasEdge> edges)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        }
    }

    /// <summary>
    /// Pure, deterministic projection of a <see cref="RelationGraph"/> onto a fixed design-space
    /// canvas: nodes are spread on a circle centered on the canvas (12 o'clock first, clockwise —
    /// see <see cref="CircleLayout"/>), edges become straight segments between node centers,
    /// trimmed back by the node radius on both ends. This is the whole "graph to VM coordinates"
    /// step, kept free of game types so it is unit-testable.
    /// </summary>
    public static class GraphCanvas
    {
        public static GraphCanvasLayout Compute(RelationGraph graph, in GraphCanvasSpec spec)
        {
            if (graph is null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var center = new Point2D(spec.CanvasWidth / 2.0, spec.CanvasHeight / 2.0);
            var placed = CircleLayout.Compute(graph.Nodes, spec.CircleRadius, center);

            var nodes = new GraphCanvasNode[placed.Count];
            var positionById = new Dictionary<string, Point2D>(placed.Count, StringComparer.Ordinal);
            for (var i = 0; i < placed.Count; i++)
            {
                var layout = placed[i];
                nodes[i] = new GraphCanvasNode(layout.NodeId, layout.Position.X, layout.Position.Y);
                positionById[layout.NodeId] = layout.Position;
            }

            var edges = new GraphCanvasEdge[graph.Edges.Count];
            for (var i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];

                // RelationGraph guarantees both endpoints are members of the node set.
                var a = positionById[edge.NodeA];
                var b = positionById[edge.NodeB];

                // Clamp the trim to the segment midpoint: when two medallions sit closer than
                // 2 x NodeRadius, a full trim from each end would cross the endpoints past each
                // other (issue #6 core-contract note). Clamped, the segment degenerates to a
                // zero-length midpoint pair, which renderers skip — the line is fully hidden
                // behind the overlapping medallions anyway.
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var trim = Math.Min(spec.NodeRadius, distance / 2.0);

                var (start, end) = CircleLayout.TrimToNodeEdges(a, b, trim);
                edges[i] = new GraphCanvasEdge(edge, start.X, start.Y, end.X, end.Y);
            }

            return new GraphCanvasLayout(nodes, edges);
        }
    }
}
