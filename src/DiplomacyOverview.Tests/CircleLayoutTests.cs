using System;
using System.Collections.Generic;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    public class CircleLayoutTests
    {
        private const double Tolerance = 1e-9;

        private static readonly Point2D Origin = new Point2D(0, 0);

        [Fact]
        public void Compute_EmptyNodeList_ReturnsEmpty()
        {
            var result = CircleLayout.Compute(Array.Empty<string>(), radius: 100, center: Origin);

            Assert.Empty(result);
        }

        [Fact]
        public void Compute_SingleNode_IsPlacedAtTwelveOClock()
        {
            var result = CircleLayout.Compute(new[] { "A" }, radius: 100, center: Origin);

            var node = Assert.Single(result);
            Assert.Equal(0, node.Position.X, precision: 9);
            Assert.Equal(-100, node.Position.Y, precision: 9);
            Assert.Equal(0, node.AngleDegrees, precision: 9);
        }

        [Fact]
        public void Compute_TwoNodes_AreOppositeEachOtherStartingAtTwelveOClock()
        {
            var result = CircleLayout.Compute(new[] { "A", "B" }, radius: 100, center: Origin);

            Assert.Equal(2, result.Count);

            // First node at 12 o'clock (straight up).
            AssertPoint(new Point2D(0, -100), result[0].Position);
            Assert.Equal(0, result[0].AngleDegrees, precision: 9);

            // Second node 180 degrees around -> 6 o'clock (straight down).
            AssertPoint(new Point2D(0, 100), result[1].Position);
            Assert.Equal(180, result[1].AngleDegrees, precision: 9);
        }

        [Fact]
        public void Compute_FourNodes_AreEvenlySpacedNinetyDegreesApartClockwise()
        {
            var result = CircleLayout.Compute(new[] { "A", "B", "C", "D" }, radius: 10, center: Origin);

            Assert.Equal(4, result.Count);

            // 12 o'clock
            AssertPoint(new Point2D(0, -10), result[0].Position);
            // 3 o'clock (clockwise from 12)
            AssertPoint(new Point2D(10, 0), result[1].Position);
            // 6 o'clock
            AssertPoint(new Point2D(0, 10), result[2].Position);
            // 9 o'clock
            AssertPoint(new Point2D(-10, 0), result[3].Position);

            var expectedAngles = new[] { 0.0, 90.0, 180.0, 270.0 };
            var actualAngles = Array2(result);
            for (var i = 0; i < expectedAngles.Length; i++)
            {
                Assert.Equal(expectedAngles[i], actualAngles[i], precision: 9);
            }
        }

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(12)]
        [InlineData(93)] // realistic RoT kingdom+clan scale ceiling from doc 07
        public void Compute_NNodes_AreEvenlySpacedByThreeSixtyOverN(int n)
        {
            var nodes = new string[n];
            for (var i = 0; i < n; i++)
            {
                nodes[i] = $"Node{i}";
            }

            var result = CircleLayout.Compute(nodes, radius: 50, center: Origin);

            var expectedStep = 360.0 / n;
            for (var i = 0; i < n; i++)
            {
                var expectedAngle = (i * expectedStep) % 360.0;
                Assert.Equal(expectedAngle, result[i].AngleDegrees, precision: 9);
            }
        }

        [Fact]
        public void Compute_AllNodesLieOnTheCircle_AtExactlyTheGivenRadius()
        {
            var nodes = new[] { "A", "B", "C", "D", "E", "F", "G" };
            var center = new Point2D(37, -12);
            const double radius = 64;

            var result = CircleLayout.Compute(nodes, radius, center);

            foreach (var node in result)
            {
                var dx = node.Position.X - center.X;
                var dy = node.Position.Y - center.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                Assert.Equal(radius, distance, precision: 9);
            }
        }

        [Fact]
        public void Compute_LabelDirection_IsUnitVectorPointingFromCenterThroughNode()
        {
            var nodes = new[] { "A", "B", "C", "D" };
            var center = new Point2D(5, 5);
            const double radius = 20;

            var result = CircleLayout.Compute(nodes, radius, center);

            foreach (var node in result)
            {
                var length = Math.Sqrt(node.LabelDirection.X * node.LabelDirection.X + node.LabelDirection.Y * node.LabelDirection.Y);
                Assert.Equal(1.0, length, precision: 9);

                // Direction, scaled by radius and added to center, must reproduce the node position.
                var reconstructedX = center.X + node.LabelDirection.X * radius;
                var reconstructedY = center.Y + node.LabelDirection.Y * radius;
                Assert.Equal(node.Position.X, reconstructedX, precision: 9);
                Assert.Equal(node.Position.Y, reconstructedY, precision: 9);
            }
        }

        [Fact]
        public void Compute_CalledTwiceWithSameArguments_ProducesIdenticalResults()
        {
            var nodes = new[] { "Vlandia", "Sturgia", "Aserai", "Khuzait", "Battania" };
            var center = new Point2D(100, 200);
            const double radius = 250;

            var first = CircleLayout.Compute(nodes, radius, center);
            var second = CircleLayout.Compute(nodes, radius, center);

            Assert.Equal(first.Count, second.Count);
            for (var i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i].NodeId, second[i].NodeId);
                Assert.Equal(first[i].Position.X, second[i].Position.X);
                Assert.Equal(first[i].Position.Y, second[i].Position.Y);
                Assert.Equal(first[i].AngleDegrees, second[i].AngleDegrees);
                Assert.Equal(first[i].LabelDirection.X, second[i].LabelDirection.X);
                Assert.Equal(first[i].LabelDirection.Y, second[i].LabelDirection.Y);
            }
        }

        [Fact]
        public void Compute_ZeroRadius_CollapsesAllNodesOntoCenter()
        {
            var nodes = new[] { "A", "B", "C" };
            var center = new Point2D(9, -4);

            var result = CircleLayout.Compute(nodes, radius: 0, center: center);

            foreach (var node in result)
            {
                AssertPoint(center, node.Position);
            }
        }

        [Fact]
        public void Compute_NegativeRadius_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CircleLayout.Compute(new[] { "A" }, radius: -1, center: Origin));
        }

        [Fact]
        public void Compute_NullNodes_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                CircleLayout.Compute(null!, radius: 10, center: Origin));
        }

        [Fact]
        public void TrimToNodeEdges_EndpointsAreExactlyNodeRadiusFromCenters()
        {
            var a = new Point2D(0, 0);
            var b = new Point2D(100, 0);
            const double nodeRadius = 15;

            var (start, end) = CircleLayout.TrimToNodeEdges(a, b, nodeRadius);

            var distFromA = Distance(a, start);
            var distFromB = Distance(b, end);

            Assert.Equal(nodeRadius, distFromA, precision: 9);
            Assert.Equal(nodeRadius, distFromB, precision: 9);
        }

        [Fact]
        public void TrimToNodeEdges_PreservesOriginalDirection()
        {
            var a = new Point2D(10, 20);
            var b = new Point2D(310, 420);
            const double nodeRadius = 12;

            var (start, end) = CircleLayout.TrimToNodeEdges(a, b, nodeRadius);

            var originalDx = b.X - a.X;
            var originalDy = b.Y - a.Y;
            var originalLength = Math.Sqrt(originalDx * originalDx + originalDy * originalDy);

            var trimmedDx = end.X - start.X;
            var trimmedDy = end.Y - start.Y;
            var trimmedLength = Math.Sqrt(trimmedDx * trimmedDx + trimmedDy * trimmedDy);

            // Normalized directions must match (same unit vector).
            Assert.Equal(originalDx / originalLength, trimmedDx / trimmedLength, precision: 9);
            Assert.Equal(originalDy / originalLength, trimmedDy / trimmedLength, precision: 9);

            // Trimmed segment is shorter by exactly two node radii.
            Assert.Equal(originalLength - 2 * nodeRadius, trimmedLength, precision: 9);
        }

        [Fact]
        public void TrimToNodeEdges_DiagonalSegment_TrimsCorrectlyOnBothAxes()
        {
            // 3-4-5 triangle for exact arithmetic: direction (3,4)/5.
            var a = new Point2D(0, 0);
            var b = new Point2D(30, 40);
            const double nodeRadius = 5;

            var (start, end) = CircleLayout.TrimToNodeEdges(a, b, nodeRadius);

            AssertPoint(new Point2D(3, 4), start);
            AssertPoint(new Point2D(27, 36), end);
        }

        [Fact]
        public void TrimToNodeEdges_ZeroNodeRadius_ReturnsOriginalCenters()
        {
            var a = new Point2D(1, 2);
            var b = new Point2D(50, 60);

            var (start, end) = CircleLayout.TrimToNodeEdges(a, b, nodeRadius: 0);

            AssertPoint(a, start);
            AssertPoint(b, end);
        }

        [Fact]
        public void TrimToNodeEdges_CoincidentCenters_ReturnsSegmentUnchanged()
        {
            var a = new Point2D(5, 5);
            var b = new Point2D(5, 5);

            var (start, end) = CircleLayout.TrimToNodeEdges(a, b, nodeRadius: 10);

            AssertPoint(a, start);
            AssertPoint(b, end);
        }

        [Fact]
        public void TrimToNodeEdges_NegativeNodeRadius_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CircleLayout.TrimToNodeEdges(Origin, new Point2D(10, 10), nodeRadius: -1));
        }

        private static void AssertPoint(Point2D expected, Point2D actual)
        {
            Assert.Equal(expected.X, actual.X, precision: 9);
            Assert.Equal(expected.Y, actual.Y, precision: 9);
        }

        private static double Distance(Point2D a, Point2D b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double[] Array2(IReadOnlyList<NodeLayout> nodes)
        {
            var angles = new double[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
            {
                angles[i] = nodes[i].AngleDegrees;
            }

            return angles;
        }
    }
}
