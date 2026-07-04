using System;
using System.Collections.Generic;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// Pure, deterministic node ordering for the relation web. <see cref="CircleLayout"/> seats
    /// nodes by list index, so the ORDER a graph's nodes are handed to it decides who lands next to
    /// whom — and, crucially, who lands OPPOSITE whom. Left in raw campaign order, related factions
    /// (e.g. a rebel kingdom and the parent it split from, adjacent in creation order) sit side by
    /// side on the ring, so their relation line collapses to a stub hidden under the two medallions
    /// (docs/research/10 runs 7–8; the reason <see cref="GraphCanvas"/> had to bow short edges into
    /// the interior). This reorders nodes so related factions are pushed toward opposite sides of
    /// the circle, turning each relation into a long, legible chord.
    ///
    /// Strategy (v1): greedily match the relation graph into vertex-disjoint pairs, seat each pair
    /// at an antipodal slot pair (exact diameters through the centre), spread the pairs evenly
    /// around the ring, and fill the remaining slots — unmatched "sibling" endpoints (a faction
    /// related to more than one other, which can be antipodal to only one partner) and unrelated
    /// factions — in stable input order. Matched
    /// pairs are the ones guaranteed a full-diameter line; a sibling degrades to whatever chord its
    /// fill slot yields (GraphCanvas still bows a short one into the ring). The pass is
    /// kind-agnostic: it spreads whatever edges it is given, so alliance / NAP edges get the same
    /// treatment as wars once those providers exist. A richer sibling strategy (component-aware fan
    /// placement, maximum matching) can replace the greedy matching without changing this contract.
    /// </summary>
    public static class CircularArrangement
    {
        /// <summary>
        /// Returns <paramref name="nodes"/> reordered so related pairs tend to sit on opposite
        /// sides of the circle. The result is a permutation of the input (same ids, same count).
        /// Deterministic: identical inputs yield an identical ordering. Edges referencing ids not in
        /// <paramref name="nodes"/> are ignored; duplicate and both-direction edges collapse.
        /// </summary>
        /// <param name="nodes">Ordered node ids; the input order is the tie-break for everything the
        /// arrangement does not otherwise constrain, keeping the output stable.</param>
        /// <param name="edges">Relation edges of any kind. Only the unordered node pair matters here
        /// (kind and details are ignored); every kind is spread the same way.</param>
        public static IReadOnlyList<string> Arrange(IReadOnlyList<string> nodes, IReadOnlyList<RelationEdge> edges)
        {
            if (nodes is null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (edges is null)
            {
                throw new ArgumentNullException(nameof(edges));
            }

            var count = nodes.Count;

            // Nothing to gain: 0/1 nodes are trivial, 2 are already antipodal (12 vs 6 o'clock),
            // and with no edges every ordering is equivalent. Preserve input order verbatim.
            if (count <= 2 || edges.Count == 0)
            {
                return nodes;
            }

            var indexOf = new Dictionary<string, int>(count, StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                // First occurrence wins; callers pass distinct ids, this is only defensive.
                if (!indexOf.ContainsKey(nodes[i]))
                {
                    indexOf[nodes[i]] = i;
                }
            }

            // Collect unique relation pairs as index tuples (lo < hi); drop unknown endpoints the
            // way RelationGraph would, so this stays a well-defined function of raw provider edges.
            var pairSet = new HashSet<(int Lo, int Hi)>();
            foreach (var edge in edges)
            {
                if (edge is null)
                {
                    continue;
                }

                if (!indexOf.TryGetValue(edge.NodeA, out var a) || !indexOf.TryGetValue(edge.NodeB, out var b))
                {
                    continue;
                }

                if (a == b)
                {
                    continue; // defensive: RelationEdge.Create already forbids self-loops
                }

                pairSet.Add(a < b ? (a, b) : (b, a));
            }

            if (pairSet.Count == 0)
            {
                return nodes;
            }

            // Deterministic pair order: by lower endpoint, then upper.
            var pairs = new List<(int Lo, int Hi)>(pairSet);
            pairs.Sort((p, q) => p.Lo != q.Lo ? p.Lo.CompareTo(q.Lo) : p.Hi.CompareTo(q.Hi));

            // Greedy maximal matching: seat a pair antipodally only when BOTH ends are still free.
            // The remaining edges of a "sibling" node (already matched to someone else) fall through
            // to the filler pass.
            var matched = new bool[count];
            var matchedPairs = new List<(int Lo, int Hi)>();
            foreach (var (lo, hi) in pairs)
            {
                if (!matched[lo] && !matched[hi])
                {
                    matched[lo] = true;
                    matched[hi] = true;
                    matchedPairs.Add((lo, hi));
                }
            }

            var order = new string?[count];
            var half = count / 2;
            var m = matchedPairs.Count;

            // Seat matched pairs at evenly-spread diameters: pair k at slot p_k in [0, half) and its
            // antipode p_k + half. floor(k*half/m) is strictly increasing for m <= half — always the
            // case here since 2m <= count — so the seats never collide, and p_k + half < count.
            for (var k = 0; k < m; k++)
            {
                var slotA = (int)((long)k * half / m);
                var slotB = slotA + half;
                order[slotA] = nodes[matchedPairs[k].Lo];
                order[slotB] = nodes[matchedPairs[k].Hi];
            }

            // Fill the rest — unmatched siblings and unrelated factions — in stable input order.
            // They act as spacers between the relation chords. There is exactly one empty slot per
            // filler, and fillSlot only advances, so this never overflows.
            var fillSlot = 0;
            for (var i = 0; i < count; i++)
            {
                if (matched[i])
                {
                    continue;
                }

                while (order[fillSlot] != null)
                {
                    fillSlot++;
                }

                order[fillSlot] = nodes[i];
                fillSlot++;
            }

            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = order[i]!;
            }

            return result;
        }
    }
}
