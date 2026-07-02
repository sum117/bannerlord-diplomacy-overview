using System;
using System.Collections.Generic;
using System.Linq;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// An immutable snapshot of nodes and their relation edges. Construction dedups edges (the same
    /// unordered node pair with the same <see cref="RelationKind"/> collapses to a single edge; the
    /// same pair with different kinds remains distinct) and silently drops any edge referencing a
    /// node id that is not in the node set.
    /// </summary>
    public sealed class RelationGraph
    {
        /// <summary>Node ids in the order supplied at construction.</summary>
        public IReadOnlyList<string> Nodes { get; }

        /// <summary>Deduped, order-preserved edges whose endpoints are both members of <see cref="Nodes"/>.</summary>
        public IReadOnlyList<RelationEdge> Edges { get; }

        public RelationGraph(IEnumerable<string> nodes, IEnumerable<RelationEdge> edges)
        {
            if (nodes is null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (edges is null)
            {
                throw new ArgumentNullException(nameof(edges));
            }

            var orderedNodes = nodes.ToList();
            var nodeSet = new HashSet<string>(orderedNodes, StringComparer.Ordinal);

            var seen = new HashSet<(string NodeA, string NodeB, RelationKind Kind)>();
            var dedupedEdges = new List<RelationEdge>();

            foreach (var edge in edges)
            {
                if (edge is null)
                {
                    continue;
                }

                if (!nodeSet.Contains(edge.NodeA) || !nodeSet.Contains(edge.NodeB))
                {
                    continue;
                }

                var key = (edge.NodeA, edge.NodeB, edge.Kind);
                if (seen.Add(key))
                {
                    dedupedEdges.Add(edge);
                }
            }

            Nodes = orderedNodes;
            Edges = dedupedEdges;
        }

        /// <summary>
        /// Returns the subset of <see cref="Edges"/> whose <see cref="RelationEdge.Kind"/> is set in
        /// <paramref name="mask"/>. <see cref="Nodes"/> is unaffected — only edges are filtered.
        /// </summary>
        public IReadOnlyList<RelationEdge> Filter(RelationKind mask)
        {
            if (mask == RelationKind.None)
            {
                return Array.Empty<RelationEdge>();
            }

            return Edges.Where(e => (e.Kind & mask) != 0).ToList();
        }
    }
}
