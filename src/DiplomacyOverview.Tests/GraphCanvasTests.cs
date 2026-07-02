using System;
using System.Collections.Generic;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    /// <summary>
    /// Provider-to-VM mapping shape (issue #6): hand-built edges through RelationGraph +
    /// GraphCanvas must land on the expected design-space coordinates. This is exactly the data
    /// the game-side view models bind, minus the game types.
    /// </summary>
    public class GraphCanvasTests
    {
        private static readonly GraphCanvasSpec Spec =
            new GraphCanvasSpec(canvasWidth: 1000, canvasHeight: 1000, circleRadius: 400, nodeRadius: 50);

        private static RelationGraph Graph(IEnumerable<string> nodes, params RelationEdge[] edges)
            => new RelationGraph(nodes, edges);

        [Fact]
        public void Compute_NodesLandOnCanvasCenteredCircle_TwelveOClockFirstClockwise()
        {
            var layout = GraphCanvas.Compute(Graph(new[] { "n0", "n1", "n2", "n3" }), in Spec);

            Assert.Equal(4, layout.Nodes.Count);

            AssertNode(layout.Nodes[0], "n0", 500, 100); // 12 o'clock
            AssertNode(layout.Nodes[1], "n1", 900, 500); // 3 o'clock
            AssertNode(layout.Nodes[2], "n2", 500, 900); // 6 o'clock
            AssertNode(layout.Nodes[3], "n3", 100, 500); // 9 o'clock
        }

        [Fact]
        public void Compute_EdgeSegments_AreTrimmedByNodeRadiusOnBothEnds()
        {
            var war = RelationEdge.Create("n0", "n2", RelationKind.War);
            var layout = GraphCanvas.Compute(Graph(new[] { "n0", "n1", "n2", "n3" }, war), in Spec);

            var edge = Assert.Single(layout.Edges);

            // n0 (500,100) -> n2 (500,900), vertical, trimmed 50 from each medallion center.
            Assert.Equal(500, edge.X1, precision: 9);
            Assert.Equal(150, edge.Y1, precision: 9);
            Assert.Equal(500, edge.X2, precision: 9);
            Assert.Equal(850, edge.Y2, precision: 9);
        }

        [Fact]
        public void Compute_EdgeEndpointsFollowCanonicalNodeOrder_NotInsertionOrder()
        {
            // Created with swapped arguments; RelationEdge canonicalizes to (n1, n3), so the
            // segment must run from n1's position to n3's position.
            var war = RelationEdge.Create("n3", "n1", RelationKind.War);
            var layout = GraphCanvas.Compute(Graph(new[] { "n0", "n1", "n2", "n3" }, war), in Spec);

            var edge = Assert.Single(layout.Edges);

            // n1 (900,500) -> n3 (100,500), horizontal, trimmed 50 from each end.
            Assert.Equal(850, edge.X1, precision: 9);
            Assert.Equal(500, edge.Y1, precision: 9);
            Assert.Equal(150, edge.X2, precision: 9);
            Assert.Equal(500, edge.Y2, precision: 9);
        }

        [Fact]
        public void Compute_OverlappingMedallions_ClampTrimToSegmentMidpoint()
        {
            // Two nodes on a tiny circle: centers (500,460) and (500,540) are 80 apart — less
            // than 2 x nodeRadius (100). Naive trimming would cross the endpoints past each
            // other (issue #6 core-contract note); the clamp collapses the segment onto the
            // midpoint instead, which renderers then skip as zero-length.
            var spec = new GraphCanvasSpec(1000, 1000, circleRadius: 40, nodeRadius: 50);
            var war = RelationEdge.Create("a", "b", RelationKind.War);

            var layout = GraphCanvas.Compute(Graph(new[] { "a", "b" }, war), in spec);

            var edge = Assert.Single(layout.Edges);
            Assert.Equal(500, edge.X1, precision: 9);
            Assert.Equal(500, edge.Y1, precision: 9);
            Assert.Equal(500, edge.X2, precision: 9);
            Assert.Equal(500, edge.Y2, precision: 9);
        }

        [Fact]
        public void Compute_EdgeReferencingUnknownNode_WasDroppedByGraph_SoCanvasHasNoEdge()
        {
            var stray = RelationEdge.Create("n0", "ghost", RelationKind.War);
            var layout = GraphCanvas.Compute(Graph(new[] { "n0", "n1" }, stray), in Spec);

            Assert.Equal(2, layout.Nodes.Count);
            Assert.Empty(layout.Edges);
        }

        [Fact]
        public void Compute_DuplicateWarPair_CollapsesToSingleSegment()
        {
            // The war provider sees each war from both sides; the graph dedups the canonical pair.
            var ab = RelationEdge.Create("a", "b", RelationKind.War);
            var ba = RelationEdge.Create("b", "a", RelationKind.War);

            var layout = GraphCanvas.Compute(Graph(new[] { "a", "b" }, ab, ba), in Spec);

            Assert.Single(layout.Edges);
        }

        [Fact]
        public void Compute_CarriesSourceEdgeThrough_KindAndDetailsIntact()
        {
            var details = new Dictionary<string, string>
            {
                ["war.startDay"] = "1084",
                ["tribute.payer"] = "b",
            };
            var war = RelationEdge.Create("a", "b", RelationKind.War, details);

            var layout = GraphCanvas.Compute(Graph(new[] { "a", "b", "c" }, war), in Spec);

            var edge = Assert.Single(layout.Edges);
            Assert.Same(war, edge.Edge);
            Assert.Equal(RelationKind.War, edge.Edge.Kind);
            Assert.Equal("b", edge.Edge.Details!["tribute.payer"]);
        }

        [Fact]
        public void Compute_EmptyGraph_YieldsEmptyLayout()
        {
            var layout = GraphCanvas.Compute(Graph(Array.Empty<string>()), in Spec);

            Assert.Empty(layout.Nodes);
            Assert.Empty(layout.Edges);
        }

        [Fact]
        public void Compute_CalledTwice_IsDeterministic()
        {
            var edges = new[]
            {
                RelationEdge.Create("empire", "battania", RelationKind.War),
                RelationEdge.Create("sturgia", "vlandia", RelationKind.War),
            };
            var nodes = new[] { "empire", "battania", "sturgia", "vlandia", "khuzait" };

            var first = GraphCanvas.Compute(Graph(nodes, edges), in Spec);
            var second = GraphCanvas.Compute(Graph(nodes, edges), in Spec);

            Assert.Equal(first.Nodes.Count, second.Nodes.Count);
            for (var i = 0; i < first.Nodes.Count; i++)
            {
                Assert.Equal(first.Nodes[i].NodeId, second.Nodes[i].NodeId);
                Assert.Equal(first.Nodes[i].CenterX, second.Nodes[i].CenterX);
                Assert.Equal(first.Nodes[i].CenterY, second.Nodes[i].CenterY);
            }

            Assert.Equal(first.Edges.Count, second.Edges.Count);
            for (var i = 0; i < first.Edges.Count; i++)
            {
                Assert.Equal(first.Edges[i].X1, second.Edges[i].X1);
                Assert.Equal(first.Edges[i].Y1, second.Edges[i].Y1);
                Assert.Equal(first.Edges[i].X2, second.Edges[i].X2);
                Assert.Equal(first.Edges[i].Y2, second.Edges[i].Y2);
            }
        }

        [Fact]
        public void Compute_NullGraph_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GraphCanvas.Compute(null!, in Spec));
        }

        [Theory]
        [InlineData(0, 800, 300, 46)]
        [InlineData(1400, 0, 300, 46)]
        [InlineData(1400, 800, -1, 46)]
        [InlineData(1400, 800, 300, -1)]
        public void Spec_RejectsNonPositiveCanvasAndNegativeRadii(
            double width, double height, double circleRadius, double nodeRadius)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GraphCanvasSpec(width, height, circleRadius, nodeRadius));
        }

        private static void AssertNode(GraphCanvasNode node, string expectedId, double expectedX, double expectedY)
        {
            Assert.Equal(expectedId, node.NodeId);
            Assert.Equal(expectedX, node.CenterX, precision: 9);
            Assert.Equal(expectedY, node.CenterY, precision: 9);
        }
    }
}
