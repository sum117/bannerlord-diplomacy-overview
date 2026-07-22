using System;
using System.Collections.Generic;
using DiplomacyOverview.Core;
using TaleWorlds.CampaignSystem;

namespace DiplomacyOverview.Providers
{
    /// <summary>
    /// Alliance edges between non-eliminated kingdoms, straight from the vanilla v1.3 native
    /// alliance system (docs/research/05: <c>Kingdom.AlliedKingdoms</c> per node — no O(n²) pair
    /// scan, mirroring <see cref="WarProvider"/>).
    ///
    /// Unlike war, alliance has no asymmetric per-side payload (no tribute-style direction to
    /// encode), so edges here carry no <see cref="RelationEdge.Details"/>: end-date/tooltip data
    /// is deferred to issue #10.
    /// </summary>
    internal sealed class AllianceProvider : IRelationProvider
    {
        public RelationKind Provides => RelationKind.Alliance;

        public IReadOnlyList<RelationEdge> GetEdges()
        {
            try
            {
                return BuildEdges();
            }
            catch (Exception ex)
            {
                // AGENTS.md rule 6: a provider failure means missing lines, never a crash.
                Diagnostics.Note("alliance provider failed outright: " + ex);
                return Array.Empty<RelationEdge>();
            }
        }

        private static IReadOnlyList<RelationEdge> BuildEdges()
        {
            if (Campaign.Current is null) // P-07
            {
                return Array.Empty<RelationEdge>();
            }

            var edges = new List<RelationEdge>();

            foreach (var kingdom in Kingdom.All)
            {
                // Containment is PER KINGDOM on purpose: on a heavy mod list one malformed
                // kingdom object must cost its own edges, not the whole web (mirrors WarProvider).
                try
                {
                    if (!KingdomFilter.IsParticipant(kingdom))
                    {
                        continue;
                    }

                    var allies = kingdom.AlliedKingdoms; // MBReadOnlyList<Kingdom>, vanilla-maintained
                    if (allies is null)
                    {
                        continue;
                    }

                    foreach (var other in allies)
                    {
                        // Kingdom scope only (issue #6/#7), and zombie-kingdom endpoints are
                        // dropped by the same participant test used everywhere else in this mod.
                        if (!KingdomFilter.IsParticipant(other))
                        {
                            continue;
                        }

                        // AlliedKingdoms yields each alliance from both sides; keep the pair once.
                        // (Also guards the self-loop RelationEdge.Create rejects.)
                        if (string.CompareOrdinal(kingdom.StringId, other.StringId) >= 0)
                        {
                            continue;
                        }

                        edges.Add(RelationEdge.Create(kingdom.StringId, other.StringId, RelationKind.Alliance));
                    }
                }
                catch (Exception ex)
                {
                    Diagnostics.Note(
                        "alliance edges skipped for kingdom '" + (kingdom?.StringId ?? "<null>") + "': " + ex.Message);
                }
            }

            return edges;
        }
    }
}
