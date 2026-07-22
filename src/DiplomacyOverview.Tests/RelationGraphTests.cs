using System.Linq;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    public class RelationGraphTests
    {
        [Fact]
        public void Constructor_PreservesNodeOrder()
        {
            var nodes = new[] { "C", "A", "B" };

            var graph = new RelationGraph(nodes, Enumerable.Empty<RelationEdge>());

            Assert.Equal(nodes, graph.Nodes);
        }

        [Fact]
        public void Constructor_SamePairSameKindBothOrders_DedupsToOneEdge()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[]
            {
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("B", "A", RelationKind.War),
            };

            var graph = new RelationGraph(nodes, edges);

            var edge = Assert.Single(graph.Edges);
            Assert.Equal(RelationKind.War, edge.Kind);
        }

        [Fact]
        public void Constructor_SamePairDifferentKinds_KeepsBothAsDistinctEdges()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[]
            {
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("A", "B", RelationKind.Alliance),
            };

            var graph = new RelationGraph(nodes, edges);

            Assert.Equal(2, graph.Edges.Count);
            Assert.Contains(graph.Edges, e => e.Kind == RelationKind.War);
            Assert.Contains(graph.Edges, e => e.Kind == RelationKind.Alliance);
        }

        [Fact]
        public void Constructor_DuplicateSubmittedThreeTimes_StillDedupsToOne()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[]
            {
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("B", "A", RelationKind.War),
            };

            var graph = new RelationGraph(nodes, edges);

            Assert.Single(graph.Edges);
        }

        [Fact]
        public void Constructor_EdgeWithEndpointOutsideNodeSet_IsSilentlyDropped()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[]
            {
                RelationEdge.Create("A", "Ghost", RelationKind.War),
                RelationEdge.Create("A", "B", RelationKind.Alliance),
            };

            var graph = new RelationGraph(nodes, edges);

            var edge = Assert.Single(graph.Edges);
            Assert.Equal(RelationKind.Alliance, edge.Kind);
        }

        [Fact]
        public void Constructor_EdgeWithBothEndpointsOutsideNodeSet_IsSilentlyDropped()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[]
            {
                RelationEdge.Create("Ghost1", "Ghost2", RelationKind.War),
            };

            var graph = new RelationGraph(nodes, edges);

            Assert.Empty(graph.Edges);
        }

        [Fact]
        public void Filter_SingleFlagMask_ReturnsOnlyMatchingEdges()
        {
            var nodes = new[] { "A", "B", "C" };
            var edges = new[]
            {
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("B", "C", RelationKind.Alliance),
            };
            var graph = new RelationGraph(nodes, edges);

            var filtered = graph.Filter(RelationKind.War);

            var edge = Assert.Single(filtered);
            Assert.Equal(RelationKind.War, edge.Kind);
        }

        [Fact]
        public void Filter_CombinedMask_ReturnsUnionOfMatchingEdges()
        {
            var nodes = new[] { "A", "B", "C" };
            var edges = new[]
            {
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("B", "C", RelationKind.Alliance),
                RelationEdge.Create("A", "C", RelationKind.NonAggressionPact),
            };
            var graph = new RelationGraph(nodes, edges);

            var filtered = graph.Filter(RelationKind.War | RelationKind.Alliance);

            Assert.Equal(2, filtered.Count);
            Assert.Contains(filtered, e => e.Kind == RelationKind.War);
            Assert.Contains(filtered, e => e.Kind == RelationKind.Alliance);
            Assert.DoesNotContain(filtered, e => e.Kind == RelationKind.NonAggressionPact);
        }

        [Fact]
        public void Filter_TradeAgreementMask_ReturnsOnlyTradeEdges()
        {
            var nodes = new[] { "A", "B", "C" };
            var edges = new[]
            {
                RelationEdge.Create("A", "B", RelationKind.War),
                RelationEdge.Create("A", "B", RelationKind.TradeAgreement), // same pair, distinct kind survives dedup
                RelationEdge.Create("C", "B", RelationKind.TradeAgreement), // swapped-arg canonicalization
            };
            var graph = new RelationGraph(nodes, edges);

            var filtered = graph.Filter(RelationKind.TradeAgreement);

            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, e => Assert.Equal(RelationKind.TradeAgreement, e.Kind));
        }

        [Fact]
        public void Filter_NoneMask_ReturnsEmpty()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[] { RelationEdge.Create("A", "B", RelationKind.War) };
            var graph = new RelationGraph(nodes, edges);

            var filtered = graph.Filter(RelationKind.None);

            Assert.Empty(filtered);
        }

        [Fact]
        public void Filter_DoesNotMutateNodes()
        {
            var nodes = new[] { "A", "B" };
            var edges = new[] { RelationEdge.Create("A", "B", RelationKind.War) };
            var graph = new RelationGraph(nodes, edges);

            graph.Filter(RelationKind.None);

            Assert.Equal(nodes, graph.Nodes);
        }
    }
}
