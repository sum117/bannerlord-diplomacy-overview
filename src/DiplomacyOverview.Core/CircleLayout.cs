using System;
using System.Collections.Generic;

namespace DiplomacyOverview.Core
{
    /// <summary>A 2D point in canvas space.</summary>
    public readonly struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>A 2D vector, used here for outward-facing unit directions.</summary>
    public readonly struct Vector2D
    {
        public double X { get; }
        public double Y { get; }

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// The computed position and label placement hint for a single node on the circle.
    /// </summary>
    public readonly struct NodeLayout
    {
        /// <summary>The node id, echoed from the input ordering.</summary>
        public string NodeId { get; }

        /// <summary>The node's center position on the circle.</summary>
        public Point2D Position { get; }

        /// <summary>
        /// Angle in degrees, measured clockwise from 12 o'clock (0 = 12 o'clock, 90 = 3 o'clock,
        /// 180 = 6 o'clock, 270 = 9 o'clock), normalized to [0, 360).
        /// </summary>
        public double AngleDegrees { get; }

        /// <summary>Unit vector pointing from the circle's center through <see cref="Position"/> — the
        /// direction a label should be pushed outward, away from the circle.</summary>
        public Vector2D LabelDirection { get; }

        public NodeLayout(string nodeId, Point2D position, double angleDegrees, Vector2D labelDirection)
        {
            NodeId = nodeId;
            Position = position;
            AngleDegrees = angleDegrees;
            LabelDirection = labelDirection;
        }
    }

    /// <summary>
    /// Pure, deterministic circle-layout math: positions nodes evenly around a circle starting at
    /// 12 o'clock and proceeding clockwise, plus a helper to trim an edge segment so it starts/ends
    /// at node (medallion) boundaries instead of centers.
    /// </summary>
    /// <remarks>
    /// Coordinate convention: Y increases downward (standard screen/canvas space), matching Gauntlet
    /// UI. "Clockwise starting at 12 o'clock" is defined relative to that convention: the first node
    /// sits directly above the center, and subsequent nodes proceed toward the point directly to the
    /// right of center before continuing down and around.
    /// </remarks>
    public static class CircleLayout
    {
        /// <summary>
        /// Computes one <see cref="NodeLayout"/> per input node, evenly spaced around a circle of the
        /// given <paramref name="radius"/> centered at <paramref name="center"/>. The first node (index
        /// 0) is placed at 12 o'clock; subsequent nodes proceed clockwise. Calling this twice with the
        /// same arguments yields bit-identical results (deterministic; no hidden state, no randomness).
        /// </summary>
        /// <param name="nodes">Ordered node ids. An empty list yields an empty result. A single node is
        /// placed at 12 o'clock.</param>
        /// <param name="radius">Circle radius. May be zero (all nodes collapse onto <paramref name="center"/>);
        /// must not be negative.</param>
        /// <param name="center">Circle center.</param>
        public static IReadOnlyList<NodeLayout> Compute(IReadOnlyList<string> nodes, double radius, Point2D center)
        {
            if (nodes is null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must not be negative.");
            }

            var count = nodes.Count;
            var result = new NodeLayout[count];

            for (var i = 0; i < count; i++)
            {
                var angleDegrees = count == 0 ? 0.0 : NormalizeDegrees(i * (360.0 / count));
                var (position, direction) = PositionAt(center, radius, angleDegrees);
                result[i] = new NodeLayout(nodes[i], position, angleDegrees, direction);
            }

            return result;
        }

        /// <summary>
        /// Trims the segment between two node centers so it starts and ends at the boundary of each
        /// node's medallion (a circle of <paramref name="nodeRadius"/>) instead of at the centers,
        /// preserving the original A-to-B direction. If the centers coincide (zero-length segment,
        /// no defined direction), the segment is returned unchanged.
        /// </summary>
        public static (Point2D Start, Point2D End) TrimToNodeEdges(Point2D a, Point2D b, double nodeRadius)
        {
            if (nodeRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeRadius), nodeRadius, "Node radius must not be negative.");
            }

            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length <= double.Epsilon)
            {
                return (a, b);
            }

            var ux = dx / length;
            var uy = dy / length;

            var start = new Point2D(a.X + ux * nodeRadius, a.Y + uy * nodeRadius);
            var end = new Point2D(b.X - ux * nodeRadius, b.Y - uy * nodeRadius);

            return (start, end);
        }

        private static (Point2D Position, Vector2D Direction) PositionAt(Point2D center, double radius, double angleDegrees)
        {
            var radians = angleDegrees * Math.PI / 180.0;

            // Clockwise from 12 o'clock, Y-down screen space: 0 deg -> straight up, 90 deg -> right.
            var ux = Math.Sin(radians);
            var uy = -Math.Cos(radians);

            var position = new Point2D(center.X + radius * ux, center.Y + radius * uy);
            var direction = new Vector2D(ux, uy);

            return (position, direction);
        }

        private static double NormalizeDegrees(double degrees)
        {
            var normalized = degrees % 360.0;
            return normalized < 0 ? normalized + 360.0 : normalized;
        }
    }
}
