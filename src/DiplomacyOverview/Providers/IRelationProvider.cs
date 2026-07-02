using System.Collections.Generic;
using DiplomacyOverview.Core;

namespace DiplomacyOverview.Providers
{
    /// <summary>
    /// A source of relation edges of exactly one <see cref="RelationKind"/>, queried lazily when
    /// the Relations tab (re)builds its graph. Contract (AGENTS.md rule 6): implementations are
    /// read-only over campaign state and never throw outward — any internal failure degrades to
    /// an empty edge list, so the worst user-visible outcome is "lines missing", never a crash.
    /// </summary>
    internal interface IRelationProvider
    {
        /// <summary>The single relation kind this provider emits.</summary>
        RelationKind Provides { get; }

        /// <summary>
        /// Snapshot of the current edges (kingdom scope for now). Never null, never throws;
        /// returns an empty list outside campaigns (P-07) or on any internal failure.
        /// </summary>
        IReadOnlyList<RelationEdge> GetEdges();
    }
}
