using System;
using System.Collections.Generic;
using System.Globalization;
using DiplomacyOverview.Core;
using TaleWorlds.CampaignSystem;

namespace DiplomacyOverview.Providers
{
    /// <summary>
    /// War edges between non-eliminated kingdoms, straight from the vanilla stance system
    /// (docs/research/05: <c>IFaction.FactionsAtWarWith</c> per node — no O(n²) pair scan; the
    /// per-war <c>StanceLink</c> payload is captured into <see cref="RelationEdge.Details"/> for
    /// later tooltips).
    ///
    /// Because <see cref="RelationEdge.Create"/> canonicalizes endpoint order, every
    /// direction-sensitive detail is encoded in Details VALUES, never inferred from A/B order
    /// (issue #6 core-contract note): the tribute payer is stored as a node id under
    /// "tribute.payer".
    /// </summary>
    internal sealed class WarProvider : IRelationProvider
    {
        /// <summary>Details key: campaign day the war started (invariant-culture number).</summary>
        public const string WarStartDayKey = "war.startDay";

        /// <summary>Details key: StringId of the tribute-paying side. Absent when no tribute flows.</summary>
        public const string TributePayerKey = "tribute.payer";

        /// <summary>Details key: daily tribute amount paid by "tribute.payer" (positive integer).</summary>
        public const string TributeDailyAmountKey = "tribute.dailyAmount";

        public RelationKind Provides => RelationKind.War;

        public IReadOnlyList<RelationEdge> GetEdges()
        {
            try
            {
                return BuildEdges();
            }
            catch
            {
                // AGENTS.md rule 6: a provider failure means missing lines, never a crash.
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
                if (kingdom is null || kingdom.IsEliminated)
                {
                    continue;
                }

                var enemies = kingdom.FactionsAtWarWith;
                if (enemies is null)
                {
                    continue;
                }

                foreach (var enemy in enemies)
                {
                    if (!(enemy is Kingdom other) || other.IsEliminated)
                    {
                        continue; // kingdom scope only: wars against clans/minors are out (issue #6)
                    }

                    // FactionsAtWarWith yields each war from both sides; keep the pair once.
                    // (Also guards the self-loop RelationEdge.Create rejects.)
                    if (string.CompareOrdinal(kingdom.StringId, other.StringId) >= 0)
                    {
                        continue;
                    }

                    edges.Add(RelationEdge.Create(
                        kingdom.StringId, other.StringId, RelationKind.War, BuildDetails(kingdom, other)));
                }
            }

            return edges;
        }

        private static IReadOnlyDictionary<string, string>? BuildDetails(Kingdom kingdom, Kingdom other)
        {
            try
            {
                var stance = kingdom.GetStanceWith(other);
                if (stance is null)
                {
                    return null;
                }

                var details = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [WarStartDayKey] = stance.WarStartDate.ToDays.ToString("0.##", CultureInfo.InvariantCulture),
                };

                // GetDailyTributeToPay(f) is what f pays per day: positive = f pays, negative =
                // f receives (decompiled v1.3.15 StanceLink). Direction goes into the VALUE.
                var dailyTribute = stance.GetDailyTributeToPay(kingdom);
                if (dailyTribute > 0)
                {
                    details[TributePayerKey] = kingdom.StringId;
                    details[TributeDailyAmountKey] = dailyTribute.ToString(CultureInfo.InvariantCulture);
                }
                else if (dailyTribute < 0)
                {
                    details[TributePayerKey] = other.StringId;
                    details[TributeDailyAmountKey] = (-dailyTribute).ToString(CultureInfo.InvariantCulture);
                }

                return details;
            }
            catch
            {
                // Tooltip payload is optional garnish — losing it must not lose the edge.
                return null;
            }
        }
    }
}
