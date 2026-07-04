using System;
using System.Collections.Generic;
using System.Linq;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    /// <summary>
    /// Node ordering (issue #6 follow-up): related factions must be pushed to opposite sides of the
    /// ring so their relation line is a long chord instead of a stub buried under adjacent
    /// medallions (docs/research/10 runs 7–8). These lock the antipodal-matching contract and its
    /// degenerate cases; the arrangement stays a pure permutation of the input.
    /// </summary>
    public class CircularArrangementTests
    {
        private static RelationEdge War(string a, string b) => RelationEdge.Create(a, b, RelationKind.War);

        private static int IndexOf(IReadOnlyList<string> order, string id)
        {
            for (var i = 0; i < order.Count; i++)
            {
                if (order[i] == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Slots apart the short way round the ring — the antipode is count/2 either way.</summary>
        private static int CircularDistance(IReadOnlyList<string> order, string a, string b)
        {
            var diff = Math.Abs(IndexOf(order, a) - IndexOf(order, b));
            return Math.Min(diff, order.Count - diff);
        }

        private static void AssertPermutationOf(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(expected.OrderBy(s => s, StringComparer.Ordinal), actual.OrderBy(s => s, StringComparer.Ordinal));
        }

        [Fact]
        public void Arrange_SingleWar_SeatsThePairAntipodally()
        {
            var nodes = new[] { "a", "b", "c", "d", "e", "f" };

            var order = CircularArrangement.Arrange(nodes, new[] { War("a", "b") });

            AssertPermutationOf(nodes, order);
            Assert.Equal(order.Count / 2, CircularDistance(order, "a", "b")); // exactly opposite
        }

        [Fact]
        public void Arrange_MirrorsTheReferenceWorld_EveryAdjacentWarBecomesADiameter()
        {
            // docs/research/10 run 8: 82 living kingdoms, 5 wars, each between kingdoms adjacent in
            // creation order (rebel vs parent). Raw order buries all five under the medallions; the
            // arrangement must lift every one onto a full diameter (half the ring apart).
            var nodes = new List<string>();
            for (var i = 0; i < 82; i++)
            {
                nodes.Add("k" + i.ToString("00"));
            }

            var wars = new[]
            {
                War("k00", "k01"),
                War("k02", "k03"),
                War("k04", "k05"),
                War("k06", "k07"),
                War("k08", "k09"),
            };

            var order = CircularArrangement.Arrange(nodes, wars);

            AssertPermutationOf(nodes, order);
            foreach (var war in wars)
            {
                Assert.Equal(41, CircularDistance(order, war.NodeA, war.NodeB)); // 82 / 2
            }
        }

        [Fact]
        public void Arrange_TwoDisjointWars_SpreadsThePairsInsteadOfBundlingThem()
        {
            var nodes = new[] { "a", "b", "c", "d", "e", "f", "g", "h" };

            var order = CircularArrangement.Arrange(nodes, new[] { War("a", "b"), War("c", "d") });

            AssertPermutationOf(nodes, order);
            Assert.Equal(4, CircularDistance(order, "a", "b")); // each pair a full diameter
            Assert.Equal(4, CircularDistance(order, "c", "d"));

            // The two diameters are rotated apart, not stacked on the same axis.
            Assert.NotEqual(IndexOf(order, "a"), IndexOf(order, "c"));
            Assert.NotEqual(IndexOf(order, "a"), IndexOf(order, "d"));
        }

        [Fact]
        public void Arrange_SiblingSharesANode_OneWarGetsTheDiameter_TheOtherStillPlacesEveryNode()
        {
            // "a" is at war with both "b" and "c" — it can be antipodal to only one. The matched
            // pair (a,b) gets the diameter; the sibling war (a,c) degrades to a filler slot, which
            // GraphCanvas still bows into the ring. Correctness we CAN guarantee: nothing is lost.
            var nodes = new[] { "a", "b", "c", "d", "e" };

            var order = CircularArrangement.Arrange(nodes, new[] { War("a", "b"), War("a", "c") });

            AssertPermutationOf(nodes, order);
            Assert.Equal(order.Count / 2, CircularDistance(order, "a", "b")); // matched pair antipodal
        }

        [Fact]
        public void Arrange_IsDeterministic()
        {
            var nodes = new[] { "empire", "battania", "sturgia", "vlandia", "khuzait", "aserai" };
            var edges = new[] { War("empire", "battania"), War("sturgia", "vlandia") };

            var first = CircularArrangement.Arrange(nodes, edges);
            var second = CircularArrangement.Arrange(nodes, edges);

            Assert.Equal(first, second);
        }

        [Fact]
        public void Arrange_NoEdges_ReturnsInputOrderUnchanged()
        {
            var nodes = new[] { "a", "b", "c", "d" };

            var order = CircularArrangement.Arrange(nodes, Array.Empty<RelationEdge>());

            Assert.Equal(nodes, order);
        }

        [Fact]
        public void Arrange_TwoNodes_ReturnsInputOrder_AlreadyAntipodal()
        {
            var nodes = new[] { "a", "b" };

            var order = CircularArrangement.Arrange(nodes, new[] { War("a", "b") });

            Assert.Equal(nodes, order);
        }

        [Fact]
        public void Arrange_UnknownEndpoint_IsIgnored_AndOrderIsUnaffected()
        {
            var nodes = new[] { "a", "b", "c", "d" };

            var order = CircularArrangement.Arrange(nodes, new[] { War("a", "ghost") });

            // The only edge references a non-node, so there is nothing to seat: identity order.
            Assert.Equal(nodes, order);
        }

        [Fact]
        public void Arrange_DuplicateAndBothDirectionEdges_CollapseToOnePair()
        {
            var nodes = new[] { "a", "b", "c", "d", "e", "f" };

            var order = CircularArrangement.Arrange(
                nodes,
                new[] { War("a", "b"), War("b", "a"), War("a", "b") });

            AssertPermutationOf(nodes, order);
            Assert.Equal(order.Count / 2, CircularDistance(order, "a", "b")); // one collapsed pair, antipodal
        }

        [Fact]
        public void Arrange_NullNodes_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CircularArrangement.Arrange(null!, Array.Empty<RelationEdge>()));
        }

        [Fact]
        public void Arrange_NullEdges_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CircularArrangement.Arrange(new[] { "a" }, null!));
        }
    }
}
