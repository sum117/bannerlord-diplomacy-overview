using System;
using System.Collections.Generic;

namespace DiplomacyOverview.Core
{
    /// <summary>
    /// An immutable, undirected edge between two opaque node ids (the game layer passes faction
    /// StringIds) describing exactly one <see cref="RelationKind"/>. Endpoints are stored in
    /// canonical order (<see cref="NodeA"/> precedes <see cref="NodeB"/> by ordinal comparison) so
    /// that two logically-identical edges constructed with swapped arguments compare equal.
    /// </summary>
    public sealed class RelationEdge
    {
        /// <summary>The lexicographically-first endpoint id (ordinal comparison).</summary>
        public string NodeA { get; }

        /// <summary>The lexicographically-second endpoint id (ordinal comparison).</summary>
        public string NodeB { get; }

        /// <summary>The single relation kind this edge represents.</summary>
        public RelationKind Kind { get; }

        /// <summary>Optional tooltip payload (e.g. war stats, expiry dates). Never a mutable reference.</summary>
        public IReadOnlyDictionary<string, string>? Details { get; }

        private RelationEdge(string nodeA, string nodeB, RelationKind kind, IReadOnlyDictionary<string, string>? details)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Kind = kind;
            Details = details;
        }

        /// <summary>
        /// Builds a canonical <see cref="RelationEdge"/>. Endpoints are reordered as needed so that
        /// <c>Create(a, b, ...)</c> and <c>Create(b, a, ...)</c> produce edges with identical
        /// <see cref="NodeA"/>/<see cref="NodeB"/> values.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="a"/> or <paramref name="b"/> is null/empty, when they are
        /// equal (a self-loop), or when <paramref name="kind"/> is not exactly one flag
        /// (i.e. it is <see cref="RelationKind.None"/> or a combination of multiple flags).
        /// </exception>
        public static RelationEdge Create(
            string a,
            string b,
            RelationKind kind,
            IReadOnlyDictionary<string, string>? details = null)
        {
            if (string.IsNullOrEmpty(a))
            {
                throw new ArgumentException("Node id must not be null or empty.", nameof(a));
            }

            if (string.IsNullOrEmpty(b))
            {
                throw new ArgumentException("Node id must not be null or empty.", nameof(b));
            }

            if (string.Equals(a, b, StringComparison.Ordinal))
            {
                throw new ArgumentException("An edge must connect two distinct nodes (no self-loops).", nameof(b));
            }

            if (!IsSingleFlag(kind))
            {
                throw new ArgumentException(
                    $"Kind must be exactly one flag; got '{kind}'.", nameof(kind));
            }

            return string.CompareOrdinal(a, b) <= 0
                ? new RelationEdge(a, b, kind, details)
                : new RelationEdge(b, a, kind, details);
        }

        private static bool IsSingleFlag(RelationKind kind)
        {
            var value = (int)kind;
            return value != 0 && (value & (value - 1)) == 0;
        }
    }
}
